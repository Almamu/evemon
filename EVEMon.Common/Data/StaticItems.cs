﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;
using EVEMon.Common.Serialization.Datafiles;

namespace EVEMon.Common.Data
{
    /// <summary>
    /// Represents all the items (not ships or implants, see <see cref="StaticShips"/> and <see cref="StaticImplants"/> for that) loaded from the datafiles. 
    /// Not that not all items are present, only the ones you can use for your ship. 
    /// </summary>
    public static class StaticItems
    {
        private static readonly Dictionary<int, Item> s_itemsByID = new Dictionary<int, Item>();
        private static readonly ImplantSlot[] s_implantSlots = new ImplantSlot[10];

        private static MarketGroupCollection s_roots;
        private static MarketGroup s_shipsGroup;

        private static bool s_reprocessingInitialized = false;

        /// <summary>
        /// Gets the root category, containing all the top level categories
        /// </summary>
        public static MarketGroupCollection MarketGroups
        {
            get 
            {
                return s_roots; 
            }
        }

        /// <summary>
        /// Gets the collection of all the items in this category and its descendants.
        /// </summary>
        public static IEnumerable<Item> AllItems
        {
            get
            {
                foreach (var item in s_itemsByID.Values)
                {
                    yield return item;
                }
            }
        }

        /// <summary>
        /// Gets the market group for ships.
        /// </summary>
        public static MarketGroup Ships
        {
            get { return s_shipsGroup; }
        }

        /// <summary>
        /// Gets the collection of implants for the given slot.
        /// </summary>
        /// <param name="slot"></param>
        /// <returns></returns>
        public static ImplantSlot GetImplants(ImplantSlots slot)
        {
            return s_implantSlots[(int)slot];
        }

        /// <summary>
        /// Recursively searches the root category and all underlying categories for the first item with a 
        /// name that exactly matches the given itemName.
        /// </summary>
        /// <param name="itemName">The name of the item to find.</param>
        /// <returns>The first item which name matches itemName, Null if no such item is found.</returns>
        public static Item FindItem(string itemName)
        {
            foreach (var item in s_itemsByID.Values)
            {
                if (item.Name == itemName) return item;
            }
            return null;
        }

        /// <summary>
        /// Recursively searches the root category and all underlying categories for the first item with an 
        /// Id matching the given itemId.
        /// </summary>
        /// <param name="itemId">The id of the item to find.</param>
        /// <returns>The first item which id matches itemId, Null if no such item is found.</returns>
        public static Item GetItem(int itemId)
        {
            Item value = null;
            s_itemsByID.TryGetValue(itemId, out value);
            return value;
        }

        /// <summary>
        /// Load the items from the datafile
        /// </summary>
        internal static void Load()
        {
            if (s_roots != null) return;

            // Create the implants slots
            for (int i = 0; i < s_implantSlots.Length; i++)
            {
                s_implantSlots[i] = new ImplantSlot((ImplantSlots)i);
                s_implantSlots[i].Add(new Implant());
            }

            // Deserialize the items datafile
            var datafile = Util.DeserializeDatafile<ItemsDatafiles>(DatafileConstants.ItemsDatafile);
            s_roots = new MarketGroupCollection(null, datafile.MarketGroups);

            // Gather the items into a by-ID dictionary.
            foreach (var group in s_roots)
            {
                InitializeDictionaries(group);
            }
        }

        /// <summary>
        /// Recursively collect the items within all groups and stores them in the dictionaries.
        /// </summary>
        /// <param name="group"></param>
        private static void InitializeDictionaries(MarketGroup group)
        {
            // Special groups
            if (group.ID == DBConstants.ShipsGroupID)
            {
                s_shipsGroup = group;
            }

            foreach (var item in group.Items)
            {
                s_itemsByID[item.ID] = item;
            }

            foreach (var childGroup in group.SubGroups)
            {
                InitializeDictionaries(childGroup);
            }
        }

        /// <summary>
        /// Ensures the reprocessing informations have been intialized.
        /// </summary>
        internal static bool EnsureReprocessingInitialized()
        {
            if (s_reprocessingInitialized) return false;
            var datafile = Util.DeserializeDatafile<ReprocessingDatafile>(DatafileConstants.ReprocessingDatafile);

            foreach (var itemMaterials in datafile.Items)
            {
                // Skip if no materials
                if (itemMaterials.Materials == null) continue;

                var item = s_itemsByID[itemMaterials.ID];
                item.InitializeReprocessing(itemMaterials.Materials);
            }

            s_reprocessingInitialized = true;
            return true;
        }
    }
}
