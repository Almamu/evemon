﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.Data;

namespace EVEMon.SkillPlanner
{
    public sealed partial class ShipSelectControl : EveObjectSelectControl
    {
        private Func<Item, Boolean> m_racePredicate = (x) => true;
        private bool m_isLoaded;

        #region Initialisation
        /// <summary>
        /// Constructor
        /// </summary>
        public ShipSelectControl()
        {
            InitializeComponent();
            this.cbSkillFilter.Items[0] = "All Ships";
            this.cbSkillFilter.Items[1] = "Ships I can fly";
            this.cbSkillFilter.Items[2] = "Ships I cannot fly";
        }

        /// <summary>
        /// On load, we read the settings and fill the tree.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void EveObjectSelectControl_Load(object sender, EventArgs e)
        {
            // Return on design mode
            if (this.DesignMode || this.IsDesignModeHosted()) return;

            // Call the base method
            base.EveObjectSelectControl_Load(sender, e);

            // Build the ships tree
            BuildTreeView();

            // Read the settings
            if (Settings.UI.UseStoredSearchFilters)
            {
                cbSkillFilter.SelectedIndex = (int)Settings.UI.ShipBrowser.UsabilityFilter;
                tbSearchText.Text = Settings.UI.ShipBrowser.TextSearch;

                this.cbAmarr.Checked = (Settings.UI.ShipBrowser.RacesFilter & Race.Amarr) != Race.None;
                this.cbCaldari.Checked = (Settings.UI.ShipBrowser.RacesFilter & Race.Caldari) != Race.None;
                this.cbGallente.Checked = (Settings.UI.ShipBrowser.RacesFilter & Race.Gallente) != Race.None;
                this.cbMinmatar.Checked = (Settings.UI.ShipBrowser.RacesFilter & Race.Minmatar) != Race.None;
                this.cbFaction.Checked = (Settings.UI.ShipBrowser.RacesFilter & Race.Faction) != Race.None;
                this.cbORE.Checked = (Settings.UI.ShipBrowser.RacesFilter & Race.Ore) != Race.None;
            }
            else
            {
                cbSkillFilter.SelectedIndex = 0;
            }
        }
        #endregion

        #region Callbacks
        /// <summary>
        /// When the combo for filter changes, we update the settings and the control content.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbSkillFilter_SelectedIndexChanged(object sender, EventArgs e)
        {
            // Update settings
            Settings.UI.ShipBrowser.UsabilityFilter = (ObjectUsabilityFilter)cbSkillFilter.SelectedIndex;

            // Update the filter delegate
            switch (Settings.UI.ShipBrowser.UsabilityFilter)
            {
                case ObjectUsabilityFilter.All: m_usabilityPredicate = SelectAll;
                    break;

                case ObjectUsabilityFilter.Usable:
                    m_usabilityPredicate = CanUse;
                    break;

                case ObjectUsabilityFilter.Unusable:
                    m_usabilityPredicate = CannotUse;
                    break;

                default:
                    throw new NotImplementedException();
            }

            // Update content
            UpdateContent();
        }

        /// <summary>
        /// When one of the races combo is checked/unchecked, we update the settings and the control content.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void cbRace_SelectedChanged(object sender, EventArgs e)
        {
            // Retrieve the race
            Race race = Race.None;
            if (cbAmarr.Checked) race |= Race.Amarr;
            if (cbCaldari.Checked) race |= Race.Caldari;
            if (cbGallente.Checked) race |= Race.Gallente;
            if (cbMinmatar.Checked) race |= Race.Minmatar;
            if (cbFaction.Checked) race |= Race.Faction;
            if (cbORE.Checked) race |= Race.Ore;

            // Update the settings
            Settings.UI.ShipBrowser.RacesFilter |= race;

            // Update the predicate
            m_racePredicate = (x) => (x.Race & race) != Race.None;

            // Update content
            m_isLoaded = true;
            UpdateContent();
        }

        /// <summary>
        /// When the search text changed, we store the next settings and update the list view and the list/tree visibilities.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        protected override void tbSearchText_TextChanged(object sender, EventArgs e)
        {
            Settings.UI.ShipBrowser.TextSearch = tbSearchText.Text;
            base.tbSearchText_TextChanged(sender, e);
        }
        #endregion

        #region Content creation
        /// <summary>
        /// Refresh the controls
        /// </summary>
        private void UpdateContent()
        {
            if (!m_isLoaded) return;
            BuildTreeView();
            BuildListView();
        }

        /// <summary>
        /// Rebuild the tree view
        /// </summary>
        private void BuildTreeView()
        {
            tvItems.BeginUpdate();
            tvItems.Nodes.Clear();
            try
            {
                // Create the nodes
                foreach (var typeGroup in StaticItems.Ships.SubGroups)
                {
                    // Sub groups
                    TreeNode typeNode = new TreeNode(typeGroup.Name);
                    foreach (var raceGroup in typeGroup.SubGroups)
                    {
                        // items
                        TreeNode raceNode = new TreeNode(raceGroup.Name);
                        foreach (var ship in raceGroup.Items.Where(m_usabilityPredicate).Where(m_racePredicate))
                        {
                            TreeNode shipNode = new TreeNode(ship.Name);
                            shipNode.Tag = ship;

                            raceNode.Nodes.Add(shipNode);
                        }

                        // dont display empty nodes
                        if (raceNode.Nodes.Count != 0) typeNode.Nodes.Add(raceNode);
                    }

                    // Items
                    foreach (var ship in typeGroup.Items)
                    {
                        TreeNode shipNode = new TreeNode(ship.Name);
                        shipNode.Tag = ship;

                        typeNode.Nodes.Add(shipNode);
                    }

                    // dont display empty nodes
                    if (typeNode.Nodes.Count != 0) tvItems.Nodes.Add(typeNode);
                }
            }
            finally
            {
                tvItems.EndUpdate();
            }
        }
        #endregion
    }
}
