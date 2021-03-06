using System;
using System.Diagnostics;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Reflection;
using System.Text;
using System.Windows.Forms;
using System.Xml;
using System.Xml.Serialization;

using EVEMon.Common.Serialization;
using EVEMon.Common.Attributes;
using System.Threading;
using EVEMon.Common.Serialization.Settings;
using EVEMon.Common.Serialization.API;
using EVEMon.Common.SettingsObjects;
using EVEMon.Common.Data;

namespace EVEMon.Common
{
    /// <summary>
    /// Represents a base class for character
    /// </summary>
    [EnforceUIThreadAffinity]
    public abstract class Character : BaseCharacter
    {
        protected readonly Guid m_guid;

        protected CharacterIdentity m_identity;
        protected long m_characterID;

        // Attributes
        protected decimal m_balance;
        protected string m_name;
        protected string m_race;
        protected string m_bloodLine;
        protected string m_gender;
        protected string m_corporationName;
        protected string m_cloneName;
        protected int m_cloneSkillPoints;
        protected bool m_isUpdatingPortrait;

        // Attributes
        protected readonly Skill m_learningSkill;
        protected readonly CharacterAttribute[] m_attributes = new CharacterAttribute[5];

        // These are the new replacements for m_attributeBonuses
        protected readonly ImplantSetCollection m_implants;

        // Certificates
        protected readonly CertificateCategoryCollection m_certificateCategories;
        protected readonly CertificateClassCollection m_certificateClasses;
        protected readonly CertificateCollection m_certificates;

        // Skills
        protected readonly SkillGroupCollection m_skillGroups;
        protected readonly SkillCollection m_skills;

        // Plans
        protected readonly PlanCollection m_plans;

        // Settings
        protected CharacterUISettings m_uiSettings;


        #region Initialization
        /// <summary>
        /// Default constructor
        /// </summary>
        /// <param name="identity">The identitiy for this character</param>
        /// <param name="guid"></param>
        protected Character(CharacterIdentity identity, Guid guid)
        {
            m_characterID = identity.CharacterID;
            m_name = identity.Name;
            m_identity = identity;
            m_guid = guid;

            m_skillGroups = new SkillGroupCollection(this);
            m_skills = new SkillCollection(this);

            m_certificateCategories = new CertificateCategoryCollection(this);
            m_certificateClasses = new CertificateClassCollection(this);
            m_certificates = new CertificateCollection(this);
            m_implants = new ImplantSetCollection(this);
            m_plans = new PlanCollection(this);

            m_learningSkill = m_skills[StaticSkills.LearningSkill];
            for (int i = 0; i < m_attributes.Length; i++)
            {
                m_attributes[i] = new CharacterAttribute(this, (EveAttribute)i);
            }

            m_uiSettings = new CharacterUISettings();
        }
        #endregion


        #region Bio
        /// <summary>
        /// Gets a global identifier for this character
        /// </summary>
        public Guid Guid
        {
            get { return m_guid; }
        }

        /// <summary>
        /// Gets the identity for this character
        /// </summary>
        public CharacterIdentity Identity
        {
            get { return m_identity; }
        }

        /// <summary>
        /// Gets or sets true if the character is monitored and displayed on the main window.
        /// </summary>
        public bool Monitored
        {
            get 
            {
                return EveClient.MonitoredCharacters.Contains(this);
            }
            set
            {
                EveClient.MonitoredCharacters.OnCharacterMonitoringChanged(this, value);
            }
        }

        /// <summary>
        /// Gets the ID for this character
        /// </summary>
        public long CharacterID
        {
            get { return m_identity.CharacterID; }
        }

        /// <summary>
        /// Gets or sets the source's name. By default, it's the character's name but it may be overriden to help distinct tabs on the main window.
        /// </summary>
        public string Name
        {
            get { return m_name; }
            set
            {
                if (m_name != value)
                {
                    m_name = value;
                    EveClient.OnCharacterChanged(this);
                }
            }
        }

        /// <summary>
        /// Gets an adorned name, with (file), (url) or (cached) labels.
        /// </summary>
        public virtual string AdornedName
        {
            get { return m_name; }
        }

        /// <summary>
        /// Gets the character's race
        /// </summary>
        public string Race
        {
            get { return m_race; }
        }

        /// <summary>
        /// Gets the character's bloodline
        /// </summary>
        public string Bloodline
        {
            get { return m_bloodLine; }
        }

        /// <summary>
        /// Gets the character's gender
        /// </summary>
        public string Gender
        {
            get { return m_gender; }
        }

        /// <summary>
        /// Gets the name of the character's corporation
        /// </summary>
        public string CorporationName
        {
            get { return m_corporationName; }
        }

        /// <summary>
        /// Gets the name of the clone.
        /// </summary>
        /// <value>The name of the clone.</value>
        public string CloneName
        {
            get { return m_cloneName; }
        }

        /// <summary>
        /// Gets the clone's capacity
        /// </summary>
        /// <value>The clone skill points.</value>
        public int CloneSkillPoints
        {
            get { return m_cloneSkillPoints; }
        }

        /// <summary>
        /// Gets the current character's wallet balance
        /// </summary>
        public Decimal Balance
        {
            get { return m_balance; }
        }

        /// <summary>
        /// Gets true whether the portrait is currently updating.
        /// </summary>
        internal bool IsUpdatingPortrait
        {
            get { return m_isUpdatingPortrait; }
            set { m_isUpdatingPortrait = value; }
        }
        #endregion


        #region Attributes
        /// <summary>
        /// Gets the base attribute value (without any learning bonus or implant) for the given attribute
        /// </summary>
        /// <param name="attribute">The attribute to retrieve</param>
        /// <returns></returns>
        protected override ICharacterAttribute GetAttribute(EveAttribute attribute)
        {
            return m_attributes[(int)attribute];
        }

        /// <summary>
        /// Gets the learning factor granted by the "learning" skill.
        /// </summary>
        public float LearningFactor
        {
            get { return (100 + m_learningSkill.Level * 2) * 0.01f; }
        }
        #endregion


        #region Implants
        /// <summary>
        /// Gets the implants sets of the character and its clones
        /// </summary>
        public ImplantSetCollection ImplantSets
        {
            get { return m_implants; }
        }

        /// <summary>
        /// Gets the current implants' bonuses
        /// </summary>
        public ImplantSet CurrentImplants
        {
            get { return m_implants.Current; }
        }
        #endregion


        #region Skills
        /// <summary>
        /// Gets the collection of skills
        /// </summary>
        public SkillCollection Skills
        {
            get { return m_skills; }
        }

        /// <summary>
        /// Gets the collection of skill groups
        /// </summary>
        public SkillGroupCollection SkillGroups
        {
            get { return m_skillGroups; }
        }

        /// <summary>
        /// gets the total skill points for this character
        /// </summary>
        /// <returns></returns>
        protected override int GetTotalSkillPoints()
        {
            int sp = 0;
            foreach(var skill in m_skills) sp += skill.SkillPoints;
            return sp; 
        }

        /// <summary>
        /// Gets the number of skills this character knows
        /// </summary>
        public int KnownSkillCount
        {
            get
            {
                int count = 0;
                foreach (var skill in m_skills)
                {
                    if (skill.IsKnown) count++;
                }
                return count;
            }
        }

        /// <summary>
        /// Gets the number of skills currently known at the same level than the one specified
        /// </summary>
        /// <param name="level"></param>
        /// <returns></returns>
        public int GetSkillCountAtLevel(int level)
        {
            int count = 0;
            foreach (var skill in m_skills)
            {
                if (skill.LastConfirmedLvl == level) count++;
            }
            return count;
        }

        /// <summary>
        /// Gets the level of the given skill
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        public override int GetSkillLevel(StaticSkill skill)
        {
            return m_skills[skill].Level;
        }

        /// <summary>
        /// Gets the level of the given skill
        /// </summary>
        /// <param name="skill"></param>
        /// <returns></returns>
        public override int GetSkillPoints(StaticSkill skill)
        {
            return m_skills[skill].SkillPoints;
        }
        #endregion


        #region Certificates
        /// <summary>
        /// Gets the collection of certificate categories.
        /// </summary>
        public CertificateCategoryCollection CertificateCategories
        {
            get { return m_certificateCategories; }
        }

        /// <summary>
        /// Gets the collection of certificate classes.
        /// </summary>
        public CertificateClassCollection CertificateClasses
        {
            get { return m_certificateClasses; }
        }

        /// <summary>
        /// Gets the collection of certificates.
        /// </summary>
        public CertificateCollection Certificates
        {
            get { return m_certificates; }
        }
        #endregion


        #region Plans
        /// <summary>
        /// Gets the collection of plans.
        /// </summary>
        public PlanCollection Plans
        {
            get { return m_plans; }
        }
        #endregion


        #region Training
        /// <summary>
        /// Gets true when the character is currently training, false otherwise
        /// </summary>
        public virtual bool IsTraining
        {
            get { return false; }
        }

        /// <summary>
        /// Gets the skill currently in training
        /// </summary>
        public virtual QueuedSkill CurrentlyTrainingSkill
        {
            get { return null; }
        }
        #endregion


        #region Importation / exportation
        /// <summary>
        /// Create a serializable character sheet for this character
        /// </summary>
        /// <returns></returns>
        public abstract SerializableSettingsCharacter Export();


        /// <summary>
        /// Fetches the data to the given serialization object, used by inheritors.
        /// </summary>
        /// <param name="ci"></param>
        protected void Export(SerializableSettingsCharacter serial)
        {
            serial.Guid = m_guid;
            serial.ID = m_identity.CharacterID;
            serial.Name = m_name;
            serial.Race = m_race;
            serial.Gender = m_gender;
            serial.BloodLine = m_bloodLine;
            serial.CorporationName = m_corporationName;
            serial.CloneSkillPoints = m_cloneSkillPoints;
            serial.CloneName = m_cloneName;
            serial.Balance = m_balance;

            // Attributes
            serial.Attributes.Intelligence = this.Intelligence.Base;
            serial.Attributes.Perception = this.Perception.Base;
            serial.Attributes.Willpower = this.Willpower.Base;
            serial.Attributes.Charisma = this.Charisma.Base;
            serial.Attributes.Memory = this.Memory.Base;

            // Implants sets
            serial.ImplantSets = this.ImplantSets.Export();

            // Certificates
            serial.Certificates = new List<SerializableCharacterCertificate>();
            foreach(var cert in this.Certificates)
            {
                if (cert.IsGranted) 
                {
                    serial.Certificates.Add(new SerializableCharacterCertificate{ CertificateID = cert.ID });
                }
            }

            // Skills
            serial.Skills = new List<SerializableCharacterSkill>();
            foreach(var skill in this.Skills)
            {
                if (skill.IsKnown || skill.IsOwned) serial.Skills.Add(skill.Export());
            }
        }

        /// <summary>
        /// Imports data from the given character sheet informations
        /// </summary>
        /// <param name="serial">The serialized character sheet</param>
        internal void Import(APIResult<SerializableAPICharacter> serial)
        {
            if (!serial.HasError)
            {
                Import(serial.Result);
                EveClient.OnCharacterChanged(this);
            }
        }

        /// <summary>
        /// Imports data from the given character sheet informations
        /// </summary>
        /// <param name="serial">The serialized character sheet</param>
        protected void Import(SerializableSettingsCharacter serial)
        {
            Import((SerializableCharacterBase)serial);

            // Implants
            m_implants.Import(serial.ImplantSets);
        }

        /// <summary>
        /// Imports data from the given character sheet informations
        /// </summary>
        /// <param name="serial">The serialized character sheet</param>
        protected void Import(SerializableAPICharacter serial)
        {
            Import((SerializableCharacterBase)serial);

            // Implants
            m_implants.Import(serial.Implants);

            // Clean the obsolete entries on plans.
            CleanObsoleteEntries();
        }

        /// <summary>
        /// Imports data from the given character sheet informations
        /// </summary>
        /// <param name="serial">The serialized character sheet</param>
        protected void Import(SerializableCharacterBase serial)
        {
            bool fromCCP = (serial is SerializableAPICharacter);

            // Bio
            m_name = serial.Name;
            m_race = serial.Race;
            m_gender = serial.Gender;
            m_balance = serial.Balance;
            m_bloodLine = serial.BloodLine;
            m_corporationName = serial.CorporationName;
            m_cloneName = serial.CloneName;
            m_cloneSkillPoints = serial.CloneSkillPoints;

            // Attributes
            m_attributes[(int)EveAttribute.Intelligence].Base = serial.Attributes.Intelligence;
            m_attributes[(int)EveAttribute.Perception].Base = serial.Attributes.Perception;
            m_attributes[(int)EveAttribute.Willpower].Base = serial.Attributes.Willpower;
            m_attributes[(int)EveAttribute.Charisma].Base = serial.Attributes.Charisma;
            m_attributes[(int)EveAttribute.Memory].Base = serial.Attributes.Memory;

            // Skills : reset all > update all
            foreach (var skill in m_skills) skill.Reset(fromCCP);
            foreach (var serialSkill in serial.Skills)
            {
                // Take care of the new skills not in our datafiles yet. Update if it exists.
                var foundSkill = m_skills[serialSkill.ID];
                if (foundSkill != null) foundSkill.Import(serialSkill, fromCCP);
            }

            // Certificates : reset > mark the granted ones > update the other ones
            foreach (var cert in m_certificates) cert.Reset();
            foreach (var serialCert in serial.Certificates)
            {
                // Take care of the new certs not in our datafiles yet. Mark as granted if it exists.
                var foundCert = m_certificates[serialCert.CertificateID];
                if (foundCert != null) foundCert.MarkAsGranted();
            }

            while (true)
            {
                bool updatedAnything = false;
                foreach (var cert in m_certificates)
                {
                    updatedAnything |= cert.TryUpdateCertificateStatus();
                }
                if (!updatedAnything) break;
            }

            // Not from CCP ?
            if (!fromCCP)
            {
                SerializableSettingsCharacter nonCCPSerial = (SerializableSettingsCharacter)serial;
            }
        }

        /// <summary>
        /// Imports the given plans
        /// </summary>
        /// <param name="plans"></param>
        internal void ImportPlans(IEnumerable<SerializablePlan> plans)
        {
            m_plans.Import(plans);
            CleanObsoleteEntries();
        }

        /// <summary>
        /// Export the plans to the given list
        /// </summary>
        /// <param name="list"></param>
        internal void ExportPlans(List<SerializablePlan> list)
        {
            foreach (var plan in m_plans)
            {
                list.Add(plan.Export());
            }
        }
        #endregion


        /// <summary>
        /// Gets the UI settings for this character.
        /// </summary>
        public CharacterUISettings UISettings
        {
            get { return m_uiSettings; }
            internal set { m_uiSettings = value; }
        }

        /// <summary>
        /// Updates the character on a timer tick
        /// </summary>
        internal virtual void UpdateOnOneSecondTick()
        {

        }

        /// <summary>
        /// Clean the obsolete entries in the plan.
        /// </summary>
        internal void CleanObsoleteEntries()
        {
            foreach (var plan in m_plans)
            {
                plan.CleanObsoleteEntries();
            }
        }

        /// <summary>
        /// Gets a unique hascode for this character.
        /// </summary>
        /// <returns></returns>
        public override int GetHashCode()
        {
            return m_guid.GetHashCode();
        }

        /// <summary>
        /// Gets the name fo the character.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return m_name;
        }
    }
}
