using Autodesk.Revit.DB;
using System.Windows;
using System.Windows.Input;
using TepscoIFCToRevit.UI.ViewModels.VMJoinGeometry;

namespace TepscoIFCToRevit.UI.Views
{
    /// <summary>
    /// Interaction logic for JoinGeometryUI.xaml
    /// </summary>
    public partial class JoinGeometryUI : Window
    {
        public JoinGeometryUI(Document doc)
        {
            InitializeComponent();

            DataContext = new VMJoinGeometry(doc);
        }

        public void CloseButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
                App.IFCConvDlg = null;
            }
        }
    }
}