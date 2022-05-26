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
        public int Pollspeed { get; set; }

        [XmlArray("Targets")]
        public Target[] Targets { get; set; }

        [XmlArray("TrackedValues")]
        public TrackedValue[] TrackedValues { get; set; }

        public void CheckIntegrity()
        {
            if (string.IsNullOrEmpty(ProcessName))
                throw new ArgumentNullException("TrackedValue ProcessName cannot be null");
            if (string.IsNullOrEmpty(UserName))
                throw new ArgumentNullException("UserName Name cannot be null");
            if (string.IsNullOrEmpty(Host))
                throw new ArgumentNullException("Host Name cannot be null");

            if (HostPort < 1 && HostPort > 0xFFFF)
                throw new ArgumentOutOfRangeException("Host port must be between 1 and 65535");

            if (string.IsNullOrEmpty(SessionID))
                throw new ArgumentNullException("SessionID Name cannot be null");

            if (Pollspeed < 1)
                throw new ArgumentOutOfRangeException("Pollspeed must be above 1");

            Targets.ToList().ForEach(t => t.CheckIntegrity());
            TrackedValues.ToList().ForEach(t => t.CheckIntegrity());
        }
    }
}
