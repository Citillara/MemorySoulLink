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
    public class Math : Action
    {
        [XmlAttribute]
        public string TargetName { get; set; }

        [XmlAttribute]
        public Computations Operation { get; set; }

        [XmlAttribute]
        public long Value { get; set; }

        [XmlAttribute]
        public bool UpdateLocal { get; set; }

        public enum Computations { Addition, Substraction, Multiplication, Division }

        delegate long ComputeMethod(long value);
        ComputeMethod Compute;


        public override void CheckIntegrity()
        {
            if (string.IsNullOrEmpty(TargetName))
                throw new ArgumentNullException("Math TargetName cannot be null");

            switch (Operation)
            {
                case Computations.Addition:
                    Compute = new ComputeMethod(Add);
                    break;
                case Computations.Substraction:
                    Compute = new ComputeMethod(Substract);
                    break;
                case Computations.Multiplication:
                    Compute = new ComputeMethod(Multiplication);
                    break;
                case Computations.Division:
                    Compute = new ComputeMethod(Division);
                    break;
                default: throw new NotImplementedException("WTF");
            }
        }

        public override void Execute(Process p, string name, long value)
        {
            long newVal = Compute(value);

            if (UpdateLocal)
                Program.Targets[TargetName].UpdateValue(p, newVal);

            Program.SendUpdate(TargetName, newVal);
        }

        long Add(long value)
        {
            return value + Value;
        }

        long Substract(long value)
        {
            return value - Value;
        }

        long Multiplication(long value)
        {
            return value * Value;
        }

        long Division(long value)
        {
            return value / Value;
        }
    }
}
