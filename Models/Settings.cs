using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace MemorySoulLink.Models
{
    [Serializable, XmlRoot(IsNullable = false)]
    public class Settings
    {

        [XmlElement(IsNullable = false)]
        public string ProcessName { get; set; }

        [XmlElement(IsNullable = false)]
        public string UserName { get; set; }

        [XmlElement(IsNullable = false)]
        public string Host { get; set; }

        [XmlElement(IsNullable = false)]
        public int HostPort { get; set; }

        [XmlElement(IsNullable = false)]
        public bool HostTLS { get; set; }

        [XmlElement(IsNullable = false)]
        public string SessionID { get; set; }

        [XmlElement(IsNullable = false)]
        public MemoryLine[] MemoryLines { get; set; }

        [XmlElement(IsNullable = false)]
        public int PollSpped { get; set; }

    }

    [Serializable]
    public class MemoryLine
    {
        [XmlElement(IsNullable = false)]
        public string Name { get; set; }

        [XmlElement(IsNullable = false)]
        public string HexPointerTargetValue { get; set; }

        [XmlElement(IsNullable = false)]
        public int NumberOfBytes { get; set; }

        [XmlElement(IsNullable = false)]
        public string HexPointerAboveValueCondition { get; set; }

        [XmlElement(IsNullable = false)]
        public long AboveValueConditionValue { get; set; }

        [XmlElement(IsNullable = false)]
        public int AboveNumberOfBytes { get; set; }

        [XmlElement(IsNullable = false)]
        public string HexPointerBelowValueCondition { get; set; }

        [XmlElement(IsNullable = false)]
        public long BelowValueConditionValue { get; set; }

        [XmlElement(IsNullable = false)]
        public int BelowNumberOfBytes { get; set; }

        [XmlElement(IsNullable = false)]
        public string HexPointerEqualValueCondition { get; set; }

        [XmlElement(IsNullable = false)]
        public long EqualValueConditionValue { get; set; }

        [XmlElement(IsNullable = false)]
        public int EqualNumberOfBytes { get; set; }
    }


}
