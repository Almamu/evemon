﻿namespace EVEMon
{
    partial class MainWindowMarketOrdersList
    {
        /// <summary> 
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary> 
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.Windows.Forms.ListViewGroup listViewGroup1 = new System.Windows.Forms.ListViewGroup("Sell Orders", System.Windows.Forms.HorizontalAlignment.Left);
            System.Windows.Forms.ListViewGroup listViewGroup2 = new System.Windows.Forms.ListViewGroup("Buy Orders", System.Windows.Forms.HorizontalAlignment.Left);
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindowMarketOrdersList));
            this.listView = new System.Windows.Forms.ListView();
            this.itemColumn = new System.Windows.Forms.ColumnHeader();
            this.volumeColumn = new System.Windows.Forms.ColumnHeader();
            this.priceColumn = new System.Windows.Forms.ColumnHeader();
            this.locationColumn = new System.Windows.Forms.ColumnHeader();
            this.noOrdersLabel = new System.Windows.Forms.Label();
            this.ilListIcons = new System.Windows.Forms.ImageList(this.components);
            this.SuspendLayout();
            // 
            // listView
            // 
            this.listView.BorderStyle = System.Windows.Forms.BorderStyle.None;
            this.listView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] {
            this.itemColumn,
            this.volumeColumn,
            this.priceColumn,
            this.locationColumn});
            this.listView.Dock = System.Windows.Forms.DockStyle.Fill;
            this.listView.FullRowSelect = true;
            listViewGroup1.Header = "Sell Orders";
            listViewGroup1.Name = "sellGroup";
            listViewGroup2.Header = "Buy Orders";
            listViewGroup2.Name = "buyGroup";
            this.listView.Groups.AddRange(new System.Windows.Forms.ListViewGroup[] {
            listViewGroup1,
            listViewGroup2});
            this.listView.Location = new System.Drawing.Point(0, 0);
            this.listView.Name = "listView";
            this.listView.Size = new System.Drawing.Size(454, 434);
            this.listView.SmallImageList = this.ilListIcons;
            this.listView.TabIndex = 0;
            this.listView.UseCompatibleStateImageBehavior = false;
            this.listView.View = System.Windows.Forms.View.Details;
            // 
            // itemColumn
            // 
            this.itemColumn.Text = "Item";
            this.itemColumn.Width = 192;
            // 
            // volumeColumn
            // 
            this.volumeColumn.Text = "Volume";
            this.volumeColumn.Width = 88;
            // 
            // priceColumn
            // 
            this.priceColumn.Text = "Price";
            this.priceColumn.Width = 92;
            // 
            // locationColumn
            // 
            this.locationColumn.Text = "System";
            this.locationColumn.Width = 80;
            // 
            // noOrdersLabel
            // 
            this.noOrdersLabel.Dock = System.Windows.Forms.DockStyle.Fill;
            this.noOrdersLabel.ForeColor = System.Drawing.SystemColors.GrayText;
            this.noOrdersLabel.Location = new System.Drawing.Point(0, 0);
            this.noOrdersLabel.Name = "noOrdersLabel";
            this.noOrdersLabel.Size = new System.Drawing.Size(454, 434);
            this.noOrdersLabel.TabIndex = 1;
            this.noOrdersLabel.Text = "No market orders are available.";
            this.noOrdersLabel.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // ilListIcons
            // 
            this.ilListIcons.ImageStream = ((System.Windows.Forms.ImageListStreamer)(resources.GetObject("ilListIcons.ImageStream")));
            this.ilListIcons.TransparentColor = System.Drawing.Color.Transparent;
            this.ilListIcons.Images.SetKeyName(0, "arrow_up.png");
            this.ilListIcons.Images.SetKeyName(1, "arrow_down.png");
            this.ilListIcons.Images.SetKeyName(2, "16x16Transparant.png");
            // 
            // MainWindowMarketOrders
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.listView);
            this.Controls.Add(this.noOrdersLabel);
            this.Name = "MainWindowMarketOrders";
            this.Size = new System.Drawing.Size(454, 434);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.ListView listView;
        private System.Windows.Forms.Label noOrdersLabel;
        private System.Windows.Forms.ColumnHeader itemColumn;
        private System.Windows.Forms.ColumnHeader volumeColumn;
        private System.Windows.Forms.ColumnHeader locationColumn;
        private System.Windows.Forms.ColumnHeader priceColumn;
        private System.Windows.Forms.ImageList ilListIcons;
    }
}
