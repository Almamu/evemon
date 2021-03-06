﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.Common.Notifications;
using EVEMon.Common.SettingsObjects;

namespace EVEMon
{
    /// <summary>
    /// Displays a list of notifications
    /// </summary>
    public partial class NotificationList : UserControl
    {
        private readonly Color WarningColor = Color.LightGoldenrodYellow;
        private readonly Color ErrorColor = Color.LavenderBlush;
        private readonly Color InfoColor = Color.AliceBlue;

        private const int TextLeft = 20;
        private const int LeftPadding = 2;
        private const int RightPadding = 2;
        private const int IconDeletePositionFromRight = 14;
        private const int IconMagnifierPositionFromRight = 34;

        private List<Notification> m_notifications = new List<Notification>();
        private int m_hoveredIndex = -1;

        private bool m_pendingUpdate;

        /// <summary>
        /// Constructor.
        /// </summary>
        public NotificationList()
        {
            InitializeComponent();
            listBox.DrawItem += new DrawItemEventHandler(listBox_DrawItem);
            listBox.MouseMove += new MouseEventHandler(listBox_MouseMove);
            listBox.MouseDown += new MouseEventHandler(listBox_MouseDown);
            listBox.MouseLeave += new EventHandler(listBox_MouseLeave);
        }

        /// <summary>
        /// On load, fils up the list for the design mode.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            if (this.DesignMode || this.IsDesignModeHosted())
            {
                var list = new List<Notification>();
                var notification = new Notification(NotificationCategory.AccountNotInTraining, null);
                notification.Priority = NotificationPriority.Information;
                notification.Description = "Some information";
                list.Add(notification);

                notification = new Notification(NotificationCategory.AccountNotInTraining, null);
                notification.Priority = NotificationPriority.Warning;
                notification.Description = "Some warning";
                list.Add(notification);

                notification = new Notification(NotificationCategory.AccountNotInTraining, null);
                notification.Priority = NotificationPriority.Error;
                notification.Description = "Some error";
                list.Add(notification);

                this.Notifications = list;
            }

            base.OnLoad(e);
        }

        /// <summary>
        /// Gets or sets the displayed notifications.
        /// </summary>
        public IEnumerable<Notification> Notifications
        {
            get 
            {
                foreach (var notification in m_notifications)
                {
                    yield return notification;
                }
            }
            set
            {
                m_notifications.Clear();
                if (value != null)
                {
                    var notificationsToAdd = value.Where(x => Settings.Notifications.Categories[x.Category].ShowOnMainWindow);
                    m_notifications.AddRange(notificationsToAdd.ToArray().OrderBy(x => (int)x.Priority));
                }
                UpdateContent();
            }
        }

        /// <summary>
        /// Updates the content of the listbox.
        /// </summary>
        private void UpdateContent()
        {
            m_pendingUpdate = true;
            if (!this.Visible) return;
            m_pendingUpdate = false;

            listBox.BeginUpdate();
            try
            {
                listBox.Items.Clear();
                foreach (var notification in m_notifications)
                {
                    listBox.Items.Add(notification);
                }
            }
            finally
            {
                listBox.EndUpdate();
            }

            // Update size
            this.Height = m_notifications.Count * listBox.ItemHeight;
            this.Width = listBox.Width;
            this.Invalidate();
        }

        /// <summary>
        /// Draws an item.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void listBox_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index == -1) return;
            var g = e.Graphics;

            var notification = m_notifications[e.Index];

            // Retrieves the icon and background color
            Image icon;
            Color color;
            switch (notification.Priority)
            {
                case NotificationPriority.Error:
                    icon = Properties.Resources.Error16;
                    color = ErrorColor;
                    break;
                case NotificationPriority.Warning:
                    icon = Properties.Resources.Warning16;
                    color = WarningColor;
                    break;
                case NotificationPriority.Information:
                    icon = Properties.Resources.Information16;
                    color = InfoColor;
                    break;
                default:
                    throw new NotImplementedException();
            }

            // Background
            using (var brush = new SolidBrush(color))
            {
                g.FillRectangle(brush, e.Bounds);
            }

            // Left icon
            g.DrawImageUnscaled(icon, new Point(e.Bounds.Left + LeftPadding, e.Bounds.Top + (listBox.ItemHeight - icon.Height) / 2));

            // Delete icon
            if (m_hoveredIndex == e.Index) icon = Properties.Resources.CrossBlack;
            else icon = Properties.Resources.CrossGray;
            g.DrawImageUnscaled(icon, new Point(e.Bounds.Right - IconDeletePositionFromRight, e.Bounds.Top + (listBox.ItemHeight - icon.Height) / 2));

            // Magnifier icon
            if (notification.HasDetails)
            {
                icon = Properties.Resources.Magnifier;
                g.DrawImageUnscaled(icon, new Point(e.Bounds.Right - IconMagnifierPositionFromRight, e.Bounds.Top + (listBox.ItemHeight - icon.Height) / 2));
            }

            // Text
            using (var foreBrush = new SolidBrush(this.ForeColor))
            {
                string text = notification.ToString();
                var size = g.MeasureString(text, this.Font);
                g.DrawString(text, this.Font, foreBrush, new Point(e.Bounds.Left + TextLeft, e.Bounds.Top + (int)(listBox.ItemHeight - size.Height) / 2));
            }

            // Draw line on top
            using (var lineBrush = new SolidBrush(Color.Gray))
            {
                using (var pen = new Pen(lineBrush, 1.0f))
                {
                    g.DrawLine(pen, new Point(e.Bounds.Left, e.Bounds.Bottom - 1), new Point(e.Bounds.Right, e.Bounds.Bottom - 1));
                }
            }
        }

        /// <summary>
        /// When the mouse moves, detect the hovered index
        /// </summary>
        /// <param name="e"></param>
        void listBox_MouseMove(object sender, MouseEventArgs e)
        {
            int oldHoveredIndex = m_hoveredIndex;

            m_hoveredIndex = -1;
            for(int i = 0; i<m_notifications.Count; i++)
            {
                var rect = GetDeleteIconRect(i);
                if (rect.Contains(e.Location))
                {
                    // Repaint the listbox if the previous index was different
                    m_hoveredIndex = i;
                    if (oldHoveredIndex != m_hoveredIndex)
                    {
                        listBox.Invalidate();
                        DisplayTooltip((Notification)listBox.Items[i]);
                    }
                    return;
                }
            }

            toolTip.Active = false;
        }

        /// <summary>
        /// When the user clicks, we need to detect whether it was on one of the buttons.
        /// </summary>
        /// <param name="e"></param>
        void listBox_MouseDown(object sender, MouseEventArgs e)
        {
            // First test whether the "delete" and "mangifier" icons have been clicked
            for (int i = 0; i < m_notifications.Count; i++)
            {
                // Did he click on the "delete" icon ?
                var rect = GetDeleteIconRect(i);

                if (rect.Contains(e.Location))
                {
                    EveClient.Notifications.Remove(m_notifications[i]);
                    m_notifications.RemoveAt(i);
                    UpdateContent();
                    return;
                }

                // Did he click on the "magnifier" icon ?
                if (m_notifications[i].HasDetails)
                {
                    rect = GetMagnifierIconRect(i);
                    if (rect.Contains(e.Location))
                    {
                        ShowDetails(m_notifications[i]);
                        return;
                    }
                }
            }

            // On wheel-click, we delete the clicked notification.
            if (e.Button == MouseButtons.Middle)
            {
                for (int i = 0; i < m_notifications.Count; i++)
                {
                    var rect = listBox.GetItemRectangle(i);
                    if (rect.Contains(e.Location))
                    {
                        EveClient.Notifications.Remove(m_notifications[i]);
                        m_notifications.RemoveAt(i);
                        UpdateContent();
                        return;
                    }
                }
            }
        }

        /// <summary>
        /// Show the details for the given notification.
        /// </summary>
        /// <param name="notification"></param>
        private void ShowDetails(Notification notification)
        {
            // API error ?
            if (notification is APIErrorNotification)
            {
                var window = WindowsFactory<APIErrorWindow>.ShowUnique();
                window.Notification = (APIErrorNotification)notification;
                return;
            }

            // Market orders ?
            if (notification is MarketOrdersNotification)
            {
                var ordersNotification = (MarketOrdersNotification)notification;
                var window = WindowsFactory<MarketOrdersWindow>.ShowUnique();
                window.Orders = ordersNotification.Orders;
                window.Columns = Settings.UI.MainWindow.MarketOrders.Columns;
                window.Grouping = MarketOrderGrouping.State;
                return;
            }
        }

        /// <summary>
        /// Displays the tooltip for the hovered item
        /// </summary>
        private void DisplayTooltip(Notification notification)
        {
            // No details ?
            if (!notification.HasDetails)
            {
                toolTip.Active = true;
                return;
            }

            // API error ?
            if (notification is APIErrorNotification)
            {
                var errorNotification = (APIErrorNotification)notification;
                toolTip.SetToolTip(listBox, errorNotification.Result.ErrorMessage);
                toolTip.Active = true;
                return;
            }

            // Market orders ?
            if (notification is MarketOrdersNotification)
            {
                var ordersNotification = (MarketOrdersNotification)notification;

                StringBuilder builder = new StringBuilder();
                foreach (var orderGroup in ordersNotification.Orders.GroupBy(x => x.State))
                {
                    if (builder.Length != 0) builder.AppendLine();
                    builder.AppendLine(orderGroup.Key.GetHeader());

                    foreach (var order in orderGroup)
                    {
                        int volume = order.InitialVolume;
                        var format = AbbreviationFormat.AbbreviationSymbols;

                        // Expired :    12k/15k invulnerability fields at Pator V - Tech School
                        // Fulfilled :  15k invulnerability fields at Pator V - Tech School
                        if (order.State == OrderState.Expired)
                        {
                            builder.Append(MarketOrder.Format(order.RemainingVolume, format)).Append("/");
                        }
                        builder.Append(MarketOrder.Format(order.InitialVolume, format)).Append(" ");
                        builder.Append(order.Item.Name).Append(" at ");
                        builder.AppendLine(order.Station.Name);
                    }
                }

                toolTip.SetToolTip(listBox, builder.ToString());
                toolTip.Active = true;
                return;
            }
        }

        /// <summary>
        /// When the mouse leaves the control, we "unhover" the delete icon.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void listBox_MouseLeave(object sender, EventArgs e)
        {
            if (m_hoveredIndex != -1)
            {
                m_hoveredIndex = -1;
                listBox.Invalidate();
            }
        }

        /// <summary>
        /// Gets the rectangle of the "magnifier" icon for the listbox item at the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Rectangle GetMagnifierIconRect(int index)
        {
            var rect = this.listBox.GetItemRectangle(index);
            var icon = Properties.Resources.Magnifier;
            var yOffset = (rect.Height - icon.Height) / 2;
            var magnifierIconRect = new Rectangle(rect.Right - IconMagnifierPositionFromRight, rect.Top + yOffset, icon.Width, icon.Height);
            magnifierIconRect.Inflate(2, 8);
            return magnifierIconRect;
        }

        /// <summary>
        /// Gets the rectangle of the "delete" icon for the listbox item at the given index.
        /// </summary>
        /// <param name="index"></param>
        /// <returns></returns>
        private Rectangle GetDeleteIconRect(int index)
        {
            var rect = this.listBox.GetItemRectangle(index);
            var icon = Properties.Resources.CrossBlack;
            var yOffset = (rect.Height - icon.Height) / 2;
            var deleteIconRect = new Rectangle(rect.Right - IconDeletePositionFromRight, rect.Top + yOffset, icon.Width, icon.Height);
            deleteIconRect.Inflate(2, 8);
            return deleteIconRect;
        }

        /// <summary>
        /// When the control becomes visible again, we check whether there was an update pending.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (this.Visible && m_pendingUpdate) this.UpdateContent();
            base.OnVisibleChanged(e);
        }
    }
}
