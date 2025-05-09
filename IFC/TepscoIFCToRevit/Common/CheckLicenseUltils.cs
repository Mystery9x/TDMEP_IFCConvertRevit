using Autodesk.Revit.UI;
using System;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Security.Cryptography;
using System.Text;
using System.Windows;

namespace TepscoIFCToRevit.Common
{
    public class CheckLicenseUltils
    {
        private static string Key;
        private static string IV;

        public static bool CheckLicense(bool showInfo = true)
        {
            bool hasLicense = false;
            try
            {
                string filePath = Path.Combine(FileUtils.GetAddinFolder(), Define.NAME_FILE_LICENSE);
                LicenseData licenseData = ReadAndDecryptFromFile(filePath);
                if (licenseData != null)
                {
                    DateTime targetTime = DateTime.ParseExact(licenseData.TimeEnd, Define.TIME_DATA, null);
                    if (targetTime.Date >= DateTime.Now.Date)
                    {
                        hasLicense = true;
                    }
                    else
                    {
                        if (showInfo)
                        {
                            MessageBox.Show(Define.LICENSE_EXPIRED, Define.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                        }
                    }
                }
                else
                {
                    if (showInfo)
                    {
                        MessageBox.Show(Define.LICENSE_DATA_NULL, Define.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
            }
            catch (Exception)
            {
                if (showInfo)
                {
                    MessageBox.Show(Define.LICENSE_DATA_NULL, Define.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                }
            }
            if (hasLicense)
            {
                EnableItemRibbon(App._UICtrlApp);
            }
            else
            {
                DisableItemRibbon(App._UICtrlApp);
            }

            return hasLicense;
        }

        public static LicenseData GetLicenseDataFile()
        {
            string configPath = Path.Combine(FileUtils.GetAddinFolder(), Define.NAME_FILE_LICENSE);
            try
            {
                if (CheckLicenseFile(configPath))
                    return ReadAndDecryptFromFile(configPath);
                else
                    File.Create(configPath);
            }
            catch (Exception)
            {
                return null;
            }

            return null;
        }

        public static bool CheckLicenseFile(string filePath)
        {
            if (File.Exists(filePath))
            {
                string encryptedText = File.ReadAllText(filePath);

                if (!string.IsNullOrEmpty(encryptedText))
                    return true;
            }
            return false;
        }

        public static LicenseData ReadAndDecryptFromFile(string filePath)
        {
            try
            {
                string encryptedText = File.ReadAllText(filePath);
                string decryptedData = Decrypt(encryptedText);

                if (string.IsNullOrEmpty(decryptedData))
                    return null;

                return new LicenseData(decryptedData);
            }
            catch (Exception)
            {
                return null;
            }
        }

        public static void DisableItemRibbon(UIControlledApplication app)
        {
            if (app == null)
            {
                return;
            }
            App.SetImgRibbonButton(Define.TEPSCO_IFC_TO_REV_TABNAME, Define.TEPSCO_IFC_LICENSE_PANELNAME, Define.ButtonLicense, Define.ButtonLogin, "Signin-01.png");
            App.LicenseLogin = false;

            var ribbonPanels = app.GetRibbonPanels(Define.TEPSCO_IFC_TO_REV_TABNAME);
            foreach (var item in ribbonPanels)
            {
                foreach (var ribbonItem in item.GetItems())
                {
                    if (ribbonItem.Name == "btnLogin")
                    {
                        ribbonItem.ItemText = Define.ButtonLogin;
                    }
                    else
                        ribbonItem.Enabled = false;
                }
            }
        }

        public static void EnableItemRibbon(UIControlledApplication app)
        {
            if (app == null)
            {
                return;
            }
            App.SetImgRibbonButton(Define.TEPSCO_IFC_TO_REV_TABNAME, Define.TEPSCO_IFC_LICENSE_PANELNAME, Define.ButtonLogin, Define.ButtonLicense, "License-01.png");
            App.LicenseLogin = true;

            var ribbonPanels = app.GetRibbonPanels(Define.TEPSCO_IFC_TO_REV_TABNAME);
            foreach (var item in ribbonPanels)
            {
                foreach (var ribbonItem in item.GetItems())
                {
                    if (ribbonItem.Name == "btnLogin")
                        ribbonItem.ItemText = Define.ButtonLicense;
                    else
                    {
                        ribbonItem.Enabled = true;
                        App.LicenseLogin = true;
                    }
                }
            }
        }

        public static string Decrypt(string cipherText)
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
            catch (Exception)
            {
                return null;
            }
        }

        public static string GetMacAddress()
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

    public class LicenseData
    {
        public string TimeStart { get; set; }
        public string TimeEnd { get; set; }
        public string IpMac { get; set; }
        public string DataSet { get; set; }

        public LicenseData(string timeStart, string timeEnd, string ipMac)
        {
            TimeEnd = timeEnd;
            TimeStart = timeStart;
            IpMac = ipMac;

            DataSet = $"{TimeStart}|{TimeEnd}|{IpMac}";
        }

        public LicenseData(string dataSet)
        {
            DataSet = dataSet;
            var parts = DataSet.Split('|');
            if (parts.Length == 3)
            {
                TimeStart = parts[0];
                TimeEnd = parts[1];
                IpMac = parts[2];
            }
        }
    }
}