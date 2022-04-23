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

        [XmlElement(IsNullable = true)]
        public MemoryLine[] MemoryLines { get; set; }

        [XmlElement(IsNullable = false)]
        public int PollSpped { get; set; }

        [XmlArray("Targets")]
        public Target[] Targets { get; set; }

        [XmlArray("TrackedValues")]
        public TrackedValue[] TrackedValues { get; set; }
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


    [Serializable]
    public class Target
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string HexPointer { get; set; }

        [XmlAttribute]
        public BytesSize BytesSize { get; set; }
    }

    [Serializable]
    [XmlInclude(typeof(UpdateValue))]
    [XmlInclude(typeof(If))]
    [XmlInclude(typeof(Randomize))]
    public class Action
    {

    }

    [Serializable]
    public class TrackedValue
    {
        [XmlAttribute]
        public string HexPointer { get; set; }

        [XmlAttribute]
        public BytesSize BytesSize { get; set; }
        
        [XmlArray("Actions")]
        [XmlArrayItem("UpdateValue", Type = typeof(UpdateValue))]
        [XmlArrayItem("If", Type = typeof(If))]
        [XmlArrayItem("Randomize", Type = typeof(Randomize))]
        public Action[] Actions { get; set; }
    }


    [Serializable]
    public class UpdateValue : Action
    {
        [XmlAttribute]
        public string TargetName { get; set; }
    }

    [Serializable]
    public class Randomize : Action
    {
        [XmlAttribute]
        public string TargetName { get; set; }
        [XmlAttribute]
        public long Min { get; set; }
        [XmlAttribute]
        public long Max { get; set; }
    }


    [Serializable]
    public class If : Action
    {
        [XmlAttribute]
        public string HexPointer1 { get; set; }
        [XmlAttribute()]
        public string Constant1 { get; set; }
        [XmlAttribute]
        public string HexPointer2 { get; set; }
        [XmlAttribute]
        public string Constant2 { get; set; }
        [XmlAttribute]
        public string Operation { get; set; }


        [XmlArray("Then")]
        [XmlArrayItem("UpdateValue", Type = typeof(UpdateValue))]
        [XmlArrayItem("If", Type = typeof(If))]
        [XmlArrayItem("Randomize", Type = typeof(Randomize))]
        public Action[] Then { get; set; }

        [XmlArray("Else")]
        [XmlArrayItem("UpdateValue", Type = typeof(UpdateValue))]
        [XmlArrayItem("If", Type = typeof(If))]
        [XmlArrayItem("Randomize", Type = typeof(Randomize))]
        public Action[] Else { get; set; }
    }
}
