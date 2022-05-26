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
    public class Randomize : Action
    {
        static Random RNG = new Random();

        [XmlAttribute]
        public string TargetName { get; set; }
        [XmlAttribute]
        public int Min { get; set; }
        [XmlAttribute]
        public int Max { get; set; }
        [XmlAttribute]
        public bool UpdateLocal { get; set; }

        public override void CheckIntegrity()
        {
            if (string.IsNullOrEmpty(TargetName))
                throw new ArgumentNullException("Randomize Name cannot be null");
        }

        public override void Execute(Process p, string name, long value)
        {
            long newVal = (long)RNG.Next(Min, Max + 1);
            
            if(UpdateLocal)
                Program.Targets[TargetName].UpdateValue(p, newVal);

            Program.SendUpdate(name, newVal);
        }
    }
}
