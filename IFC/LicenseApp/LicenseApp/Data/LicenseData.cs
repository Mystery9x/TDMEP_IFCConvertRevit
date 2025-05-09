namespace LicenseApp.Data
{
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
            IpMac = string.IsNullOrWhiteSpace(ipMac) ? string.Empty : ipMac.Trim();

            DataSet = $"{TimeStart}|{TimeEnd}|{IpMac}";
        }
    }
}