﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;

namespace Engine
{
    public class Player : LivingCreature
    {
        private int _gold;
        private int _experiencePoints;
        public int Gold
        {
            get { return _gold; }
            set
            {
                _gold = value;
                OnPropertyChanged("Gold");
            }
        }
        public int ExperiencePoints
        {
            get { return _experiencePoints; }
            private set
            {
                _experiencePoints = value;
                OnPropertyChanged("ExperiencePoints");
                OnPropertyChanged("Level");
            }
        }
        //public int Level { get; set; }
        public int Level
        {
            get { return ((ExperiencePoints / 100) + 1); }
        }
        public Location CurrentLocation { get; set; }
        public Weapon CurrentWeapon { get; set; }
        public BindingList<InventoryItem> Inventory { get; set; }
        public BindingList<PlayerQuest> Quests { get; set; }

        //因为有其他2种方法来创建Player对象，因此设置成private（这意味着它只能由 Player 类中的另一个函数调用）
        private Player(int currentHitPoints, int maximumHitPoints, int gold, int experiencePoints) : base(currentHitPoints, maximumHitPoints)
        {
            Gold = gold;
            ExperiencePoints = experiencePoints;
            Inventory = new BindingList<InventoryItem>();
            Quests = new BindingList<PlayerQuest>();
        }
        //如果没有存档，创建一个新角色
        public static Player CreateDefaultPlayer()
        {
            Player player = new Player(10, 10, 20, 0);
            player.Inventory.Add(new InventoryItem(World.ItemByID(World.ITEM_ID_RUSTY_SWORD), 1));
            player.CurrentLocation = World.LocationByID(World.LOCATION_ID_HOME);
            return player;
        }
        //从XML获取player数据
        public static Player CreatePlayerFromXmlString(string xmlPlayerData)
        {
            try
            {   // Load the XML data into an XmlDocument object
                XmlDocument playerData = new XmlDocument();
                playerData.LoadXml(xmlPlayerData);

                // Extract the player's stats and inventory items from the XML data
                int currentHitPoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentHitPoints").InnerText);
                int maximumHitPoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/MaximumHitPoints").InnerText);
                int gold = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/Gold").InnerText);
                int experiencePoints = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/ExperiencePoints").InnerText);
                
                // Create a new Player object with the extracted stats and inventory items
                Player player = new Player(currentHitPoints, maximumHitPoints, gold, experiencePoints);
                
                // Set the player's current location
                int currentLocationID = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentLocation").InnerText);
                player.CurrentLocation = World.LocationByID(currentLocationID);

                //set the player's weapon
                if (playerData.SelectSingleNode("/Player/Stats/CurrentWeapon") != null)
                {
                    int currentWeaponID = Convert.ToInt32(playerData.SelectSingleNode("/Player/Stats/CurrentWeapon").InnerText);
                    player.CurrentWeapon = (Weapon)World.ItemByID(currentWeaponID);
                }

                // Add inventory items to the player's inventory
                foreach (XmlNode node in playerData.SelectNodes("/Player/InventoryItems/InventoryItem"))
                {
                    int id = Convert.ToInt32(node.Attributes["ID"].Value);
                    int quantity = Convert.ToInt32(node.Attributes["Quantity"].Value);
                    for (int i = 0; i < quantity; i++)
                    {
                        player.AddItemToInventory(World.ItemByID(id));
                    }
                }
                // Add player quests to the player's quest list
                foreach (XmlNode node in playerData.SelectNodes("/Player/PlayerQuests/PlayerQuest"))
                {
                    int id = Convert.ToInt32(node.Attributes["ID"].Value);
                    bool isCompleted = Convert.ToBoolean(node.Attributes["IsCompleted"].Value);
                    PlayerQuest playerQuest = new PlayerQuest(World.QuestByID(id));
                    playerQuest.IsCompleted = isCompleted;
                    player.Quests.Add(playerQuest);
                }
                // Return the created player object
                return player;
            }
            catch
            {
                // If there was an error with the XML data, return a default player object
                return Player.CreateDefaultPlayer();
            }
        }
        //增加经验值
        public void AddExperiencePoints(int experiencePointsToAdd)
        {
            ExperiencePoints += experiencePointsToAdd;
            MaximumHitPoints = (Level * 10);
        }
        //当玩家获得等级时增加最大生命值

        public bool HasRequiredItemToEnterThisLocation(Location location)
        {
            if (location.ItemRequiredToEnter == null)
            {
                // There is no required item for this location, so return "true"
                return true;
            }

            // See if the player has the required item in their inventory
            return Inventory.Any(ii => ii.Details.ID == location.ItemRequiredToEnter.ID);

        }

        //public bool HasThisQuest(Quest quest)
        //{
        //    foreach (PlayerQuest playerQuest in Quests)
        //    {
        //        if (playerQuest.Details.ID == quest.ID)
        //        {
        //            return true;
        //        }
        //    }

        //    return false;
        //}
        public bool HasThisQuest(Quest quest)
        {
            return Quests.Exists(pq=>pq.Details.ID == quest.ID);
        }

        public bool CompletedThisQuest(Quest quest)
        {
            foreach (PlayerQuest playerQuest in Quests)
            {
                if (playerQuest.Details.ID == quest.ID)
                {
                    return playerQuest.IsCompleted;
                }
            }

            return false;
        }
        //public bool CompletedThisQuest(Quest quest)
        //{
        //    return Quests.Exists(pq => pq.Details.ID != quest.ID);
        //}

        public bool HasAllQuestCompletionItems(Quest quest)
        {
            // See if the player has all the items needed to complete the quest here
            foreach (QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                // Check each item in the player's inventory, to see if they have it, and enough of it
                if (!Inventory.Any(ii => ii.Details.ID == qci.Details.ID && ii.Quantity >= qci.Quantity))
                {
                    return false;
                }
            }
            // If we got here, then the player must have all the required items, and enough of them, to complete the quest.
            return true;
        }

        public void RemoveQuestCompletionItems(Quest quest)
        {
            foreach (QuestCompletionItem qci in quest.QuestCompletionItems)
            {
                //foreach (InventoryItem ii in Inventory)
                //{
                //    if (ii.Details.ID == qci.Details.ID)
                //    {
                //        // Subtract the quantity from the player's inventory that was needed to complete the quest
                //        ii.Quantity -= qci.Quantity;
                //        break;
                //    }
                //}
                InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == qci.Details.ID);
                if (item != null)
                {
                    // Subtract the quantity from the player's inventory that was needed to complete the quest
                    item.Quantity -= qci.Quantity;
                }
            }
        }

        public void AddItemToInventory(Item itemToAdd)
        {
            InventoryItem item = Inventory.SingleOrDefault(ii => ii.Details.ID == itemToAdd.ID);
            if(item != null)// They have the item in their inventory, so increase the quantity by one
            {
                item.Quantity++;
            }
            else// They didn't have the item, so add it to their inventory, with a quantity of 1
            {
                Inventory.Add(new InventoryItem(itemToAdd, 1));
            }
            //foreach (InventoryItem ii in Inventory)
            //{
            //    if (ii.Details.ID == itemToAdd.ID)
            //    {
            //        
            //        ii.Quantity++;
            //        return; // We added the item, and are done, so get out of this function
            //    }
            //}
            //
            //Inventory.Add(new InventoryItem(itemToAdd, 1));
        }

        public void MarkQuestCompleted(Quest quest)
        {
            // Find the quest in the player's quest list
            PlayerQuest item = Quests.SingleOrDefault(ii => ii.Details.ID == quest.ID);
            if(item != null )
            {
                item.IsCompleted = true;
            }
            //foreach (PlayerQuest pq in Quests)
            //{
            //    if (pq.Details.ID == quest.ID)
            //    {
            //        // Mark it as completed
            //        pq.IsCompleted = true;
            //        return; // We found the quest, and marked it complete, so get out of this function
            //    }
            //}
        }

        //  <Player>
        //    <Stats>
        //        <CurrentHitPoints>7</CurrentHitPoints>
        //        <MaximumHitPoints>10</MaximumHitPoints>
        //        <Gold>123</Gold>
        //        <ExperiencePoints>275</ExperiencePoints>
        //        <CurrentLocation>2</CurrentLocation>
        //    </Stats>
        //    <InventoryItems>
        //        <InventoryItem ID = "1" Quantity="1" />
        //        <InventoryItem ID = "2" Quantity="5" />
        //        <InventoryItem ID = "7" Quantity="2" />
        //    </InventoryItems>
        //    <PlayerQuests>
        //        <PlayerQuest ID = "1" IsCompleted="true" />
        //        <PlayerQuest ID = "2" IsCompleted="false" />
        //    </PlayerQuests>
        //  </Player>
        public string ToXmlString()

        {
            XmlDocument playerData = new XmlDocument();

            // Create the top-level XML node，CreateElement 方法创建 XML 元素节点
            XmlNode player = playerData.CreateElement("Player");
            playerData.AppendChild(player);

            // Create the "Stats" child node to hold the other player statistics nodes
            XmlNode stats = playerData.CreateElement("Stats");
            player.AppendChild(stats);

            // Create the child nodes for the "Stats" node
            XmlNode currentHitPoints = playerData.CreateElement("CurrentHitPoints");
            //将创建的文本节点作为子节点添加到CurrentHitPoints元素中。这将使得CurrentHitPoints元素包含新创建的文本节点作为其子节点。
            currentHitPoints.AppendChild(playerData.CreateTextNode(this.CurrentHitPoints.ToString()));
            stats.AppendChild(currentHitPoints);

            XmlNode maximumHitPoints = playerData.CreateElement("MaximumHitPoints");
            maximumHitPoints.AppendChild(playerData.CreateTextNode(this.MaximumHitPoints.ToString()));
            stats.AppendChild(maximumHitPoints);

            XmlNode gold = playerData.CreateElement("Gold");
            gold.AppendChild(playerData.CreateTextNode(this.Gold.ToString()));
            stats.AppendChild(gold);

            XmlNode experiencePoints = playerData.CreateElement("ExperiencePoints");
            experiencePoints.AppendChild(playerData.CreateTextNode(this.ExperiencePoints.ToString()));
            stats.AppendChild(experiencePoints);

            XmlNode currentLocation = playerData.CreateElement("CurrentLocation");
            currentLocation.AppendChild(playerData.CreateTextNode(this.CurrentLocation.ID.ToString()));
            stats.AppendChild(currentLocation);

            if (CurrentWeapon != null)
            {
                XmlNode currentWeapon = playerData.CreateElement("CurrentWeapon");
                currentWeapon.AppendChild(playerData.CreateTextNode(this.CurrentWeapon.ID.ToString()));
                stats.AppendChild(currentWeapon);
            }

            // Create the "InventoryItems" child node to hold each InventoryItem node
            XmlNode inventoryItems = playerData.CreateElement("InventoryItems");
            player.AppendChild(inventoryItems);

            // Create an "InventoryItem" node for each item in the player's inventory
            foreach (InventoryItem item in this.Inventory)
            {
                // Create a new "InventoryItem" node for this item
                XmlNode inventoryItem = playerData.CreateElement("InventoryItem");
                //CreateAttribute 方法是一种用于创建 XML 文档中的属性节点的方法
                XmlAttribute idAttribute = playerData.CreateAttribute("ID");
                //设置属性值
                idAttribute.Value = item.Details.ID.ToString();
                // 将属性节点添加到元素节点中
                inventoryItem.Attributes.Append(idAttribute);
                XmlAttribute quantityAttribute = playerData.CreateAttribute("Quantity");
                quantityAttribute.Value = item.Quantity.ToString();
                inventoryItem.Attributes.Append(quantityAttribute);
                // Add the "InventoryItem" node to the "InventoryItems" node
                inventoryItems.AppendChild(inventoryItem);
            }

            // Create the "PlayerQuests" child node to hold each PlayerQuest node
            XmlNode playerQuests = playerData.CreateElement("PlayerQuests");
            player.AppendChild(playerQuests);

            // Create a "PlayerQuest" node for each quest the player has acquired
            foreach (PlayerQuest quest in this.Quests)
            {
                XmlNode playerQuest = playerData.CreateElement("PlayerQuest");
                XmlAttribute idAttribute = playerData.CreateAttribute("ID");
                idAttribute.Value = quest.Details.ID.ToString();
                playerQuest.Attributes.Append(idAttribute);
                XmlAttribute isCompletedAttribute = playerData.CreateAttribute("IsCompleted");
                isCompletedAttribute.Value = quest.IsCompleted.ToString();
                playerQuest.Attributes.Append(isCompletedAttribute);
                playerQuests.AppendChild(playerQuest);
            }

            return playerData.InnerXml; // The XML document, as a string, so we can save the data to disk
        }
    }
}