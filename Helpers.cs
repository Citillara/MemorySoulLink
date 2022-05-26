using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MemorySoulLink
{
    internal class Helpers
    {
        public static int ParsePointer(string value, string name)
        {
            Int32 oInt = 0;

            if (Program.DemoMode)
                return 123456;
            // Target pointer
            if (!int.TryParse(value,
                NumberStyles.HexNumber,
                CultureInfo.CurrentCulture, out oInt))
            {
                throw new ArgumentException("Error parsing hex pointer " + name);

            }
            return oInt;
        }

    }
}
