using Autodesk.Revit.DB;

namespace TepscoIFCToRevit.UI.ViewModels.VMShowDialog
{
    public class VMElement : BindableBase
    {
        private string _id;

        public string Id
        {
            get { return _id; }
            set { SetProperty(ref _id, value); }
        }

        private string _type;

        public string Type
        {
            get { return _type; }
            set { SetProperty(ref _type, value); }
        }

        private string _status;

        public string Status
        {
            get { return _status; }
            set { SetProperty(ref _status, value); }
        }

        private bool _isSuccess;

        public bool IsSuccess
        {
            get { return _isSuccess; }
            set
            {
                SetProperty(ref _isSuccess, value);
            }
        }

        private RevitLinkInstance m_revLnkIns = null;

        public RevitLinkInstance LinkInstance
        {
            get => m_revLnkIns;
            set => SetProperty(ref m_revLnkIns, value);
        }

        private string _revLinkName;

        public string RevLinkName
        {
            get => _revLinkName;
            set => SetProperty(ref _revLinkName, value);
        }

        public VMElement(string id, string type, string status, bool isSuccess, RevitLinkInstance revitLink, string revitLinkName = null)
        {
            Id = id;
            Type = type;
            Status = status;
            IsSuccess = isSuccess;
            LinkInstance = revitLink;
            RevLinkName = revitLinkName != null ? revitLinkName : revitLink?.Name.ToString();
        }
    }
}