using MemorySoulLink.Actions;
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
    public class TrackedValue
    {
        [XmlAttribute]
        public string Name { get; set; }

        [XmlAttribute]
        public string HexPointer { get; set; }

        [XmlAttribute]
        public BytesSize BytesSize { get; set; }


        [XmlArray("Actions")]
        [XmlArrayItem("UpdateValue", Type = typeof(UpdateValue))]
        [XmlArrayItem("If", Type = typeof(If))]
        [XmlArrayItem("Randomize", Type = typeof(Randomize))]
        public MemorySoulLink.Actions.Action[] Actions { get; set; }

        [XmlIgnore]
        public Int32 TargetPointer { get { return m_targetPointer; } }

        Int32 m_targetPointer = 0;

        byte m_bOldVal;
        short m_sOldVal;
        int m_iOldVal;


        BytesSize m_byteSize = BytesSize.Two;


        public void CheckIntegrity()
        {
            if(string.IsNullOrEmpty(Name))
                throw new ArgumentNullException("TrackedValue Name cannot be null");
            m_targetPointer = Helpers.ParsePointer(HexPointer, "TrackedValue HexPointer");
            
        }

        public bool CheckIfChanged(Process p, out long newVal)
        {

            newVal = 0;
            if (p == null)
            {
                newVal = 0;
                return false;
            }


            byte m_bCurVal = 0;
            short m_sCurVal = 0;
            int m_iCurVal = 0;

            switch (m_byteSize)
            {
                case BytesSize.One: m_bCurVal = MemoryHelper.ReadByte(p, m_targetPointer); break;
                case BytesSize.Two: m_sCurVal = MemoryHelper.ReadShort(p, m_targetPointer); break;
                case BytesSize.Four: m_iCurVal = MemoryHelper.ReadInt(p, m_targetPointer); break;
                default: throw new NotImplementedException("WTF");
            }

            switch (m_byteSize)
            {
                case BytesSize.One:
                    if (m_bCurVal != m_bOldVal)
                    {
                        m_bOldVal = m_bCurVal;
                        newVal = m_bCurVal;
                        return true;
                    }
                    break;
                case BytesSize.Two:
                    if (m_sCurVal != m_sOldVal)
                    {
                        m_sOldVal = m_sCurVal;
                        newVal = m_sCurVal;
                        return true;
                    }
                    break;

                case BytesSize.Four:
                    if (m_iCurVal != m_iOldVal)
                    {
                        m_iOldVal = m_iCurVal;
                        newVal = m_iCurVal;
                        return true;
                    }
                    break;
                default: throw new NotImplementedException("WTF");
            }


            return false;
        }

        Process m_preparedProcess = null;
        string m_preparedName = null;
        long m_preparedValue = 0;

        public void PrepareExecute(Process p, string name, long value)
        {
            m_preparedName = name;
            m_preparedValue = value;
            m_preparedProcess = p;
        }

        public void ExecuteActions()
        {
            foreach (Actions.Action a in Actions)
            {
                a.Execute(m_preparedProcess, m_preparedName, m_preparedValue);
            }
        }
    }
}
