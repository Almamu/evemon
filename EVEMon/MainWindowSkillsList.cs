﻿using System;
using System.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Reflection;
using System.Diagnostics;
using System.Globalization;
using EVEMon.Common;
using EVEMon.Common.Controls;
using EVEMon.SkillPlanner;

namespace EVEMon
{
    public partial class MainWindowSkillsList : UserControl
    {
        // Skills drawing - Region & text padding
        private const int PadTop = 2;
        private const int PadLeft = 6;
        private const int PadRight = 7;
        private const int LineVPad = 0;

        // Skills drawing - Boxes
        private const int BoxWidth = 57;
        private const int BoxHeight = 14;
        private const int LowerBoxHeight = 8;
        private const int BoxHPad = 6;
        private const int BoxVPad = 2;
        private const int SkillDetailHeight = 31;

        // Skills drawing - Skills groups
        private const int SkillHeaderHeight = 21;
        private const int CollapserPadRight = 6;

        // Skills drawing - Font & brushes
        private readonly Font m_skillsFont;
        private readonly Font m_boldSkillsFont;

        private Character m_character;
        private Object m_lastTooltipItem;
        private bool m_requireRefresh;
        private int count = 0;


        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindowSkillsList()
        {
            InitializeComponent();
            noSkillsLabel.Visible = true;
            lbSkills.Visible = false;

            m_skillsFont = FontFactory.GetFont("Tahoma", 8.25F);
            m_boldSkillsFont = FontFactory.GetFont("Tahoma", 8.25F, FontStyle.Bold);
            noSkillsLabel.Font = FontFactory.GetFont("Tahoma", 11.25F, FontStyle.Bold);

            EveClient.CharacterChanged += new EventHandler<CharacterChangedEventArgs>(EveClient_CharacterChanged);
            EveClient.SettingsChanged += new EventHandler(EveClient_SettingsChanged);
            EveClient.TimerTick += new EventHandler(EveClient_TimerTick);
            this.Disposed += new EventHandler(OnDisposed);
        }

        /// <summary>
        /// Unsubscribe events on disposing.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnDisposed(object sender, EventArgs e)
        {
            EveClient.CharacterChanged -= new EventHandler<CharacterChangedEventArgs>(EveClient_CharacterChanged);
            EveClient.SettingsChanged -= new EventHandler(EveClient_SettingsChanged);
            EveClient.TimerTick -= new EventHandler(EveClient_TimerTick);
            this.Disposed -= new EventHandler(OnDisposed);
        }

        /// <summary>
        /// Gets the character associated with this monitor.
        /// </summary>
        public Character Character
        {
            get { return m_character; }
            set 
            { 
                m_character = value;
                UpdateContent();
            }
        }

        /// <summary>
        /// On load, we update the content
        /// </summary>
        /// <param name="e"></param>
        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);
            UpdateContent();
        }

        /// <summary>
        /// When the control becomes visible again, checks whether a refresh is required.
        /// </summary>
        /// <param name="e"></param>
        protected override void OnVisibleChanged(EventArgs e)
        {
            if (this.Visible && m_requireRefresh) UpdateContent();
            base.OnVisibleChanged(e);
        }

        /// <summary>
        /// Updates all the content
        /// </summary>
        /// <remarks>Another high-complexity method for us to look at.</remarks>
        /// <param name="sender">The sender.</param>
        /// <param name="e">The <see cref="EVEMon.Common.SkillChangedEventArgs"/> instance containing the event data.</param>
        private void UpdateContent()
        {
            // Returns if not visible
            if (!this.Visible)
            {
                m_requireRefresh = true;
                return;
            }

            // When no character, we just clear the list
            if (m_character == null)
            {
                m_requireRefresh = false;
                noSkillsLabel.Visible = true;
                lbSkills.Visible = false;
                lbSkills.Items.Clear();
                return;
            }

            // Update the skills list
            lbSkills.BeginUpdate();
            try
            {
                IEnumerable<Skill> skills = m_character.Skills;
                if (Settings.UI.MainWindow.ShowPrereqMetSkills) skills = skills.Where(x => (x.ArePrerequisitesMet && x.IsPublic) || x.IsKnown);
                else
                {
                    if (Settings.UI.MainWindow.ShowNonPublicSkills) skills = m_character.Skills;
                    else if (Settings.UI.MainWindow.ShowAllPublicSkills) skills = skills.Where(x => x.IsPublic || x.IsKnown);
                    else skills = skills.Where(x => x.IsKnown);
                }
                var groups = skills.GroupBy(x => x.Group).ToArray().OrderBy(x => x.Key.Name);

                // Scroll through groups
                lbSkills.Items.Clear();
                foreach (var group in groups)
                {
                    lbSkills.Items.Add(group.Key);

                    // Add items in the group when it's not collapsed
                    if (!m_character.UISettings.CollapsedGroups.Contains(group.Key.Name))
                    {
                        foreach (var skill in group.ToArray().OrderBy(x => x.Name))
                        {
                            lbSkills.Items.Add(skill);
                        }
                    }
                }

                // Display or hide the "no skills" label.
                noSkillsLabel.Visible = skills.IsEmpty();
                lbSkills.Visible = !skills.IsEmpty();

                // Invalidate display
                lbSkills.Invalidate();
            }
            finally
            {
                lbSkills.EndUpdate();
                m_requireRefresh = false;
            }
        }

        #region Drawing
        /// <summary>
        /// Handles the DrawItem event of the lbSkills control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.DrawItemEventArgs"/> instance containing the event data.</param>
        private void lbSkills_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;

            object item = lbSkills.Items[e.Index];
            if (item is SkillGroup) DrawItem(item as SkillGroup, e);
            else if (item is Skill) DrawItem(item as Skill, e);
        }

        /// <summary>
        /// Handles the MeasureItem event of the lbSkills control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MeasureItemEventArgs"/> instance containing the event data.</param>
        private void lbSkills_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.ItemHeight = GetItemHeight(lbSkills.Items[e.Index]);
        }

        /// <summary>
        /// Gets the item's height.
        /// </summary>
        /// <param name="e"></param>
        /// <param name="item"></param>
        private int GetItemHeight(object item)
        {
            if (item is SkillGroup)
            {
                return SkillHeaderHeight;
            }

            return Math.Max(m_skillsFont.Height * 2 + PadTop + LineVPad + PadTop, SkillDetailHeight);
        }

        /// <summary>
        /// Draws the list item for the given skill
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="e"></param>
        public void DrawItem(Skill skill, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;

            // Draw background
            if (skill.IsTraining)
            {
                // In training
                g.FillRectangle(Brushes.LightSteelBlue, e.Bounds);
            }
            else if ((e.Index % 2) == 0)
            {
                // Not in training - odd
                g.FillRectangle(Brushes.White, e.Bounds);
            }
            else
            {
                // Not in training - even
                g.FillRectangle(Brushes.LightGray, e.Bounds);
            }


            // Measure texts
            TextFormatFlags format = TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;

            int skillLevel = skill.Level;
            int skillPointsToNextLevel = skill.StaticData.GetPointsRequiredForLevel(Math.Min(skill.Level + 1, 5));
            for (int i = 0; skill.SkillPoints >= skillPointsToNextLevel && skillLevel < 5; i++ )
            {
                skillLevel++;
                skillPointsToNextLevel = skill.StaticData.GetPointsRequiredForLevel(Math.Min(skillLevel + 1, 5));
            }

            float percentComplete = skill.FractionCompleted;
            for (int i = 0; percentComplete >= 1; i++)
            {
                percentComplete--;
            }

            string rankText = String.Format(CultureInfo.CurrentCulture, " (Rank {0})", skill.Rank);
            string spText = String.Format(CultureInfo.CurrentCulture, "SP: {0:#,##0}/{1:#,##0}", skill.SkillPoints, skillPointsToNextLevel);
            string levelText = String.Format(CultureInfo.CurrentCulture, "Level {0}", skillLevel);
            string pctText = String.Format(CultureInfo.CurrentCulture, "{0:0}% Done", Math.Floor(percentComplete * 100));

            Size skillNameSize = TextRenderer.MeasureText(g, skill.Name, m_boldSkillsFont, Size.Empty, format);
            Size levelTextSize = TextRenderer.MeasureText(g, levelText, m_skillsFont, Size.Empty, format);
            Size pctTextSize = TextRenderer.MeasureText(g, pctText, m_skillsFont, Size.Empty, format);


            // Draw texts
            Color highlightColor = Color.Black;
            if (!skill.IsKnown) highlightColor = Color.Red;
            if (!skill.IsPublic) highlightColor = Color.DarkRed;
            if (skill.ArePrerequisitesMet && skill.IsPublic && !skill.IsKnown) highlightColor = Color.SlateGray;
            if (Settings.UI.MainWindow.HighlightPartialSkills && skill.IsPartiallyTrained && !skill.IsTraining) highlightColor = Color.Green;
            if (Settings.UI.MainWindow.HighlightQueuedSkills && skill.IsQueued && !skill.IsTraining) highlightColor = Color.RoyalBlue;

            TextRenderer.DrawText(g, skill.Name, m_boldSkillsFont, new Point(e.Bounds.Left + PadLeft, e.Bounds.Top + PadTop), highlightColor);
            TextRenderer.DrawText(g, rankText, m_skillsFont, new Point(e.Bounds.Left + PadLeft + skillNameSize.Width, e.Bounds.Top + PadTop), highlightColor);
            TextRenderer.DrawText(g, spText, m_skillsFont,
                new Point(e.Bounds.Left + PadLeft, e.Bounds.Top + PadTop + skillNameSize.Height + LineVPad), highlightColor);


            // Boxes
            g.DrawRectangle(Pens.Black, new Rectangle(e.Bounds.Right - BoxWidth - PadRight, e.Bounds.Top + PadTop, BoxWidth, BoxHeight));
            
            int levelBoxWidth = (BoxWidth - 4 - 3) / 5;
            for (int level = 1; level <= 5; level++)
            {
                Rectangle brect = new Rectangle(e.Bounds.Right - BoxWidth - PadRight + 2 + (levelBoxWidth * (level - 1)) + (level - 1),
                                  e.Bounds.Top + PadTop + 2, levelBoxWidth, BoxHeight - 3);

                if (level <= skillLevel)
                {
                    g.FillRectangle(Brushes.Black, brect);
                }
                else
                {
                    g.FillRectangle(Brushes.DarkGray, brect);
                }

                // Color indicator for a queued level
                CCPCharacter ccpCharacter = m_character as CCPCharacter;
                if (ccpCharacter != null)
                {
                    SkillQueue skillQueue = ccpCharacter.SkillQueue;
                    foreach (var qskill in skillQueue)
                    {
                        if ((!skill.IsTraining && skill == qskill.Skill && level == qskill.Level)
                           || (skill.IsTraining && skill == qskill.Skill && level == qskill.Level && level > skillLevel + 1))
                        {
                            g.FillRectangle(Brushes.RoyalBlue, brect);
                        }
                    }
                }
                
                // Blinking indicator of skill in training level
                if (skill.IsTraining && level == skillLevel + 1)
                {
                    if (count == 0) g.FillRectangle(Brushes.White, brect);
                    if (count == 1) count = -1;
                    count ++;
                }
           }

            // Draw progression bar
            g.DrawRectangle(Pens.Black,
                new Rectangle(e.Bounds.Right - BoxWidth - PadRight, e.Bounds.Top + PadTop + BoxHeight + BoxVPad, BoxWidth, LowerBoxHeight));

            Rectangle pctBarRect = new Rectangle(e.Bounds.Right - BoxWidth - PadRight + 2,
                                                 e.Bounds.Top + PadTop + BoxHeight + BoxVPad + 2,
                                                 BoxWidth - 3, LowerBoxHeight - 3);

            g.FillRectangle(Brushes.DarkGray, pctBarRect);
            int fillWidth = (int)(pctBarRect.Width * percentComplete);
            if (fillWidth > 0)
            {
                Rectangle fillRect = new Rectangle(pctBarRect.X, pctBarRect.Y, fillWidth, pctBarRect.Height);
                g.FillRectangle(Brushes.Black, fillRect);
            }


            // Draw level and percent texts
            TextRenderer.DrawText(g, levelText, m_skillsFont, new Point(
                                      e.Bounds.Right - BoxWidth - PadRight - BoxHPad - levelTextSize.Width,
                                      e.Bounds.Top + PadTop), Color.Black);

            TextRenderer.DrawText(g, pctText, m_skillsFont,
                                  new Point(e.Bounds.Right - BoxWidth - PadRight - BoxHPad - pctTextSize.Width,
                                            e.Bounds.Top + PadTop + levelTextSize.Height + LineVPad), Color.Black);
        }

        /// <summary>
        /// Draws the list item for the given skill group.
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="e"></param>
        public void DrawItem(SkillGroup group, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;

            // Draws the background
            using (Brush b = new SolidBrush(Color.FromArgb(75, 75, 75)))
            {
                g.FillRectangle(b, e.Bounds);
            }
            using (Pen p = new Pen(Color.FromArgb(100, 100, 100)))
            {
                g.DrawLine(p, e.Bounds.Left, e.Bounds.Top, e.Bounds.Right + 1, e.Bounds.Top);
            }

            // Draw the header
            Size titleSizeInt = TextRenderer.MeasureText(g, group.Name, m_boldSkillsFont, Size.Empty, TextFormatFlags.NoPadding | TextFormatFlags.NoClipping);
            Point titleTopLeftInt = new Point(e.Bounds.Left + 3, e.Bounds.Top + ((e.Bounds.Height / 2) - (titleSizeInt.Height / 2)));
            Point detailTopLeftInt = new Point(titleTopLeftInt.X + titleSizeInt.Width, titleTopLeftInt.Y);

            string skillInTrainingSuffix = "";
            bool hasTrainingSkill = group.Any(x => x.IsTraining);
            bool hasQueuedSkill = group.Any(x=> x.IsQueued && !x.IsTraining);
            if (hasTrainingSkill) skillInTrainingSuffix = ", ( 1 in training )";
            if (hasQueuedSkill) skillInTrainingSuffix += String.Format(", ( {0} in queue )", group.Count(x=> x.IsQueued && !x.IsTraining));

            // Draws the rest of the text header
            string detailText = String.Format(CultureInfo.CurrentCulture,
                                              ", {0} of {1} skills, {2:#,##0} Points{3}",
                                              group.Count(x => x.IsKnown),
                                              group.Count(x => x.IsPublic),
                                              group.TotalSP,
                                              skillInTrainingSuffix);

            TextRenderer.DrawText(g, group.Name, m_boldSkillsFont, titleTopLeftInt, Color.White);
            TextRenderer.DrawText(g, detailText, m_skillsFont, detailTopLeftInt, Color.White);

            // Draws the collapsing arrows
            bool isCollapsed = m_character.UISettings.CollapsedGroups.Contains(group.Name);
            Image i = (isCollapsed ? Properties.Resources.Expand : Properties.Resources.Collapse);

            g.DrawImageUnscaled(i, new Point(e.Bounds.Right - i.Width - CollapserPadRight,
                                             (SkillHeaderHeight / 2) - (i.Height / 2) + e.Bounds.Top));
        }

        /// <summary>
        /// Gets the preferred size from the preferred size of the skills list.
        /// </summary>
        /// <param name="proposedSize"></param>
        /// <returns></returns>
        public override Size GetPreferredSize(Size proposedSize)
        {
            return lbSkills.GetPreferredSize(proposedSize);
        }
        #endregion


        #region Toggle / expand
        /// <summary>
        /// Toggles all the skill groups to collapse or open.
        /// </summary>
        public void ToggleAll()
        {
            // When at least one group collapsed, expand all
            if (m_character.UISettings.CollapsedGroups.Count != 0)
            {
                m_character.UISettings.CollapsedGroups.Clear();
            }
            // When none collapsed, collapse all
            else
            {
                foreach (var group in m_character.SkillGroups)
                {
                    m_character.UISettings.CollapsedGroups.Add(group.Name);
                }
            }

            // Update the list
            UpdateContent();
        }

        /// <summary>
        /// Toggles the expansion or collapsing of a single group
        /// </summary>
        /// <param name="group">The group to expand or collapse.</param>
        public void ToggleGroupExpandCollapse(SkillGroup group)
        {
            if (m_character.UISettings.CollapsedGroups.Contains(group.Name))
            {
                ExpandSkillGroup(group);
            }
            else
            {
                CollapseSkillGroup(group);
            }
        }

        /// <summary>
        /// Expand a skill group
        /// </summary>
        /// <param name="group">Skill group in lbSkills</param>
        public void ExpandSkillGroup(SkillGroup group)
        {
            m_character.UISettings.CollapsedGroups.Remove(group.Name);
            UpdateContent();
        }

        /// <summary>
        /// Collapse a skill group
        /// </summary>
        /// <param name="group">Skill group in lbSkills</param>
        public void CollapseSkillGroup(SkillGroup group)
        {
            m_character.UISettings.CollapsedGroups.Add(group.Name);
            UpdateContent();
        }
        #endregion


        #region Local events
        /// <summary>
        /// Handles the MouseWheel event of the lbSkills control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void lbSkills_MouseWheel(object sender, MouseEventArgs e)
        {
            // Update the drawing based upon the mouse wheel scrolling.
            int numberOfItemLinesToMove = e.Delta * SystemInformation.MouseWheelScrollLines / 120;
            int lines = numberOfItemLinesToMove;
            if (lines == 0) return;

            // Compute the number of lines to move
            int direction = lines / Math.Abs(lines);
            int[] numberOfPixelsToMove = new int[lines * direction];
            for (int i = 1; i <= Math.Abs(lines); i++)
            {
                object item = null;

                // Going up
                if (direction == Math.Abs(direction))
                {
                    if (lbSkills.TopIndex - i >= 0)
                    {
                        // Retrieve the next top item
                        item = lbSkills.Items[lbSkills.TopIndex - i];
                    }
                }
                // Going down
                else
                {
                    // Compute the height of the items from current the topindex (included)
                    int height = 0; 
                    for (int j = lbSkills.TopIndex + i - 1; j < lbSkills.Items.Count; j++)
                    {
                        height += GetItemHeight(lbSkills.Items[j]);
                    }

                    // Retrieve the next bottom item
                    if (height > lbSkills.ClientSize.Height)
                    {
                        item = lbSkills.Items[lbSkills.TopIndex + i - 1];
                    }
                }

                // If found a new item as top or bottom
                if (item != null)
                {
                    numberOfPixelsToMove[i - 1] = GetItemHeight(item) * direction;
                }
                else
                {
                    lines -= direction;
                }
            }

            // Scroll 
            if (lines != 0)
            {
                lbSkills.Invalidate();
            }
        }

        /// <summary>
        /// On a mouse down event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void lbSkills_MouseDown(object sender, MouseEventArgs e)
        {
            // Retrieve the item at the given point and quit if none
            int index = lbSkills.IndexFromPoint(e.X, e.Y);
            if (index < 0 || index >= lbSkills.Items.Count) return;

            // Beware, this last index may actually means a click in the whitespace at the bottom
            // Let's deal with this special case
            if (index == lbSkills.Items.Count - 1)
            {
                Rectangle itemRect = lbSkills.GetItemRectangle(index);
                if (!itemRect.Contains(e.Location)) return;
            }

            // For a skill group, we have to handle the collapse/expand mechanism and the tooltip
            Object item = lbSkills.Items[index];
            if (item is SkillGroup)
            {
                // Left button : expand/collapse
                SkillGroup sg = (SkillGroup)item;
                if (e.Button != MouseButtons.Right)
                {
                    ToggleGroupExpandCollapse(sg);
                    return;
                }

                // If right click on the button, still expand/collapse
                Rectangle itemRect = lbSkills.GetItemRectangle(lbSkills.Items.IndexOf(item));
                Rectangle buttonRect = GetButtonRectangle(sg, itemRect);
                if (buttonRect.Contains(e.Location))
                {
                    ToggleGroupExpandCollapse(sg);
                    return;
                }

                // Regular right click, display the tooltip
                DisplayTooltip(sg);
                return;
            }

            // Right click for skills below lv5 : we display a context menu to plan higher levels.
            Skill s = (Skill)item;
            if (e.Button == MouseButtons.Right && s.Level < 5)
            {
                // Reset the menu.
                ToolStripMenuItem tm = new ToolStripMenuItem(String.Format(CultureInfo.CurrentCulture, "Add {0}", s.Name));

                // Build the level options.
                int nextLevel = Math.Min(5, s.Level + 1);
                for (int level = nextLevel; level < 6; level++)
                {
                    ToolStripMenuItem menuLevel = new ToolStripMenuItem(String.Format(CultureInfo.CurrentCulture, "Level {0} to", Skill.GetRomanForInt(level)));
                    tm.DropDownItems.Add(menuLevel);
                    foreach (var plan in m_character.Plans)
                    {
                        ToolStripMenuItem menuPlanItem = new ToolStripMenuItem(plan.Name);
                        menuPlanItem.Click += new EventHandler(menuPlanItem_Click);
                        menuPlanItem.Tag = new Pair<Plan, SkillLevel>(plan, new SkillLevel(s, level));
                        menuLevel.DropDownItems.Add(menuPlanItem);
                    }
                }

                // Add to the context menu and display
                contextMenuStripPlanPopup.Items.Clear();
                contextMenuStripPlanPopup.Items.Add(tm);
                contextMenuStripPlanPopup.Show((Control)sender, new Point(e.X, e.Y));
                return;
            }

            // Non-right click or already lv5, display the tooltip
            DisplayTooltip(s);
        }

        /// <summary>
        /// On mouse move, we hide the tooltip.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void lbSkills_MouseMove(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < lbSkills.Items.Count; i++)
            {
                // Skip until we found the mouse location
                var rect = lbSkills.GetItemRectangle(i);
                if (!rect.Contains(e.Location)) continue;

                // Updates the tooltip
                Object item = lbSkills.Items[i];
                DisplayTooltip(item);
                return;
            }

            // If we went so far, we're not over anything.
            m_lastTooltipItem = null;
            ttToolTip.Active = false;
        }

        /// <summary>
        /// Displays the tooltip for the given item (skill or skillgroup).
        /// </summary>
        /// <param name="item"></param>
        private void DisplayTooltip(Object item)
        {
            if (ttToolTip.Active && m_lastTooltipItem == item) return;
            m_lastTooltipItem = item;

            ttToolTip.Active = false;
            if (item is SkillGroup)
            {
                ttToolTip.SetToolTip(lbSkills, GetTooltip(item as SkillGroup));
            }
            else
            {
                ttToolTip.SetToolTip(lbSkills, GetTooltip(item as Skill));
            }
            ttToolTip.Active = true;
        }

        /// <summary>
        /// Gets the tooltip text for the given skill
        /// </summary>
        /// <param name="s"></param>
        private string GetTooltip(Skill s)
        {
            int sp = s.SkillPoints;
            int nextLevel = Math.Min(5, s.Level + 1);
            float percentDone = s.FractionCompleted;
            int nextLevelSP = s.StaticData.GetPointsRequiredForLevel(nextLevel);
            int pointsLeft = s.GetLeftPointsRequiredToLevel(nextLevel);

            string remainingTimeText = Skill.TimeSpanToDescriptiveText(s.GetLeftTrainingTimeToLevel(nextLevel), DescriptiveTextOptions.IncludeCommas | DescriptiveTextOptions.UppercaseText);

            if (sp < s.StaticData.GetPointsRequiredForLevel(1))
            {
                // Training hasn't got past level 1 yet
                StringBuilder untrainedToolTip = new StringBuilder();
                untrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Not yet trained to Level I ({0}%)\n", Math.Floor(percentDone * 100));
                untrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Next level I: {0:#,##0} skill points remaining\n", pointsLeft);
                untrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time remaining: {0}", remainingTimeText);
                AddSkillBoilerPlate(untrainedToolTip, s);
                return untrainedToolTip.ToString();
            }

            // So, it's a left click on a skill, we display the tool tip
            // Partially trained skill ?
            if (s.IsPartiallyTrained)
            {
                StringBuilder partiallyTrainedToolTip = new StringBuilder();
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Partially Completed ({0}%)\n", Math.Floor(percentDone * 100));
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training to level {0}: {1:#,##0} skill points remaining\n", Skill.GetRomanForInt(nextLevel), pointsLeft);
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time remaining: {0}", remainingTimeText);
                AddSkillBoilerPlate(partiallyTrainedToolTip, s);
                return partiallyTrainedToolTip.ToString();
            }

            // We've completed all the skill points for the current level
            if (!s.IsPartiallyTrained)
            {
                if (s.Level != 5)
                {
                    StringBuilder levelCompleteToolTip = new StringBuilder();
                    levelCompleteToolTip.AppendFormat(CultureInfo.CurrentCulture, "Completed Level {0}: {1:#,##0}/{2:#,##0}\n",
                                         Skill.GetRomanForInt(s.Level), sp, nextLevelSP);
                    levelCompleteToolTip.AppendFormat(CultureInfo.CurrentCulture, "Next level {0}: {1:#,##0} skill points required\n",
                                         Skill.GetRomanForInt(nextLevel), pointsLeft);
                    levelCompleteToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training Time: {0}", remainingTimeText);
                    AddSkillBoilerPlate(levelCompleteToolTip, s);
                    return levelCompleteToolTip.ToString();
                }

                // Lv 5 completed
                StringBuilder lv5ToolTip = new StringBuilder();
                lv5ToolTip.AppendFormat(CultureInfo.CurrentCulture, "Level V Complete: {0:#,##0}/{1:#,##0}\n", sp, nextLevelSP);
                lv5ToolTip.Append("No further training required\n");
                AddSkillBoilerPlate(lv5ToolTip, s);
                return lv5ToolTip.ToString();
            }

            // Error in calculating SkillPoints
            StringBuilder calculationErrorToolTip = new StringBuilder();
            calculationErrorToolTip.AppendLine("Partially Trained (Could not cacluate all skill details)");
            calculationErrorToolTip.AppendFormat(CultureInfo.CurrentCulture, "Next level {0}: {1:#,##0} skill points remaining\n",nextLevel, pointsLeft);
            calculationErrorToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time remaining: {0}", remainingTimeText);
            AddSkillBoilerPlate(calculationErrorToolTip, s);
            return calculationErrorToolTip.ToString();
        }

        private static void AddSkillBoilerPlate(StringBuilder toolTip, Skill s)
        {
            toolTip.Append("\n\n");
            toolTip.AppendLine(s.DescriptionNL);
            toolTip.AppendFormat(CultureInfo.CurrentCulture, "\nPrimary: {0}, ", s.PrimaryAttribute);
            toolTip.AppendFormat(CultureInfo.CurrentCulture, "Secondary: {0} ", s.SecondaryAttribute);
            toolTip.AppendFormat(CultureInfo.CurrentCulture, "({0:#,##0} SP/hour)", s.SkillPointsPerHour);
        }

        /// <summary>
        /// Gets the tooltip content for the given skill group
        /// </summary>
        /// <param name="sg"></param>
        private string GetTooltip(SkillGroup group)
        {
            int totalValidSP = 0;
            int maxSP = 0;
            int totalSP = 0;
            int known = 0;
            int maxKnown = 0;
            double percentDonePoints = 0.0;
            double percentDoneSkills = 0.0;

            // Maximas are computed on public skills only
            foreach (Skill s in group.Where(x => x.IsPublic))
            {
                totalValidSP += s.SkillPoints;
                maxSP += s.StaticData.GetPointsRequiredForLevel(5);
                maxKnown++;
            }

            // Current achievements are computed on every skill, including non-public
            foreach (Skill s in group)
            {
                totalSP += s.SkillPoints;
                if (s.IsKnown) known++;
            }

            // If the group is not completed yet
            if (totalValidSP < maxSP)
            {
                percentDonePoints = (1.0 * Math.Min(totalSP, maxSP)) / maxSP;
                percentDoneSkills = (1.0 * Math.Min(known, maxKnown)) / maxKnown;

                return String.Format(CultureInfo.CurrentCulture, "Points Completed: {0:#,##0} of {1:#,##0} ({2:P1})\nSkills Known: {3} of {4} ({5:P0})",
                                     totalSP, maxSP, percentDonePoints, known, maxKnown, percentDoneSkills);
            }

            // The group has been completed !
            return String.Format(CultureInfo.CurrentCulture, "Skill Group completed: {0:#,##0}/{1:#,##0} (100%)\nSkills: {2:#}/{3:#} (100%)",
                                 totalSP, maxSP, known, maxKnown);
        }

        /// <summary>
        /// Gets the rectangle for the collapse/expand button.
        /// </summary>
        /// <param name="itemRect"></param>
        /// <returns></returns>
        public Rectangle GetButtonRectangle(SkillGroup group, Rectangle itemRect)
        {
            // Checks whether this group is collapsed
            bool isCollapsed = m_character.UISettings.CollapsedGroups.Contains(group.Name);

            // Get the image for this state
            Image btnImage = (isCollapsed ? btnImage = Properties.Resources.Expand : btnImage = Properties.Resources.Collapse);

            // Compute the top left point
            Point btnPoint = new Point(itemRect.Right - btnImage.Width - CollapserPadRight,
                                       (SkillHeaderHeight / 2) - (btnImage.Height / 2) + itemRect.Top);

            return new Rectangle(btnPoint, btnImage.Size);
        }

        /// <summary>
        /// Handler for a plan item click on the plan's context menu.
        /// Add a skill to the plan.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void menuPlanItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem planItem = (ToolStripMenuItem)sender;
            var tag = (Pair<Plan, SkillLevel>)planItem.Tag;

            var operation = tag.A.TryPlanTo(tag.B.Skill, tag.B.Level);
            PlanHelper.PerformSilently(operation);
        }
        #endregion


        #region Global events
        /// <summary>
        /// On timer tick, we invalidate the training skill display
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void EveClient_TimerTick(object sender, EventArgs e)
        {
            if (m_character.IsTraining && this.Visible)
            {
                // Retrieves the trained skill for update but quit if the skill is null (was not in our datafiles)
                var training = m_character.CurrentlyTrainingSkill;
                if (training.Skill == null) return;

                // Invalidate the skill row
                int index = lbSkills.Items.IndexOf(training.Skill);
                if (index >= 0)
                {
                    lbSkills.Invalidate(lbSkills.GetItemRectangle(index));
                }

                // invalidate the skill group row
                int groupIndex = lbSkills.Items.IndexOf(training.Skill.Group);
                if (groupIndex >= 0)
                {
                    lbSkills.Invalidate(lbSkills.GetItemRectangle(groupIndex));
                }
            }
        }

        /// <summary>
        /// When the character changed, we refresh the content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void EveClient_CharacterChanged(object sender, CharacterChangedEventArgs e)
        {
            if (e.Character != m_character) return;
            UpdateContent();
        }

        /// <summary>
        /// When the settings changed, we refresh the content (show all public skills, non-public skills, etc)
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void EveClient_SettingsChanged(object sender, EventArgs e)
        {
            UpdateContent();
        }
        #endregion

    }
}
