﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace EVEMon.Common.Serialization.API
{
    /// <summary>
    /// Represents a skill
    /// </summary>
    public sealed class SerializableCharacterSkill
    {
        [XmlAttribute("typeID")]
        public int ID
        {
            get;
            set;
        }

        [XmlAttribute("level")]
        public int Level
        {
            get;
            set;
        }

        [XmlAttribute("skillpoints")]
        public int Skillpoints
        {
            get;
            set;
        }

        [XmlAttribute("ownsBook")]
        public bool OwnsBook
        {
            get;
            set;
        }

        [XmlAttribute("isKnown")]
        public bool IsKnown
        {
            get;
            set;
        }
    }
}
