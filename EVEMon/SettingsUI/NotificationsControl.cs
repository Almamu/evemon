﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.Notifications;

namespace EVEMon.SettingsUI
{
    public partial class NotificationsControl : UserControl
    {
        // Would have love to use tableLayoutPanel, unfortunately, they are just a piece of trash.
        public const int RowHeight = 28;


        private List<ComboBox> m_combos = new List<ComboBox>();
        private List<CheckBox> m_checkboxes = new List<CheckBox>();
        private NotificationSettings m_settings;

        /// <summary>
        /// Constructor
        /// </summary>
        public NotificationsControl()
        {
            InitializeComponent();

            // Add the controls for every member of the enumeration
            int height = RowHeight;
            var categories = Enum.GetValues(typeof(NotificationCategory)).Cast<NotificationCategory>();
            foreach (var cat in categories)
            {
                // Add the label
                var label = new Label();
                label.AutoSize = false;
                label.Text = cat.GetHeader();
                label.TextAlign = ContentAlignment.MiddleLeft;
                label.Location = new Point(labelNotification.Location.X, height);
                label.Width = labelBehaviour.Location.X - 3;
                label.Height = RowHeight;
                this.Controls.Add(label);

                // Add the "system tray tooltip" combo box
                var combo = new ComboBox();
                combo.Tag = cat;
                combo.Items.Add("Never");
                combo.Items.Add("Once");
                combo.Items.Add("Repeat until clicked");
                combo.SelectedIndex = 0;
                combo.Margin = new Padding(3);
                combo.Height = RowHeight - 4;
                combo.Width = labelBehaviour.Width;
                combo.DropDownStyle = ComboBoxStyle.DropDownList;
                combo.Location = new Point(labelBehaviour.Location.X, height + 2);
                combo.SelectedIndexChanged += new EventHandler(combo_SelectedIndexChanged);
                this.Controls.Add(combo);
                m_combos.Add(combo);

                // Add the "main window" checkbox
                var checkbox = new CheckBox();
                checkbox.Tag = cat;
                checkbox.Text = "Show";
                checkbox.Margin = new Padding(3);
                checkbox.Height = RowHeight - 4;
                checkbox.Width = labelMainWindow.Width;
                checkbox.Location = new Point(labelMainWindow.Location.X + 15, height + 2);
                checkbox.CheckedChanged += new EventHandler(checkbox_CheckedChanged);
                this.Controls.Add(checkbox);
                m_checkboxes.Add(checkbox);

                // Updates the row ordinate
                height += RowHeight;
            }

            this.Height = height;
        }

        /// <summary>
        /// Gets or sets the settings to edit (should be a copy of the actual settings).
        /// </summary>
        [Browsable(false)]
        public NotificationSettings Settings
        {
            get { return m_settings; }
            set
            {
                m_settings = value;
                if (value == null) return;

                foreach (var combo in m_combos)
                {
                    var cat = (NotificationCategory)combo.Tag;
                    combo.SelectedIndex = (int)m_settings.Categories[cat].ToolTipBehaviour;
                }
                foreach (var checkbox in m_checkboxes)
                {
                    var cat = (NotificationCategory)checkbox.Tag;
                    checkbox.Checked = m_settings.Categories[cat].ShowOnMainWindow;
                }
            }
        }

        /// <summary>
        /// When the selected indew changes, we update the settings.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void combo_SelectedIndexChanged(object sender, EventArgs e)
        {
            var combo = (ComboBox)sender;
            var cat = (NotificationCategory)combo.Tag;
            m_settings.Categories[cat].ToolTipBehaviour = (ToolTipNotificationBehaviour)combo.SelectedIndex;
        }

        void checkbox_CheckedChanged(object sender, EventArgs e)
        {
            var checkbox = (CheckBox)sender;
            var cat = (NotificationCategory)checkbox.Tag;
            m_settings.Categories[cat].ShowOnMainWindow = checkbox.Checked;
        }
    }
}
