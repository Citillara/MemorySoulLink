using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using Irc;
using System.Security.Cryptography;
using Microsoft.Win32;
using MemorySoulLink.Models;
using System.Diagnostics;

namespace MemorySoulLink
{
    internal class Program
    {
        static Version version = new Version(2, 3, 0);
        const string CHAR_SET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstyvwxyz0123456789";

        const int MEMORY_TIMEOUT = 2000;

        static bool m_Run = true;
        static IrcClient m_client;
        static Settings m_settings;

        static bool m_forceUpdate = false;

        static string m_channel;

        static Dictionary<string, TrackedValue> m_trackedValues = new Dictionary<string, TrackedValue>();
        public static Dictionary<string, Target> Targets = new Dictionary<string, Target>();

        static string m_processName;

        public static bool DemoMode = false;

        static ManualResetEvent m_memoryLock = new ManualResetEvent(true);

        static void Main(string[] args)
        {
            Console.WriteLine("MemorySoulLink [" + version + "] (c) Citillara");
            try
            {

                if (args.Length == 0 && !File.Exists("settings.xml"))
                {
                    DemoMode = true;
                    Settings settingsDemo = GenerateDemoSettings();

                    if (File.Exists("settings.sample.xml"))
                        File.Delete("settings.sample.xml");

                    FileStream fileStream = new FileStream("settings.sample.xml", FileMode.Create);

                    XmlTools.ToXml<Settings>(settingsDemo, fileStream);

                    fileStream.Close();
                    fileStream.Dispose();

                    XmlTools.WriteSchema<Settings>("schema.xsd");

                    return;
                }

                if (args.Length == 0)
                    m_settings = XmlTools.FromXml<Settings>(File.OpenRead("settings.xml"));
                else
                    m_settings = XmlTools.FromXml<Settings>(File.OpenRead(args[0]));

                m_settings.CheckIntegrity();

                foreach (TrackedValue line in m_settings.TrackedValues)
                {
                    m_trackedValues.Add(line.Name, line);
                }
                foreach (Target line in m_settings.Targets)
                {
                    Targets.Add(line.Name, line);
                }

                m_processName = m_settings.ProcessName.Replace(".exe", "");

                m_channel = "#" + m_settings.SessionID;
                m_client = new IrcClient(m_settings.Host, m_settings.HostPort, m_settings.UserName, m_settings.HostTLS);
                m_client.OnPerform += Client_OnPerform;
                m_client.Password = GetPassword();
                m_client.LogEnabled = true;
                m_client.LogLevel = MessageLevel.Info;
                m_client.LogToConsole = true;
                m_client.OnDisconnect += Client_OnDisconnect;
                m_client.OnPrivateMessage += Client_OnPrivateMessage;
                m_client.Connect();

                Thread thread = new Thread(new ThreadStart(Loop));
                thread.Name = "MemoryLoop";
                thread.Start();


                Console.WriteLine("Press CTRL+C to quit");

                while (true)
                {
                    string line = Console.ReadLine();

                    if (line == "qqq")
                        break;

                    if (line == "up")
                    {
                        m_forceUpdate = true;
                    }
                }

                m_memoryLock.Set();
                m_Run = false;
                m_client.Quit("Quitting");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
        }

        private static void Loop()
        {
            try
            {
                Process process = Process.GetProcessesByName(m_processName).FirstOrDefault();

                var vals = m_trackedValues.Values;
                long val = 0;
                // Read all values once to initialize
                if (process != null)
                {
                    Console.WriteLine("Process detected");
                    vals.ToList().ForEach(x => x.CheckIfChanged(process, out val));
                }
                else
                {
                    Console.WriteLine("Process " + m_processName + " not detected during initialization");
                }

                List<TrackedValue> tvToExecute = new List<TrackedValue>();

                int echo = -1;

                while (m_Run)
                {
                       process = Process.GetProcessesByName(m_processName).FirstOrDefault();

                    if (process == null)
                    {
                        echo++;
                        if (echo % 10 == 0)
                            Console.WriteLine(  "Process " + m_processName + " not detected");

                    }
                    else
                    {
                        if (process != null && echo > 0)
                        {
                            Console.WriteLine("Process detected again");
                            vals.ToList().ForEach(x => x.CheckIfChanged(process, out val));
                            echo = -1;
                        }

                        tvToExecute.Clear();
                        m_memoryLock.Reset();
                        foreach (var tv in vals)
                        {
                            bool changed = tv.CheckIfChanged(process, out val) || m_forceUpdate;
                            if (changed)
                            {
                                Console.WriteLine("Local change : [{0}] {1}", tv.Name, val.ToString());
                                tv.PrepareExecute(process, tv.Name, val);
                                tvToExecute.Add(tv);
                            }
                        }

                        tvToExecute.ForEach(x => x.ExecuteActions());

                        m_memoryLock.Set();
                        if (m_forceUpdate)
                            m_forceUpdate = false;
                    }
                    Thread.Sleep(m_settings.Pollspeed);
                    if (!m_memoryLock.WaitOne(MEMORY_TIMEOUT))
                    {
                        Console.WriteLine("[WARN] Memory timeout on Loop. Please notify Citillara if you see this message");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
            finally
            {
                Console.WriteLine("[ERROR] Finalizing loop. Program has crashed, please restart.");
                m_memoryLock.Set();
                m_Run = false;
            }
        }

        public static void LockUpdates()
        {
            if (!m_memoryLock.WaitOne(MEMORY_TIMEOUT))
            {
                Console.WriteLine("[WARN] Memory timeout on LockUpdates. Please notify Citillara if you see this message");
            }
            if (m_Run)
                m_memoryLock.Reset();
        }

        public static void UnlockUpdate(Int32 address)
        {
            Process process = Process.GetProcessesByName(m_processName).FirstOrDefault();
            long val = 0;
            if (process != null)
            {
                // Refresh of the values we modified
                m_trackedValues.Values.Where(t => t.TargetPointer == address).ToList().ForEach(t => t.CheckIfChanged(process, out val));
            }
            m_memoryLock.Set();
        }

        public static void SendUpdate(string name, long value)
        {
            Console.WriteLine("Sending " + name + " : " + value);
            m_client.PrivMsg(m_channel, name + ':' + value.ToString());
        }

        private static void Client_OnPrivateMessage(IrcClient sender, IrcClientOnPrivateMessageEventArgs args)
        {
            Process process = Process.GetProcessesByName(m_processName).FirstOrDefault();
            if (process == null)
                return;

            // Ignore own messages
            if (args.Name == m_settings.UserName)
                return;

            if (!args.Message.Contains(':'))
                return;

            string[] msg = args.Message.Split(':');

            if (!Targets.ContainsKey(msg[0]))
                return;

            long val = 0;
            if (!long.TryParse(msg[1], out val))
                return;

            Console.WriteLine("Remote change from {0} : [{1}] {2}", args.Name, msg[0], msg[1]);

            Targets[msg[0]].UpdateValue(process, val);
        }

        private static string GetPassword()
        {
            try
            {
                using (RegistryKey key = Registry.LocalMachine.OpenSubKey("Software\\Citillara\\MemorySoulLink"))
                {
                    if (key != null)
                    {
                        Object o = key.GetValue("Password");
                        if (o != null)
                        {
                            return o.ToString();
                        }
                    }
                }
            }
            catch
            {
                throw;
            }
            return null;
        }

        private static void Client_OnDisconnect(IrcClient sender, bool wasManualDisconnect)
        {
            if (m_Run != false)
            {
                m_Run = false;
                Thread.Sleep(m_settings.Pollspeed * 4);
                Environment.Exit(1);
            }
        }

        private static void Client_OnPerform(IrcClient sender)
        {
            sender.Join(m_channel);
        }

        private static string GenerateRandomString(int size)
        {
            StringBuilder sb = new StringBuilder(size);
            Random rand = new Random();
            for (int i = 0; i < size; i++)
            {
                sb.Append(CHAR_SET[rand.Next(0, CHAR_SET.Length)]);
            }
            return sb.ToString();
        }

        static byte[] EncryptStringToBytes_Aes(string plainText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (plainText == null || plainText.Length <= 0)
                throw new ArgumentNullException("plainText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        static string DecryptStringFromBytes_Aes(byte[] cipherText, byte[] Key, byte[] IV)
        {
            // Check arguments.
            if (cipherText == null || cipherText.Length <= 0)
                throw new ArgumentNullException("cipherText");
            if (Key == null || Key.Length <= 0)
                throw new ArgumentNullException("Key");
            if (IV == null || IV.Length <= 0)
                throw new ArgumentNullException("IV");

            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Key;
                aesAlg.IV = IV;

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        private static Models.Settings GenerateDemoSettings()
        {

            Models.Settings settingsDemo = new Settings();
            settingsDemo.SessionID = GenerateRandomString(8);
            settingsDemo.ProcessName = "FF7";
            settingsDemo.UserName = "YourPlayerNameHere";
            settingsDemo.Host = "nyx.oragis.fr";
            settingsDemo.HostPort = 6667;
            settingsDemo.HostTLS = false;
            settingsDemo.Pollspeed = 750;

            settingsDemo.Targets = new Target[]
            {
                        new Target()
                        {
                            HexPointer = "01A2B3D",
                            Name = "HP1Combat",
                            BytesSize = BytesSize.Four,
                        },
                        new Target()
                        {
                            HexPointer = "F12434",
                            Name = "HP1Menu",
                            BytesSize = BytesSize.Four,
                        },
                        new Target()
                        {
                            HexPointer = "FDDD34",
                            Name = "HP2Menu",
                            BytesSize = BytesSize.Four,
                        }
            };


            settingsDemo.TrackedValues = new TrackedValue[2];

            settingsDemo.TrackedValues[0] = new TrackedValue()
            {
                Name = "TrackedExample",
                HexPointer = "1DA354",
                BytesSize = BytesSize.Four,
                Actions = new Actions.Action[]
                {
                            new Actions.UpdateValue()
                            {
                                TargetName = "HP1Combat"
                            }
                }
            };

            settingsDemo.TrackedValues[1] = new TrackedValue()
            {
                Name = "TrackedHP1",
                HexPointer = "24D144",
                BytesSize = BytesSize.Four,
                Actions = new Actions.Action[]
                {
                            new Actions.If()
                            {
                                HexPointer1 = "01234A5",
                                Constant2 = "15",
                                Operation = Actions.If.Operations.Equal,
                                Then = new Actions.Action[]
                                {
                                    new Actions.UpdateValue()
                                    {
                                        TargetName = "HP1Combat"
                                    }
                                },
                                Else = new Actions.Action[1]
                                {
                                    new Actions.UpdateValue()
                                    {
                                        TargetName = "HP1Menu"
                                    }
                                }
                            },
                            new Actions.Randomize()
                            {
                                TargetName="HP2Menu",
                                Min = 0,
                                Max = 133

                            },
                            new Actions.Math()
                            {
                                TargetName = "PotionNumber",
                                 Operation = Actions.Math.Computations.Addition,
                                 UpdateLocal = true,
                                 Value = 1

                            }
                }
            };
            return settingsDemo;
        }
    }
}

