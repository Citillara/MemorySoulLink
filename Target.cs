using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace MemorySoulLink
{
    [Serializable]
    public class Target
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string HexPointer { get; set; }

        [XmlAttribute]
        public BytesSize BytesSize { get; set; }


        Int32 m_targetPointer = 0;

        BytesSize m_byteSize = BytesSize.Two;

        public void CheckIntegrity()
        {
            m_targetPointer = Helpers.ParsePointer(HexPointer, "Target HexPointer");
        }

        public void UpdateValue(Process process, long val)
        {
            Program.LockUpdates();
            switch (m_byteSize)
            {
                case BytesSize.One:
                    MemoryHelper.WriteByte(process, m_targetPointer, (byte)val);
                    break;
                case BytesSize.Two:
                    MemoryHelper.WriteShort(process, m_targetPointer, (short)val);
                    break;
                case BytesSize.Four:
                    MemoryHelper.WriteInt(process, m_targetPointer, (int)val);
                    break;
            }

            Program.UnlockUpdate(m_targetPointer);
        }
    }
}
