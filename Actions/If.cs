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
    public class If : Action
    {
        [XmlAttribute]
        public string HexPointer1 { get; set; }
        [XmlAttribute]
        public BytesSize ByteSize1 { get; set; }
        [XmlAttribute()]
        public string Constant1 { get; set; }

        [XmlAttribute]
        public string HexPointer2 { get; set; }
        [XmlAttribute]
        public BytesSize ByteSize2 { get; set; }
        [XmlAttribute]
        public string Constant2 { get; set; }


        [XmlAttribute]
        public Operations Operation { get; set; }


        [XmlArray("Then")]
        [XmlArrayItem("UpdateValue", Type = typeof(UpdateValue))]
        [XmlArrayItem("If", Type = typeof(If))]
        [XmlArrayItem("Randomize", Type = typeof(Randomize))]
        [XmlArrayItem("Math", Type = typeof(Math))]
        [XmlArrayItem("AssignValue", Type = typeof(AssignValue))]
        public Action[] Then { get; set; }

        [XmlArray("Else")]
        [XmlArrayItem("UpdateValue", Type = typeof(UpdateValue))]
        [XmlArrayItem("If", Type = typeof(If))]
        [XmlArrayItem("Randomize", Type = typeof(Randomize))]
        [XmlArrayItem("Math", Type = typeof(Math))]
        [XmlArrayItem("AssignValue", Type = typeof(AssignValue))]
        public Action[] Else { get; set; }

        Int32 m_targetPointer1 = 0;
        Int32 m_targetPointer2 = 0;

        long m_constant1 = 0;
        long m_constant2 = 0;

        bool m_targetPointer1Enabled = false;
        bool m_targetPointer2Enabled = false;

        delegate bool CompareMethod(long a, long b);
        CompareMethod Compare;

        public override void CheckIntegrity()
        {
            if (!string.IsNullOrEmpty(HexPointer1))
            {
                m_targetPointer1 |= Helpers.ParsePointer(HexPointer1, "If HexPointer1");
                m_targetPointer1Enabled = true;
            }
            else if (!string.IsNullOrEmpty(Constant1))
            {
                m_constant1 = long.Parse(Constant1);
            }
            else
            {
                if(!Program.DemoMode)
                    throw new ArgumentException("HexPointer1 or Constant1 must be filled");
            }


            if (!string.IsNullOrEmpty(HexPointer2))
            {
                m_targetPointer2 |= Helpers.ParsePointer(HexPointer2, "If HexPointer2");
                m_targetPointer2Enabled = true;
            }
            else if (!string.IsNullOrEmpty(Constant2))
            {
                m_constant2 = long.Parse(Constant2);
            }
            else
            {
                if (!Program.DemoMode)
                    throw new ArgumentException("HexPointer2 or Constant2 must be filled");
            }

            switch (Operation)
            {
                case Operations.Equal:
                    Compare = new CompareMethod(Equal);
                    break;
                case Operations.Above:
                    Compare = new CompareMethod(Above);
                    break;
                case Operations.AboveOrEqual:
                    Compare = new CompareMethod(AboveOrEqual);
                    break;
                case Operations.Below:
                    Compare = new CompareMethod(Below);
                    break;
                case Operations.BelowOrEqual:
                    Compare = new CompareMethod(BelowOrEqual);
                    break;
                default:
                    if (!Program.DemoMode)
                        throw new NotImplementedException("WTF");
                    break;
            }

            this.Then.ToList().ForEach(x => x.CheckIntegrity());
            this.Else.ToList().ForEach(x => x.CheckIntegrity());
        }


        public enum Operations { Equal, Above, AboveOrEqual, Below, BelowOrEqual }

        public override void Execute(Process p, string name, long value)
        {
            long val1 = m_targetPointer1Enabled ? FetchValue(p, ByteSize1, m_targetPointer1) : m_constant1;
            long val2 = m_targetPointer2Enabled ? FetchValue(p, ByteSize2, m_targetPointer2) : m_constant2;

            bool res = Compare(val1, val2);
            if (res)
            {
                foreach (Actions.Action action in Then)
                {
                    action.Execute(p, name, value);
                }
            }
            else
            {
                foreach (Actions.Action action in Else)
                {
                    action.Execute(p, name, value);
                }
            }
        }

        private long FetchValue(Process p, BytesSize b, int address)
        {
            switch (b)
            {
                case BytesSize.One: return (long)MemoryHelper.ReadByte(p, address);
                case BytesSize.Two: return (long)MemoryHelper.ReadShort(p, address);
                case BytesSize.Four: return (long)MemoryHelper.ReadInt(p, address);
                default: throw new NotImplementedException("WTF");
            }
        }

        private bool Equal(long val1, long val2)
        {
            return val1 == val2;
        }
        private bool Above(long val1, long val2)
        {
            return val1 > val2;
        }
        private bool AboveOrEqual(long val1, long val2)
        {
            return val1 >= val2;
        }
        private bool Below(long val1, long val2)
        {
            return val1 < val2;
        }
        private bool BelowOrEqual(long val1, long val2)
        {
            return val1 <= val2;
        }
    }
}
