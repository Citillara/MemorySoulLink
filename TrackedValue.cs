using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MemorySoulLink
{
    internal class TrackedValue
    {
        [DllImport("kernel32.dll")]
        static extern IntPtr OpenProcess(ProcessAccessFlags dwDesiredAccess, [MarshalAs(UnmanagedType.Bool)] bool bInheritHandle, int dwProcessId);

        [DllImport("kernel32.dll", SetLastError = true)]
        static extern bool WriteProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, byte[] lpBuffer, uint nSize, out int lpNumberOfBytesWritten);

        [DllImport("kernel32.dll")]
        static extern bool ReadProcessMemory(IntPtr hProcess, IntPtr lpBaseAddress, [Out] byte[] lpBuffer, int dwSize, out IntPtr lpNumberOfBytesRead);

        [DllImport("kernel32.dll")]
        public static extern Int32 CloseHandle(IntPtr hProcess);


        [Flags]
        public enum ProcessAccessFlags : uint
        {
            All = 0x001F0FFF,
            Terminate = 0x00000001,
            CreateThread = 0x00000002,
            VMOperation = 0x00000008,
            VMRead = 0x00000010,
            VMWrite = 0x00000020,
            DupHandle = 0x00000040,
            SetInformation = 0x00000200,
            QueryInformation = 0x00000400,
            Synchronize = 0x00100000
        }


        Int32 m_targetPointer = 0;
        Int32 m_abovePointer = 0;
        Int32 m_belowPointer = 0;
        Int32 m_equalPointer = 0;

        long m_aboveThreshold = 0;
        long m_belowThreshold = 0;
        long m_equalThreshold = 0;

        byte m_bOldVal;
        short m_sOldVal;
        int m_iOldVal;
        string m_processName;
        bool m_useBaseAddress = false;

        public string Name;

        BytesSize m_byteSize = BytesSize.Two;
        BytesSize m_byteAboveSize = BytesSize.Two;
        BytesSize m_byteBelowSize = BytesSize.Two;
        BytesSize m_byteEqualSize = BytesSize.Two;

        public TrackedValue(Models.MemoryLine ml, string processName)
        {
            Name = ml.Name;
            switch (ml.NumberOfBytes)
            {
                case 1: m_byteSize = BytesSize.One; break;
                case 2: m_byteSize = BytesSize.Two; break;
                case 4: m_byteSize = BytesSize.Four; break;
                default: throw new ArgumentException("Error parsing NumberOfBytes");
            }

            switch (ml.AboveNumberOfBytes)
            {
                case 1: m_byteAboveSize = BytesSize.One; break;
                case 2: m_byteAboveSize = BytesSize.Two; break;
                case 4: m_byteAboveSize = BytesSize.Four; break;
                default: throw new ArgumentException("Error parsing AboveNumberOfBytes");
            }


            switch (ml.BelowNumberOfBytes)
            {
                case 1: m_byteBelowSize = BytesSize.One; break;
                case 2: m_byteBelowSize = BytesSize.Two; break;
                case 4: m_byteBelowSize = BytesSize.Four; break;
                default: throw new ArgumentException("Error parsing BelowNumberOfBytes");
            }

            switch (ml.EqualNumberOfBytes)
            {
                case 1: m_byteEqualSize = BytesSize.One; break;
                case 2: m_byteEqualSize = BytesSize.Two; break;
                case 4: m_byteEqualSize = BytesSize.Four; break;
                default: throw new ArgumentException("Error parsing EqualNumberOfBytes");
            }

            Int32 oInt = 0;

            // Target pointer
            if (!int.TryParse(ml.HexPointerTargetValue,
                NumberStyles.HexNumber,
                CultureInfo.CurrentCulture, out oInt))
            {
                throw new ArgumentException("Error parsing HexPointerTargetValue");

            }
            m_targetPointer = oInt;

            m_processName = processName.Replace(".exe", "");

            // Above pointer
            oInt = 0;
            if (!string.IsNullOrEmpty(ml.HexPointerAboveValueCondition) && !int.TryParse(ml.HexPointerAboveValueCondition,
                NumberStyles.HexNumber,
                CultureInfo.CurrentCulture, out oInt))
            {
                throw new ArgumentException("Error parsing HexPointerAboveValueCondition");

            }
            m_abovePointer = oInt;

            // Below pointer
            oInt = 0;
            if (!string.IsNullOrEmpty(ml.HexPointerBelowValueCondition) && !int.TryParse(ml.HexPointerBelowValueCondition,
                NumberStyles.HexNumber,
                CultureInfo.CurrentCulture, out oInt))
            {
                throw new ArgumentException("Error parsing HexPointerBelowValueCondition");

            }
            m_belowPointer = oInt;


            // Equal pointer
            oInt = 0;
            if (!string.IsNullOrEmpty(ml.HexPointerEqualValueCondition) && !int.TryParse(ml.HexPointerEqualValueCondition,
                NumberStyles.HexNumber,
                CultureInfo.CurrentCulture, out oInt))
            {
                throw new ArgumentException("Error parsing HexPointerEqualValueCondition");

            }
            m_equalPointer = oInt;
        }

        public bool CheckIfChanged(out long newVal)
        {
            newVal = 0;
            Process p = Process.GetProcessesByName(m_processName).FirstOrDefault();
            if (p == null)
            {
                newVal = 0;
                return false;
            }

            if (!CheckIfAllowedToChange(p))
                return false;

            byte m_bCurVal = 0;
            short m_sCurVal = 0;
            int m_iCurVal = 0;

            switch (m_byteSize)
            {
                case BytesSize.One: m_bCurVal = ReadByte(p, m_targetPointer); break;
                case BytesSize.Two: m_sCurVal = ReadShort(p, m_targetPointer); break;
                case BytesSize.Four: m_iCurVal = ReadInt(p, m_targetPointer); break;
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

        public void UpdateValue(long newVal)
        {
            Process p = Process.GetProcessesByName(m_processName).FirstOrDefault();
            if (p == null) 
                return;

            if (!CheckIfAllowedToChange(p))
                return;

            switch (m_byteSize)
            {
                case BytesSize.One:
                    WriteByte(p, m_targetPointer, (byte)newVal);
                    m_bOldVal = (byte)newVal;
                    break;
                case BytesSize.Two:
                    WriteShort(p, m_targetPointer, (short)newVal);
                    m_sOldVal = (short)newVal;
                    break;
                case BytesSize.Four:
                    WriteInt(p, m_targetPointer, (int)newVal);
                    m_iOldVal = (int)newVal;
                    break;
                default: throw new NotImplementedException("WTF");
            }

        }

        private bool CheckIfAllowedToChange(Process p)
        {
            bool allowed = true;
            if (m_abovePointer != 0)
            {
                switch (m_byteAboveSize)
                {
                    case BytesSize.One: allowed = ReadByte(p, m_abovePointer) > (byte)m_aboveThreshold; break;
                    case BytesSize.Two: allowed = ReadShort(p, m_abovePointer) > (short)m_aboveThreshold; break;
                    case BytesSize.Four: allowed = ReadInt(p, m_abovePointer) > (int)m_aboveThreshold; break;
                    default: throw new NotImplementedException("WTF");
                }
            }
            if(!allowed)
                return false;
            // We're doing an AND condition by default

            if (m_belowPointer != 0)
            {
                switch (m_byteBelowSize)
                {
                    case BytesSize.One: allowed = ReadByte(p, m_belowPointer) < (byte)m_belowThreshold; break;
                    case BytesSize.Two: allowed = ReadShort(p, m_belowPointer) < (short)m_belowThreshold; break;
                    case BytesSize.Four: allowed = ReadInt(p, m_belowPointer) < (int)m_belowThreshold; break;
                    default: throw new NotImplementedException("WTF");
                }
            }
            if (!allowed)
                return false;

            // We're doing an AND condition by default

            if (m_equalPointer != 0)
            {
                switch (m_byteEqualSize)
                {
                    case BytesSize.One: allowed = ReadByte(p, m_equalPointer) == (byte)m_equalThreshold; break;
                    case BytesSize.Two: allowed = ReadShort(p, m_equalPointer) < (short)m_equalThreshold; break;
                    case BytesSize.Four: allowed = ReadInt(p, m_equalPointer) < (int)m_equalThreshold; break;
                    default: throw new NotImplementedException("WTF");
                }
            }
            if (!allowed)
                return false;

            return true;
        }

        public void WriteByte(Process p, int address, byte v)
        {
            if (m_useBaseAddress)
                address += p.Handle.ToInt32();
            var hProc = OpenProcess(ProcessAccessFlags.All, false, (int)p.Id);
            var val = BitConverter.GetBytes(v);

            int wtf = 0;
            bool ret = WriteProcessMemory(hProc, new IntPtr(address), val, (UInt32)val.LongLength, out wtf);

            if (!ret)
            {
                int errint = Marshal.GetLastWin32Error();
                Console.WriteLine("Error writing memory. Err num : " + errint);
                throw new Exception("Error writing memory. Err num : " + errint);
            }
            else
            {
                if (wtf != (UInt32)val.LongLength)
                {
                    Console.WriteLine("Error writing memory. Wrote only  : " + wtf + " bytes");
                }
            }


            CloseHandle(hProc);
        }

        public void WriteShort(Process p, int address, short v)
        {
            if (m_useBaseAddress)
                address += p.Handle.ToInt32();
            var hProc = OpenProcess(ProcessAccessFlags.All, false, (int)p.Id);
            var val = BitConverter.GetBytes(v);

            int wtf = 0;
            bool ret = WriteProcessMemory(hProc, new IntPtr(address), val, (UInt32)val.LongLength, out wtf);

            if (!ret)
            {
                int errint = Marshal.GetLastWin32Error();
                Console.WriteLine("Error writing memory. Err num : " + errint);
                throw new Exception("Error writing memory. Err num : " + errint);
            }
            else
            {
                if (wtf != (UInt32)val.LongLength)
                {
                    Console.WriteLine("Error writing memory. Wrote only  : " + wtf + " bytes");
                }
            }
            CloseHandle(hProc);
        }

        public void WriteInt(Process p, int address, int v)
        {
            if (m_useBaseAddress)
                address += p.Handle.ToInt32();
            var hProc = OpenProcess(ProcessAccessFlags.All, false, (int)p.Id);
            var val = BitConverter.GetBytes(v);

            int wtf = 0;
            bool ret = WriteProcessMemory(hProc, new IntPtr(address), val, (UInt32)val.LongLength, out wtf);

            if (!ret)
            {
                int errint = Marshal.GetLastWin32Error();
                Console.WriteLine("Error writing memory. Err num : " + errint);
                throw new Exception("Error writing memory. Err num : " + errint);
            }
            else
            {
                if (wtf != (UInt32)val.LongLength)
                {
                    Console.WriteLine("Error writing memory. Wrote only  : " + wtf + " bytes");
                }
            }

            CloseHandle(hProc);
        }

        public byte ReadByte(Process p, int address)
        {
            if (m_useBaseAddress)
                address += p.Handle.ToInt32();
            var hProc = OpenProcess(ProcessAccessFlags.All, false, (int)p.Id);

            byte[] buff = new byte[1];
            IntPtr wtf = IntPtr.Zero;

            bool ret = ReadProcessMemory(hProc, new IntPtr(address), buff, buff.Length, out wtf);


            if (!ret)
            {
                int errint = Marshal.GetLastWin32Error();
                Console.WriteLine("Error reading memory. Err num : " + errint);
                throw new Exception("Error reading memory. Err num : " + errint);
            }
            else
            {
                if ((int)wtf != buff.Length)
                {
                    Console.WriteLine("Error reading memory. Read only  : " + wtf + " bytes");
                }
            }

            CloseHandle(hProc);

            return buff[0];
        }

        public short ReadShort(Process p, int address)
        {
            if (m_useBaseAddress)
                address += p.Handle.ToInt32();
            var hProc = OpenProcess(ProcessAccessFlags.All, false, (int)p.Id);

            byte[] buff = new byte[2];
            IntPtr wtf = IntPtr.Zero;

            bool ret = ReadProcessMemory(hProc, new IntPtr(address), buff, buff.Length, out wtf);


            if (!ret)
            {
                int errint = Marshal.GetLastWin32Error();
                Console.WriteLine("Error reading memory. Err num : " + errint);
                throw new Exception("Error reading memory. Err num : " + errint);
            }
            else
            {
                if ((int)wtf != buff.Length)
                {
                    Console.WriteLine("Error reading memory. Read only  : " + wtf + " bytes");
                }
            }

            CloseHandle(hProc);

            return BitConverter.ToInt16(buff, 0);
        }

        public int ReadInt(Process p, int address)
        {
            if (m_useBaseAddress)
                address += p.Handle.ToInt32();
            var hProc = OpenProcess(ProcessAccessFlags.All, false, (int)p.Id);

            byte[] buff = new byte[4];
            IntPtr wtf = IntPtr.Zero;

            bool ret = ReadProcessMemory(hProc, new IntPtr(address), buff, buff.Length, out wtf);

            if (!ret)
            {
                int errint = Marshal.GetLastWin32Error();
                Console.WriteLine("Error reading memory. Err num : " + errint);
                throw new Exception("Error reading memory. Err num : " + errint);
            }
            else
            {
                if ((int)wtf != buff.Length)
                {
                    Console.WriteLine("Error reading memory. Read only  : " + wtf + " bytes");
                }
            }

            CloseHandle(hProc);

            return BitConverter.ToInt32(buff, 0);
        }
    }
}
