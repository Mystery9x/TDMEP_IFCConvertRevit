using Autodesk.Revit.DB;

namespace TepscoIFCToRevit.UI.ViewModels
{
    public class VMConvertObject : BindableBase
    {
        #region Variable

        private bool _isChecked;
        private SourceLinkIfcData _linkIFCName;

        #endregion Variable

        #region Property

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public SourceLinkIfcData LinkIFCName
        {
            get => _linkIFCName;
            set => SetProperty(ref _linkIFCName, value);
        }

        public VMConvertObject()
        {
        }

        #endregion Property
    }

    public class VMConvertIFCtoRevTargetObject : BindableBase
    {
        #region Variable

        private bool _isChecked = false;
        private string _content = string.Empty;

        #endregion Variable

        #region Property

        private System.Windows.Visibility _visibilityHint;

        public System.Windows.Visibility VisibilityHint
        {
            get => _visibilityHint;
            set => SetProperty(ref _visibilityHint, value);
        }

        private System.Windows.Visibility _visibilityTarget;

        public System.Windows.Visibility VisibilityTarget
        {
            get => _visibilityTarget;
            set => SetProperty(ref _visibilityTarget, value);
        }

        public bool IsChecked
        {
            get => _isChecked;
            set => SetProperty(ref _isChecked, value);
        }

        public string Content
        {
            get => _content;
            set => SetProperty(ref _content, value);
        }

        #endregion Property

        #region Constructor

        public VMConvertIFCtoRevTargetObject(bool isChecked, string content)
        {
            IsChecked = isChecked;
            Content = !string.IsNullOrWhiteSpace(content) ? content.Trim().ToUpper() : string.Empty;
            VisibilityHint = System.Windows.Visibility.Visible;
            VisibilityTarget = System.Windows.Visibility.Hidden;
        }

        #endregion Constructor
    }

    public class SourceLinkIfcData
    {
        #region Variable & Properties

        public RevitLinkInstance LinkIfc { get; set; }

        public string LinkIfcName { get; set; }

        public override string ToString()
        {
            return LinkIfcName;
        }

        #endregion Variable & Properties

        #region Constructor

        public SourceLinkIfcData(RevitLinkInstance linkIns)
        {
            LinkIfcName = string.Empty;
            if (linkIns != null)
            {
                LinkIfc = linkIns;
                LinkIfcName = LinkIfc.Name;
            }
        }

        #endregion Constructor
    }
}