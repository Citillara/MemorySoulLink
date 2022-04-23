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

namespace MemorySoulLink
{
    internal class Program
    {

        const string CHAR_SET = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstyvwxyz0123456789";


        static bool m_Run = true;
        static IrcClient client;
        static Settings settings;

        static bool forceUpdate = false;

        static string channel;

        static Dictionary<string, TrackedValue> trackedValues = new Dictionary<string, TrackedValue>();

        static void Main(string[] args)
        {
            try
            {

                if (args.Length == 0 && !File.Exists("settings.xml"))
                {
                    Models.Settings settingsDemo = new Settings();
                    settingsDemo.SessionID = GenerateRandomString(8);
                    settingsDemo.ProcessName = "FF7";
                    settingsDemo.UserName = "YourPlayerNameHere";
                    settingsDemo.Host = "nyx.oragis.fr";
                    settingsDemo.HostPort = 6667;
                    settingsDemo.HostTLS = false;

                    settingsDemo.MemoryLines = new MemoryLine[1];
                    settingsDemo.MemoryLines[0] = new MemoryLine()
                    {
                        Name = "HP1",
                        HexPointerTargetValue = "Pointer to value to sync in hex (2F34A0)",
                        NumberOfBytes = 4,
                        HexPointerAboveValueCondition = "[Leave empty to disable] Pointer : alter target if only this value is above a certain number",
                        AboveValueConditionValue = 0,
                        AboveNumberOfBytes = 4,
                        HexPointerBelowValueCondition = "[Leave empty to disable] Pointer : alter target if only this value is below a certain number",
                        BelowValueConditionValue = 100,
                        BelowNumberOfBytes = 4,
                        HexPointerEqualValueCondition = "[Leave empty to disable] Pointer : alter target if only this value is equal a certain number",
                        EqualValueConditionValue = 100,
                        EqualNumberOfBytes = 4,
                    };

                    if (File.Exists("settings.sample.xml"))
                        File.Delete("settings.sample.xml");

                    FileStream fileStream = new FileStream("settings.sample.xml", FileMode.Create);

                    XmlTools.ToXml<Settings>(settingsDemo, fileStream);

                    fileStream.Close();
                    fileStream.Dispose();

                    XmlTools.WriteSchema<Settings>();

                    return;
                }
                
                if(args.Length == 0)
                    settings = XmlTools.FromXml<Settings>(File.OpenRead("settings.xml"));
                else
                    settings = XmlTools.FromXml<Settings>(File.OpenRead(args[0]));

                foreach (MemoryLine line in settings.MemoryLines)
                {
                    trackedValues.Add(line.Name, new TrackedValue(line, settings.ProcessName));
                }

                channel = "#" + settings.SessionID;
                client = new IrcClient(settings.Host, settings.HostPort, settings.UserName, settings.HostTLS);
                client.OnPerform += Client_OnPerform;
                client.Password = GetPassword();
                client.LogEnabled = true;
                client.LogLevel = MessageLevel.Info;
                client.LogToConsole = true;
                client.OnDisconnect += Client_OnDisconnect;
                client.OnPrivateMessage += Client_OnPrivateMessage;
                client.Connect();

                Thread thread = new Thread(new ThreadStart(Loop));
                thread.Start();


                Console.WriteLine("Press CTRL+C to quit");

                while (true)
                {
                    string line = Console.ReadLine();

                    if (line == "qqq")
                        break;

                    if (line == "up")
                    {
                        forceUpdate = true;
                    }
                }

                m_Run = false;
                client.Quit("Quitting");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
                Console.WriteLine("Press any key to exit");
                Console.ReadKey();
            }
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

        private static void Client_OnPrivateMessage(IrcClient sender, IrcClientOnPrivateMessageEventArgs args)
        {
            // Ignore own messages
            if (args.Name == settings.UserName)
                return;

            if (!args.Message.Contains(':'))
                return;

            string[] msg = args.Message.Split(':');

            if (!trackedValues.ContainsKey(msg[0]))
                return;

            long val = 0;
            if (!long.TryParse(msg[1], out val))
                return;
            
            Console.WriteLine("Remote change from {0} : [{1}] {2}", args.Name, msg[0], msg[1]);

            trackedValues[msg[0]].UpdateValue(val);
        }

        private static void Client_OnDisconnect(IrcClient sender, bool wasManualDisconnect)
        {
            if (m_Run != false)
            {
                m_Run = false;
                Thread.Sleep(settings.PollSpped * 4);
                Environment.Exit(1);
            }
        }

        private static void Loop()
        {
            var vals = trackedValues.Values;
            long val = 0;
            // Read all values once to initialize
            vals.ToList().ForEach(x => x.CheckIfChanged(out val));


            while (m_Run)
            {
                foreach(var tv in vals)
                {
                    bool changed = tv.CheckIfChanged(out val) || forceUpdate;
                    if (changed)
                    {
                        Console.WriteLine("Local change : [{0}] {1}", tv.Name, val.ToString());
                        client.PrivMsg(channel, tv.Name + ':' + val.ToString());
                    }
                }

                if (forceUpdate)
                    forceUpdate = false;

                Thread.Sleep(settings.PollSpped);
            }
        }

        private static void Client_OnPerform(IrcClient sender)
        {
            sender.Join(channel);
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
    }
}

