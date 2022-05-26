using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace MemorySoulLink
{
    internal class MemoryHelper
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


        public static void WriteByte(Process p, int address, byte v)
        {
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

        public static void WriteShort(Process p, int address, short v)
        {
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

        public static void WriteInt(Process p, int address, int v)
        {
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

        public static byte ReadByte(Process p, int address)
        {
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

        public static short ReadShort(Process p, int address)
        {
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

        public static int ReadInt(Process p, int address)
        {
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
