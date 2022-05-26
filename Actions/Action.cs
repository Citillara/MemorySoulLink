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
    [XmlInclude(typeof(UpdateValue))]
    [XmlInclude(typeof(If))]
    [XmlInclude(typeof(Randomize))]
    public abstract class Action
    {
        public abstract void Execute(Process p, string name, long value);

        public abstract void CheckIntegrity();
    }
}
