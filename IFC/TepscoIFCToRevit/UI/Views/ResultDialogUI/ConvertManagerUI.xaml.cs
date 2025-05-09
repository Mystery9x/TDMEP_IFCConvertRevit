using System.Windows;
using System.Windows.Input;

namespace TepscoIFCToRevit.UI.Views.ShowDialogUI
{
    /// <summary>
    /// Interaction logic for ConvertManagerUI.xaml
    /// </summary>
    public partial class ConvertManagerUI : Window
    {
        public ConvertManagerUI()
        {
            InitializeComponent();
        }

        public void CloseButton(object sender, RoutedEventArgs e)
        {
            Close();
        }

        private void Window_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                Close();
            }
        }
    }
}