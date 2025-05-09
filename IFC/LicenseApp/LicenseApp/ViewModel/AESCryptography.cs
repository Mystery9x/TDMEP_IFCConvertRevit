using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;

namespace LicenseApp.ViewModel
{
    public class AESCryptography
    {
        private static string Key; // AES key (must be 32 bytes for AES-256)
        private static string IV; // AES IV (must be 16 bytes)

        public static string Encrypt(string plainText)
        {
            try
            {
                string fixedIV = "0123456789ABCDEF";
                string fixedKey = "0123456789ABCDEF";

                Key = Convert.ToBase64String(Encoding.UTF8.GetBytes(fixedIV));
                IV = Convert.ToBase64String(Encoding.UTF8.GetBytes(fixedKey));

                using (Aes aes = Aes.Create())
                {
                    aes.KeySize = 128;
                    aes.Key = Convert.FromBase64String(Key);
                    aes.IV = Convert.FromBase64String(IV);

                    ICryptoTransform encryptor = aes.CreateEncryptor(aes.Key, aes.IV);

                    using (MemoryStream ms = new MemoryStream())
                    {
                        using (CryptoStream cs = new CryptoStream(ms, encryptor, CryptoStreamMode.Write))
                        {
                            using (StreamWriter sw = new StreamWriter(cs))
                            {
                                sw.Write(plainText);
                            }

                            byte[] encryptedBytes = ms.ToArray();

                            //return Convert.ToBase64String(ms.ToArray());
                            return Convert.ToBase64String(encryptedBytes);
                        }
                    }
                }
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static string Decrypt(string cipherText)
        {
            using (Aes aes = Aes.Create())
            {
                aes.KeySize = 128;

                aes.Key = Convert.FromBase64String(Key);
                aes.IV = Convert.FromBase64String(IV);

                ICryptoTransform decryptor = aes.CreateDecryptor(aes.Key, aes.IV);

                byte[] cipherBytes = Convert.FromBase64String(cipherText);
                using (MemoryStream ms = new MemoryStream(cipherBytes))
                {
                    using (CryptoStream cs = new CryptoStream(ms, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader sr = new StreamReader(cs))
                        {
                            return sr.ReadToEnd();
                        }
                    }
                }
            }
        }

        public string GetMacAddress()
        {
            NetworkInterface[] networkInterfaces = NetworkInterface.GetAllNetworkInterfaces();
            foreach (NetworkInterface nic in networkInterfaces)
            {
                string name = nic.Name;
                PhysicalAddress physicalAddress = nic.GetPhysicalAddress();

                if (!string.IsNullOrEmpty(physicalAddress.ToString()))
                {
                    return physicalAddress.ToString();
                }
            }

            return null;
        }

        public string GetMacAddress2()
        {
            var networkInterface = NetworkInterface.GetAllNetworkInterfaces()
                                      .Where(n => n.NetworkInterfaceType != NetworkInterfaceType.Loopback &&
                                                  n.OperationalStatus == OperationalStatus.Up)
                                      .FirstOrDefault();

            if (networkInterface != null)
            {
                var macAddress = networkInterface.GetPhysicalAddress();
                return string.Join("-", macAddress.GetAddressBytes().Select(b => b.ToString("X2")));
            }

            return null;
        }
    }
}