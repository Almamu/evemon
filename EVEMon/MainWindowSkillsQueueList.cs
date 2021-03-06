﻿using System;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using EVEMon.Common;
using EVEMon.SkillPlanner;

namespace EVEMon
{
    public partial class MainWindowSkillsQueueList : UserControl
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
        private const int MinimumHeight = 39;

        // Skills drawing - Font & brushes
        private readonly Font m_skillsQueueFont;
        private readonly Font m_boldSkillsQueueFont;

        private CCPCharacter ccpCharacter;
        private Object m_lastTooltipItem;
        private Character m_character;
        private QueuedSkill item;
        private Skill m_skill;
        private QueuedSkill[] m_skillQueue;

        private bool m_requireRefresh;
        private int count = 0;
        
        /// <summary>
        /// Constructor
        /// </summary>
        public MainWindowSkillsQueueList()
        {
            InitializeComponent();

            noSkillsQueueLabel.Visible = true;
            lbSkillsQueue.Visible = false;

            m_skillsQueueFont = FontFactory.GetFont("Tahoma", 8.25F);
            m_boldSkillsQueueFont = FontFactory.GetFont("Tahoma", 8.25F, FontStyle.Bold);
            noSkillsQueueLabel.Font = FontFactory.GetFont("Tahoma", 11.25F, FontStyle.Bold);

            EveClient.CharacterChanged += new EventHandler<CharacterChangedEventArgs>(EveClient_CharacterChanged);
            EveClient.QueuedSkillsCompleted += new EventHandler<QueuedSkillsEventArgs>(EveClient_QueuedSkillsCompleted);
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
            EveClient.QueuedSkillsCompleted -= new EventHandler<QueuedSkillsEventArgs>(EveClient_QueuedSkillsCompleted);
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
        /// Gets the item's height.
        /// </summary>
        private int GetItemHeight
        {
            get
            {
                return Math.Max((m_skillsQueueFont.Height * 2) + PadTop + LineVPad + PadTop + LowerBoxHeight, MinimumHeight);
            }
        }
        
        #region Inherited events
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
        #endregion
        
        
        #region Display update
        /// <summary>
        /// Updates all the content
        /// </summary>
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
                noSkillsQueueLabel.Visible = true;
                lbSkillsQueue.Visible = false;
                lbSkillsQueue.Items.Clear();
                return;
            }
            
            ccpCharacter = m_character as CCPCharacter;
            
            // When the skill queue hasn't changed don't do anything
            if (!QueueHasChanged(ccpCharacter.SkillQueue.ToArray()))
            {
                return;
            }

            m_skillQueue = ccpCharacter.SkillQueue.ToArray();

            // Update the skills queue list
            lbSkillsQueue.BeginUpdate();
            try
            {
                // Add items in the list
                lbSkillsQueue.Items.Clear();
                foreach (var skill in ccpCharacter.SkillQueue)
                {
                    lbSkillsQueue.Items.Add(skill);
                }

                // Display or hide the "no queue skills" label.
                noSkillsQueueLabel.Visible = ccpCharacter.SkillQueue.IsEmpty();
                lbSkillsQueue.Visible = !ccpCharacter.SkillQueue.IsEmpty();

                // Invalidate display
                lbSkillsQueue.Invalidate();
            }
            finally
            {
                lbSkillsQueue.EndUpdate();
                m_requireRefresh = false;
            }
        }

        private bool QueueHasChanged(QueuedSkill[] queue)
        {
            if (m_skillQueue == null)
                return true;
            
            if (queue.Length != m_skillQueue.Length)
                return true;

            for (var i = 0; i < queue.Length; i++)
            {
                if (queue[i] != m_skillQueue[i])
                    return true;
            }

            return false;
        }
        #endregion


        #region Drawing
        /// <summary>
        /// Handles the DrawItem event of the lbSkillsQueue control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.DrawItemEventArgs"/> instance containing the event data.</param>
        private void lbSkillsQueue_DrawItem(object sender, DrawItemEventArgs e)
        {
            if (e.Index < 0) return;
            item = lbSkillsQueue.Items[e.Index] as QueuedSkill;
            DrawItem(item, e);
        }

        /// <summary>
        /// Handles the MeasureItem event of the lbSkillsQueue control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MeasureItemEventArgs"/> instance containing the event data.</param>
        private void lbSkillsQueue_MeasureItem(object sender, MeasureItemEventArgs e)
        {
            if (e.Index < 0) return;
            e.ItemHeight = GetItemHeight;
        }

        /// <summary>
        /// Draws the list item for the given skill
        /// </summary>
        /// <param name="skill"></param>
        /// <param name="e"></param>
        public void DrawItem(QueuedSkill skill, DrawItemEventArgs e)
        {
            Graphics g = e.Graphics;

            // Draw background
            if ((e.Index % 2) == 0)
            {
                // Not in training - odd
                g.FillRectangle(Brushes.LightGray, e.Bounds);
            }
            else
            {
                // Not in training - even
                g.FillRectangle(Brushes.White, e.Bounds);
            }


            // Measure texts
            TextFormatFlags format = TextFormatFlags.NoPadding | TextFormatFlags.NoClipping;

            float percentComplete = 0;
            int skillPoints = skill.Skill.SkillPoints;
            int skillPointsToNextLevel = skill.Skill.StaticData.GetPointsRequiredForLevel(Math.Min(skill.Level, 5));
            if (skill.Level == skill.Skill.LastConfirmedLvl + 1) percentComplete = skill.FractionCompleted;
            if (skill.Level > skill.Skill.LastConfirmedLvl + 1) skillPoints = skill.CurrentSP;
            string rankText = String.Format(CultureInfo.CurrentCulture, " (Rank {0})", skill.Skill.Rank);
            string spText = String.Format(CultureInfo.CurrentCulture, "SP: {0:#,##0}/{1:#,##0}", skillPoints, skillPointsToNextLevel);
            string levelText = String.Format(CultureInfo.CurrentCulture, "Level {0}", skill.Level);
            string pctText = String.Format(CultureInfo.CurrentCulture, "{0:0}% Done", Math.Floor(percentComplete * 100));

            Size skillNameSize = TextRenderer.MeasureText(g, skill.Skill.Name, m_boldSkillsQueueFont, Size.Empty, format);
            Size levelTextSize = TextRenderer.MeasureText(g, levelText, m_skillsQueueFont, Size.Empty, format);
            Size pctTextSize = TextRenderer.MeasureText(g, pctText, m_skillsQueueFont, Size.Empty, format);


            // Draw texts
            Color highlightColor = Color.Black;

            TextRenderer.DrawText(g, skill.Skill.Name, m_boldSkillsQueueFont, new Point(e.Bounds.Left + PadLeft, e.Bounds.Top + PadTop), highlightColor);
            TextRenderer.DrawText(g, rankText, m_skillsQueueFont, new Point(e.Bounds.Left + PadLeft + skillNameSize.Width, e.Bounds.Top + PadTop), highlightColor);
            TextRenderer.DrawText(g, spText, m_skillsQueueFont,
                new Point(e.Bounds.Left + PadLeft, e.Bounds.Top + PadTop + skillNameSize.Height + LineVPad), highlightColor);


            // Boxes
            g.DrawRectangle(Pens.Black, new Rectangle(e.Bounds.Right - BoxWidth - PadRight, e.Bounds.Top + PadTop, BoxWidth, BoxHeight));

            int levelBoxWidth = (BoxWidth - 4 - 3) / 5;
            for (int level = 1; level <= 5; level++)
            {
                Rectangle brect = new Rectangle(e.Bounds.Right - BoxWidth - PadRight + 2 + (levelBoxWidth * (level - 1)) + (level - 1),
                                  e.Bounds.Top + PadTop + 2, levelBoxWidth, BoxHeight - 3);

                if (level <= skill.Skill.LastConfirmedLvl)
                {
                    g.FillRectangle(Brushes.Black, brect);
                }
                else
                {
                    g.FillRectangle(Brushes.DarkGray, brect);
                }

                // Color indicator for a queued level
                SkillQueue skillQueue = ccpCharacter.SkillQueue;
                foreach (var qskill in skillQueue)
                {
                    if ((!skill.Skill.IsTraining && skill == qskill && level == qskill.Level)
                       || (skill == qskill && level <= qskill.Level && level > skill.Skill.LastConfirmedLvl && percentComplete == 0.0))
                                                                                                     
                    {
                        g.FillRectangle(Brushes.RoyalBlue, brect);
                    }
                    
                    // Blinking indicator of skill in training level
                    if (skill.Skill.IsTraining && skill == qskill && level == skill.Level && percentComplete > 0.0)
                    {
                        if (count == 0) g.FillRectangle(Brushes.White, brect);
                        if (count == 1) count = -1;
                        count++;
                    }
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
            TextRenderer.DrawText(g, levelText, m_skillsQueueFont, new Point(
                                      e.Bounds.Right - BoxWidth - PadRight - BoxHPad - levelTextSize.Width,
                                      e.Bounds.Top + PadTop), Color.Black);

            TextRenderer.DrawText(g, pctText, m_skillsQueueFont,
                                  new Point(e.Bounds.Right - BoxWidth - PadRight - BoxHPad - pctTextSize.Width,
                                            e.Bounds.Top + PadTop + levelTextSize.Height + LineVPad), Color.Black);

            // Draw skill queue color bar
            Rectangle qBarRect = new Rectangle(e.Bounds.Left, GetItemHeight - LowerBoxHeight, e.Bounds.Width, LowerBoxHeight);
            g.FillRectangle(Brushes.DimGray, qBarRect);
            Rectangle skillRect = GetSkillRect(item, qBarRect.Width, LowerBoxHeight - 1);
            g.FillRectangle(Brushes.CornflowerBlue,
                new Rectangle(skillRect.X, GetItemHeight - LowerBoxHeight, skillRect.Width, skillRect.Height));
        }

        /// <summary>
        /// Gets the preferred size from the preferred size of the skills list.
        /// </summary>
        /// <param name="proposedSize"></param>
        /// <returns></returns>
        public override Size GetPreferredSize(Size proposedSize)
        {
            return lbSkillsQueue.GetPreferredSize(proposedSize);
        }

        /// <summary>
        /// Gets the rectangle a skill rendes in within a specified
        /// rectange.
        /// </summary>
        /// <param name="skill">Skill that exists within the queue</param>
        /// <param name="width">Width of the canvas</param>
        /// <param name="height">Height of the canvas</param>
        /// <returns>
        /// Rectangle representing the area within the visual
        /// queue the skill occupies.
        /// </returns>
        private Rectangle GetSkillRect(QueuedSkill skill, int width, int height)
        {
            TimeSpan relativeStart = skill.StartTime - DateTime.UtcNow;
            TimeSpan relativeFinish = skill.EndTime - DateTime.UtcNow;

            int TotalSeconds = (int)TimeSpan.FromHours(24).TotalSeconds;

            double Start = Math.Floor((relativeStart.TotalSeconds / TotalSeconds) * width);
            double Finish = Math.Floor((relativeFinish.TotalSeconds / TotalSeconds) * width);

            // if the start time is before now set it to zero
            if (Start < 0)
            {
                Start = 0;
            }

            return new Rectangle((int)Start, 0, (int)(Finish - Start), height);
        }
        #endregion


        #region Local events
        /// <summary>
        /// Handles the MouseWheel event of the lbSkillsQueue control.
        /// </summary>
        /// <param name="sender">The source of the event.</param>
        /// <param name="e">The <see cref="System.Windows.Forms.MouseEventArgs"/> instance containing the event data.</param>
        private void lbSkillsQueue_MouseWheel(object sender, MouseEventArgs e)
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
                    if (lbSkillsQueue.TopIndex - i >= 0)
                    {
                        // Retrieve the next top item
                        item = lbSkillsQueue.Items[lbSkillsQueue.TopIndex - i];
                    }
                }
                // Going down
                else
                {
                    // Compute the height of the items from current the topindex (included)
                    int height = 0;
                    for (int j = lbSkillsQueue.TopIndex + i - 1; j < lbSkillsQueue.Items.Count; j++)
                    {
                        height += GetItemHeight;
                    }

                    // Retrieve the next bottom item
                    if (height > lbSkillsQueue.ClientSize.Height)
                    {
                        item = lbSkillsQueue.Items[lbSkillsQueue.TopIndex + i - 1];
                    }
                }

                // If found a new item as top or bottom
                if (item != null)
                {
                    numberOfPixelsToMove[i - 1] = GetItemHeight * direction;
                }
                else
                {
                    lines -= direction;
                }
            }

            // Scroll 
            if (lines != 0)
            {
                lbSkillsQueue.Invalidate();
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
            int index = lbSkillsQueue.IndexFromPoint(e.X, e.Y);
            if (index < 0 || index >= lbSkillsQueue.Items.Count) return;

            // Beware, this last index may actually means a click in the whitespace at the bottom
            // Let's deal with this special case
            if (index == lbSkillsQueue.Items.Count - 1)
            {
                Rectangle itemRect = lbSkillsQueue.GetItemRectangle(index);
                if (!itemRect.Contains(e.Location)) return;
            }

            // Right click for skills below lv5 : we display a context menu to plan higher levels.
            item = lbSkillsQueue.Items[index] as QueuedSkill;
            m_skill = item.Skill;
            if (e.Button == MouseButtons.Right && m_skill.Level < 5)
            {
                // Reset the menu.
                ToolStripMenuItem tm = new ToolStripMenuItem(String.Format(CultureInfo.CurrentCulture, "Add {0}", m_skill.Name));

                // Build the level options.
                int nextLevel = Math.Min(5, m_skill.Level + 1);
                for (int level = nextLevel; level < 6; level++)
                {
                    ToolStripMenuItem menuLevel = new ToolStripMenuItem(String.Format(CultureInfo.CurrentCulture, "Level {0} to", Skill.GetRomanForInt(level)));
                    tm.DropDownItems.Add(menuLevel);
                    foreach (var plan in m_character.Plans)
                    {
                        ToolStripMenuItem menuPlanItem = new ToolStripMenuItem(plan.Name);
                        menuPlanItem.Click += new EventHandler(menuPlanItem_Click);
                        menuPlanItem.Tag = new Pair<Plan, SkillLevel>(plan, new SkillLevel(m_skill, level));
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
            DisplayTooltip(item);
        }

        /// <summary>
        /// On mouse move, we hide the tooltip.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void lbSkills_MouseMove(object sender, MouseEventArgs e)
        {
            for (int i = 0; i < lbSkillsQueue.Items.Count; i++)
            {
                // Skip until we found the mouse location
                var rect = lbSkillsQueue.GetItemRectangle(i);
                if (!rect.Contains(e.Location)) continue;

                // Updates the tooltip
                item = lbSkillsQueue.Items[i] as QueuedSkill;
                DisplayTooltip(item);
                return;
            }

            // If we went so far, we're not over anything.
            m_lastTooltipItem = null;
            ttToolTip.Active = false;
        }

        /// <summary>
        /// Displays the tooltip for the given skill.
        /// </summary>
        /// <param name="item"></param>
        private void DisplayTooltip(QueuedSkill item)
        {
            if (ttToolTip.Active && m_lastTooltipItem == item) return;
            m_lastTooltipItem = item;

            ttToolTip.Active = false;
            ttToolTip.SetToolTip(lbSkillsQueue, GetTooltip(item));
            ttToolTip.Active = true;
        }

        /// <summary>
        /// Gets the tooltip text for the given skill
        /// </summary>
        /// <param name="s"></param>
        private string GetTooltip(QueuedSkill skill)
        {
            int sp = skill.Skill.SkillPoints;
            int nextLevel = Math.Min(5, skill.Level);
            float percentDone = 0;
            if (skill.Level == skill.Skill.LastConfirmedLvl + 1) percentDone = skill.FractionCompleted;
            if (skill.Level > skill.Skill.LastConfirmedLvl + 1) sp = skill.CurrentSP;
            int nextLevelSP = skill.Skill.StaticData.GetPointsRequiredForLevel(nextLevel);
            int pointsLeft = nextLevelSP - sp;

            string remainingTimeText = Skill.TimeSpanToDescriptiveText(skill.Skill.GetTimeSpanForPoints(pointsLeft), DescriptiveTextOptions.IncludeCommas | DescriptiveTextOptions.UppercaseText);
            
            if (sp < skill.Skill.StaticData.GetPointsRequiredForLevel(1))
            {
                // Training hasn't got past level 1 yet
                StringBuilder untrainedToolTip = new StringBuilder();
                untrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Not yet trained to Level I ({0}%)\n", Math.Floor(percentDone * 100));
                untrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Next level I: {0:#,##0} skill points remaining\n", pointsLeft);
                untrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time remaining: {0}", remainingTimeText);
                AddSkillBoilerPlate(untrainedToolTip, skill.Skill);
                return untrainedToolTip.ToString();
            }

            // So, it's a left click on a skill, we display the tool tip
            // Currently training skill?
            if (skill.Skill.IsTraining && percentDone != 0)
            {
                StringBuilder partiallyTrainedToolTip = new StringBuilder();
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Partially Completed ({0}%)\n", Math.Floor(percentDone * 100));
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training to level {0}: {1:#,##0} skill points remaining\n", Skill.GetRomanForInt(nextLevel), pointsLeft);
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time remaining: {0}", remainingTimeText);
                AddSkillBoilerPlate(partiallyTrainedToolTip, skill.Skill);
                return partiallyTrainedToolTip.ToString();
            }

            // Currently training skill but next queued level?
            if (skill.Skill.IsTraining && percentDone == 0)
            {
                StringBuilder partiallyTrainedToolTip = new StringBuilder();
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Previous level not yet completed\n");
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Queued to level {0}: {1:#,##0} skill points remaining\n", Skill.GetRomanForInt(nextLevel), pointsLeft);
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time to next level: {0}", remainingTimeText);
                AddSkillBoilerPlate(partiallyTrainedToolTip, skill.Skill);
                return partiallyTrainedToolTip.ToString();
            }

            // Partially trained skill and not in training?
            if (skill.Skill.IsPartiallyTrained && !skill.Skill.IsTraining)
            {
                StringBuilder partiallyTrainedToolTip = new StringBuilder();
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Partially Completed ({0}%)\n", Math.Floor(percentDone * 100));
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Queued to level {0}: {1:#,##0} skill points remaining\n", Skill.GetRomanForInt(nextLevel), pointsLeft);
                partiallyTrainedToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time remaining: {0}", remainingTimeText);
                AddSkillBoilerPlate(partiallyTrainedToolTip, skill.Skill);
                return partiallyTrainedToolTip.ToString();
            }

            // We've completed all the skill points for the current level
            if (!skill.Skill.IsPartiallyTrained)
            {
                if (skill.Level != 5)
                {
                    StringBuilder levelCompleteToolTip = new StringBuilder();
                    levelCompleteToolTip.AppendFormat(CultureInfo.CurrentCulture, "Completed Level {0}: {1:#,##0}/{2:#,##0}\n",
                                         Skill.GetRomanForInt(skill.Level - 1), sp, nextLevelSP);
                    levelCompleteToolTip.AppendFormat(CultureInfo.CurrentCulture, "Queued level {0}: {1:#,##0} skill points required\n",
                                         Skill.GetRomanForInt(nextLevel), pointsLeft);
                    levelCompleteToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time to next level: {0}", remainingTimeText);
                    AddSkillBoilerPlate(levelCompleteToolTip, skill.Skill);
                    return levelCompleteToolTip.ToString();
                }
            }

            // Error in calculating SkillPoints
            StringBuilder calculationErrorToolTip = new StringBuilder();
            calculationErrorToolTip.AppendLine("Partially Trained (Could not cacluate all skill details)");
            calculationErrorToolTip.AppendFormat(CultureInfo.CurrentCulture, "Next level {0}: {1:#,##0} skill points remaining\n", nextLevel, pointsLeft);
            calculationErrorToolTip.AppendFormat(CultureInfo.CurrentCulture, "Training time remaining: {0}", remainingTimeText);
            AddSkillBoilerPlate(calculationErrorToolTip, skill.Skill);
            return calculationErrorToolTip.ToString();
        }
        
        private static void AddSkillBoilerPlate(StringBuilder toolTip, Skill skill)
        {
            toolTip.Append("\n\n");
            toolTip.AppendLine(skill.DescriptionNL);
            toolTip.AppendFormat(CultureInfo.CurrentCulture, "\nPrimary: {0}, ", skill.PrimaryAttribute);
            toolTip.AppendFormat(CultureInfo.CurrentCulture, "Secondary: {0} ", skill.SecondaryAttribute);
            toolTip.AppendFormat(CultureInfo.CurrentCulture, "({0:#,##0} SP/hour)", skill.SkillPointsPerHour);
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
                if (training == null) return;

                // Invalidate the skill row
                int index = lbSkillsQueue.Items.IndexOf(training);
                if (index >= 0)
                {
                    lbSkillsQueue.Invalidate(lbSkillsQueue.GetItemRectangle(index));
                }

                UpdateContent();
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
        /// When the queue changed, we refresh the content
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        void EveClient_QueuedSkillsCompleted(object sender, QueuedSkillsEventArgs e)
        {
            UpdateContent();
        }

        /// <summary>
        /// When the settings changed, we refresh the content
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