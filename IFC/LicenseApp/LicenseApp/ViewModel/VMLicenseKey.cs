using LicenseApp.Common;
using LicenseApp.Data;
using System;
using System.Collections.ObjectModel;
using System.Text.RegularExpressions;
using System.Windows.Forms;
using System.Windows.Input;

namespace LicenseApp.ViewModel
{
    public class VMLicenseKey : BindableBase
    {
        private ObservableCollection<string> _timeSet;

        public ObservableCollection<string> TimeSet
        {
            get { return _timeSet; }
            set { SetProperty(ref _timeSet, value); }
        }

        private DateTime? _endTime;

        public DateTime? EndTime
        {
            get { return _endTime; }
            set { SetProperty(ref _endTime, value); }
        }

        private string _selTime;

        public string SelTime
        {
            get { return _selTime; }
            set
            {
                SetProperty(ref _selTime, value);
                UpdateEndTime();
            }
        }

        private string _ipMac;

        public string IpMac
        {
            get { return _ipMac; }
            set { SetProperty(ref _ipMac, value); }
        }

        private string _licenseKey;

        public string LicenseKey
        {
            get { return _licenseKey; }
            set { SetProperty(ref _licenseKey, value); }
        }

        private bool _isSetDateTime;

        public bool IsSetDateTime
        {
            get { return _isSetDateTime; }
            set { SetProperty(ref _isSetDateTime, value); }
        }

        public VMLicenseKey()
        {
            ApplyCommand = new RelayCommand<object>(ApplyCommandInvoke);
            CancelCommand = new RelayCommand<object>(CancelCommandInvoke);
            CoppyCommand = new RelayCommand<object>(CoppylCommandInvoke);

            AESCryptography aESCryptography = new AESCryptography();

            IsSetDateTime = false;

            TimeSet = new ObservableCollection<string>
            {
                Define.OneWeek,
                Define.OneMonth,
                Define.ThreeMonth,
                Define.SixMonths,
                Define.TwelveMonths,
                Define.SetTime
            };

            SelTime = Define.OneMonth;
            //IpMac = aESCryptography.GetMacAddress2();
        }

        private void UpdateEndTime()
        {
            DateTime startTime = DateTime.Now;
            DateTime endTime = startTime;

            switch (SelTime)
            {
                case Define.OneWeek:
                    IsSetDateTime = false;
                    endTime = startTime.AddDays(7);
                    break;

                case Define.OneMonth:
                    IsSetDateTime = false;
                    endTime = startTime.AddMonths(1);
                    break;

                case Define.ThreeMonth:
                    IsSetDateTime = false;
                    endTime = startTime.AddMonths(3);
                    break;

                case Define.SixMonths:
                    IsSetDateTime = false;
                    endTime = startTime.AddMonths(6);
                    break;

                case Define.TwelveMonths:
                    IsSetDateTime = false;
                    endTime = startTime.AddYears(1);
                    break;

                case Define.SetTime:
                    IsSetDateTime = true;

                    break;

                default:
                    endTime = startTime;
                    break;
            }

            EndTime = endTime.Date;
        }

        #region Command Methods

        public ICommand ApplyCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand CoppyCommand { get; set; }

        private bool CheckEndTime()
        {
            if (EndTime == null)
            {
                MessageBox.Show(Define.SetTimeLicense, Define.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }

            TimeSpan timeSpan = (TimeSpan)(EndTime?.Date - DateTime.Now.Date);
            int daysRemaining = timeSpan.Days;

            if (daysRemaining < 0)
            {
                MessageBox.Show(Define.LicenseExpried, Define.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return false;
            }
            else if (daysRemaining == 0)
            {
                MessageBox.Show(Define.LicenseExpriedToDay, Define.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return true;
            }
            else
            {
                MessageBox.Show(Define.LicenseDayRemaining + daysRemaining, Define.Information, MessageBoxButtons.OK, MessageBoxIcon.Information);
                return true;
            }
        }

        private void CoppylCommandInvoke(object obj)
        {
            if (!string.IsNullOrWhiteSpace(LicenseKey))
            {
                Clipboard.SetText(LicenseKey);
            }
        }

        private void CancelCommandInvoke(object obj)
        {
            if (obj is System.Windows.Window window)
                window.Close();
        }

        private void ApplyCommandInvoke(object obj)
        {
            if (!string.IsNullOrWhiteSpace(IpMac))
            {
                if (!IsValidMacAddress(IpMac.Trim()))
                {
                    LicenseKey = string.Empty;
                    MessageBox.Show(Define.IpMacError, Define.Warning, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }
            }

            if (CheckEndTime())
            {
                LicenseData licenseData = new LicenseData(DateTime.Now.ToString(Define.TimeData), EndTime?.ToString(Define.TimeData), IpMac);
                LicenseKey = AESCryptography.Encrypt(licenseData.DataSet);
            }
            else
            {
                LicenseKey = string.Empty;
            }

            if (!string.IsNullOrWhiteSpace(LicenseKey))
            {
                Clipboard.SetText(LicenseKey);
            }
        }

        private bool IsValidMacAddress(string mac)
        {
            string pattern = @"^([0-9A-Fa-f]{2}[:-]){5}([0-9A-Fa-f]{2})$";
            return Regex.IsMatch(mac, pattern);
        }

        #endregion Command Methods
    }
}