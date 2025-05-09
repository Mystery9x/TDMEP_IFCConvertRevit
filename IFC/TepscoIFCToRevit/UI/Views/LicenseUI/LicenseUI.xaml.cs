using System.Windows;
using System.Windows.Input;
using TepscoIFCToRevit.UI.ViewModels.VMLicense;

namespace TepscoIFCToRevit.UI.Views.LicenseUI
{
    /// <summary>
    /// Interaction logic for LicenseUI.xaml
    /// </summary>
    public partial class LicenseUI : Window
    {
        public LicenseUI()
        {
            InitializeComponent();
            this.DataContext = new VMLicense();
            if (App.LicenseLogin)
            {
                tb_License.Visibility = Visibility.Collapsed;
                tb_LicenseKey.Visibility = Visibility.Collapsed;
                btn_License.Content = "ログアウト";
            }
            else
            {
                tb_License.Visibility = Visibility.Visible;
                tb_LicenseKey.Visibility = Visibility.Visible;
                btn_License.Content = "ログイン";
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
            }
        }
    }
}