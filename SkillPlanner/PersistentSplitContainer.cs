using System;
using System.Collections;
using System.Windows.Forms;
using EVEMon.Common;

namespace EVEMon.SkillPlanner
{
    public class PersistentSplitContainer : SplitContainer
    {
        public PersistentSplitContainer()
            : base()
        {
        }
        
        private string m_rememberDistanceKey;

        public string RememberDistanceKey
        {
            get { return m_rememberDistanceKey; }
            set { m_rememberDistanceKey = value; }
        }

        protected override void OnCreateControl()
        {
            base.OnCreateControl();

            if (!String.IsNullOrEmpty(m_rememberDistanceKey))
            {
                Settings s = Settings.GetInstance();
                if (s.SavedSplitterDistances.ContainsKey(m_rememberDistanceKey))
                {
                    int d = s.SavedSplitterDistances[m_rememberDistanceKey];
                    d = this.VerifyValidSplitterDistance(d);
                    this.SplitterDistance = d;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (!String.IsNullOrEmpty(m_rememberDistanceKey))
            {
                Settings s = Settings.GetInstance();
                int d = this.SplitterDistance;
                if (VerifyValidSplitterDistance(d) == d)
                {
                    s.SavedSplitterDistances[m_rememberDistanceKey] = d;
                }
            }

            base.Dispose(disposing);
        }

        private int VerifyValidSplitterDistance(int d)
        {
            int defaultDistance = this.Width / 4;

            if ((d < this.Panel1MinSize) || (d + this.Panel2MinSize > this.Width))
                return defaultDistance;
            else
                return d;
        }
    }
}