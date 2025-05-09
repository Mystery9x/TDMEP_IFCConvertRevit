using System;
using System.IO;
using System.Threading;
using System.Windows;
using System.Windows.Input;
using TepscoIFCToRevit.Common;

namespace TepscoIFCToRevit.UI.ViewModels.VMLicense
{
    public class VMLicense : BindableBase
    {
        private string _licenseKey;

        public string LicenseKey
        {
            get { return _licenseKey; }
            set { SetProperty(ref _licenseKey, value); }
        }

        private string _timeStart;

        public string TimeStart
        {
            get { return _timeStart; }
            set { SetProperty(ref _timeStart, value); }
        }

        private string _timeEnd;

        public string TimeEnd
        {
            get { return _timeEnd; }
            set { SetProperty(ref _timeEnd, value); }
        }

        private string _ipMac;

        public string IpMac
        {
            get { return _ipMac; }
            set { SetProperty(ref _ipMac, value); }
        }

        public ICommand LicenseCommand { get; set; }

        public LicenseData LicenseData { get; set; }

        private void InitData()
        {
            LicenseData = CheckLicenseUltils.GetLicenseDataFile();
            if (LicenseData != null)
            {
                TimeStart = LicenseData.TimeStart;
                TimeEnd = LicenseData.TimeEnd;
                IpMac = LicenseData.IpMac;
            }
            else
            {
                TimeStart = string.Empty;
                TimeEnd = string.Empty;
                IpMac = string.Empty;
            }
        }

        public VMLicense()
        {
            InitData();
            LicenseCommand = new RelayCommand<object>(LicenseCommandInvoke);
        }

        private void LicenseCommandInvoke(object obj)
        {
            if (App.LicenseLogin)
            {
                LogoutLicenseCommandInvoke(obj);
            }
            else
            {
                ApplyLicenseInvoke(obj);
            }
        }

        private void ApplyLicenseInvoke(object obj)
        {
            string configPath = Path.Combine(FileUtils.GetAddinFolder(), Define.NAME_FILE_LICENSE);
            string licenseDecrypt = CheckLicenseUltils.Decrypt(LicenseKey);

            if (!string.IsNullOrEmpty(licenseDecrypt))
            {
                LicenseData licenseData = new LicenseData(licenseDecrypt);

                string macIP = CheckLicenseUltils.GetMacAddress();

                if (string.IsNullOrEmpty(licenseData.IpMac) || licenseData.IpMac.Equals(macIP))
                {
                    try
                    {
                        File.WriteAllText(configPath, LicenseKey);
                        Thread.Sleep(1000);
                        if (CheckLicenseUltils.CheckLicense(false))
                        {
                            if (obj is Window win)
                                win.Close();
                            MessageBox.Show(Define.LICENSE_SUCCESS, Define.Information, MessageBoxButton.OK, MessageBoxImage.Information);
                        }
                        else
                            MessageBox.Show(Define.LICENSE_EXPIRED, Define.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(Define.LOGIN_FAILED, Define.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
                    }
                }
                else { MessageBox.Show(Define.LICENSE_INVALID, Define.Error, MessageBoxButton.OK, MessageBoxImage.Error); }
            }
            else
                MessageBox.Show(Define.LICENSE_INVALID, Define.Error, MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void LogoutLicenseCommandInvoke(object obj)
        {
            try
            {
                string configPath = Path.Combine(FileUtils.GetAddinFolder(), Define.NAME_FILE_LICENSE);
                if (File.Exists(configPath))
                {
                    File.Delete(configPath);
                }
                CheckLicenseUltils.DisableItemRibbon(App._UICtrlApp);
                if (obj is Window win)
                    win.Close();
                MessageBox.Show(Define.LICENSE_LOGOUT, Define.Information, MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception)
            {
                MessageBox.Show(Define.LOGOUT_FAILED, Define.Warning, MessageBoxButton.OK, MessageBoxImage.Warning);
            }
        }
    }
}