using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MemorySoulLink.Actions
{
    [Serializable]
    public class AssignValue : Action
    {
        [XmlAttribute]
        public string TargetName { get; set; }
        [XmlAttribute]
        public bool UpdateLocal { get; set; }
        [XmlAttribute]
        public long Value { get; set; }

        public override void CheckIntegrity()
        {
            if (string.IsNullOrEmpty(TargetName))
                throw new ArgumentNullException("AssignValue TargetName cannot be null");
        }

        public override void Execute(Process p, string name, long value)
        {
            if(UpdateLocal)
                Program.Targets[TargetName].UpdateValue(p, Value);

            Program.SendUpdate(TargetName, Value);
        }
    }

}
