using System.Windows.Controls;

namespace TepscoIFCToRevit.UI.Views.ShowDialogUI
{
    /// <summary>
    /// Interaction logic for RuleView.xaml
    /// </summary>
    public partial class RuleView : UserControl
    {
        public RuleView()
        {
            InitializeComponent();
        }

        private void DbMainTable_AutoGeneratingColumn(object sender, DataGridAutoGeneratingColumnEventArgs e)
        {
            if ((string)e.Column.Header == "Id")
                e.Column.Header = Define.RevElemId;

            if ((string)e.Column.Header == "Type")
            {
                e.Column.Header = Define.RevType;
                e.Column.Width = DataGridLength.Auto;
            }

            if ((string)e.Column.Header == "Status")
                e.Column.Header = Define.RevStatus;

            if ((string)e.Column.Header == "IsSuccess")
                e.Cancel = true;

            if ((string)e.Column.Header == "LinkInstance")
                e.Cancel = true;

            if ((string)e.Column.Header == "RevLinkTransform")
                e.Cancel = true;

            if ((string)e.Column.Header == "Type")
                e.Column.Width = 180;

            if ((string)e.Column.Header == "RevLinkName")
            {
                e.Column.Width = DataGridLength.Auto;
                e.Column.Header = Define.RevLnkName;
            }
        }
    }
}