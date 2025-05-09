using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace TepscoIFCToRevit.UI.Views
{
    /// <summary>
    /// Interaction logic for ProgressBarLoadFamily.xaml
    /// </summary>
    public partial class ProgressBarLoadFamily : Window
    {
        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hwnd, int index, int value);

        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hwnd, int index);

        internal static void HideMinimizeMaximizeAndCloseButtons(Window window)
        {
            const int GWL_STYLE = -16;
            const int WS_SYSMENU = 0x80000;

            IntPtr hwnd = new System.Windows.Interop.WindowInteropHelper(window).Handle;
            long value = GetWindowLong(hwnd, GWL_STYLE);

            SetWindowLong(hwnd, GWL_STYLE, (int)(value & ~WS_SYSMENU));
        }

        private UpdateProgressBarDelegate updPb0Delegate = null;

        private double value0 = 0;

        public bool IsCancel = false;
        public bool IsSkip = false;

        public ProgressBarLoadFamily(string title, string message)
        {
            InitializeComponent();
            Title = title;
            if (message != null) { }
            tbxMessageLoadFamily.Text = message;

            if (title.Equals(Define.MESS_PROGESSBAR_PROCESS_LOAD_SETTING))
            {
                tbContentLoad.Visibility = Visibility.Collapsed;
                tbxMessageLoadFamily.Visibility = Visibility.Collapsed;
            }
        }

        public void IncrementProgressBarLoad()
        {
            value0++;
            Dispatcher.Invoke(
                updPb0Delegate,
                System.Windows.Threading.DispatcherPriority.Background, new object[]
                {
                    System.Windows.Controls.ProgressBar.ValueProperty, value0
                }
            );
        }

        public void UpdateProgressBar()
        {
            Dispatcher.Invoke(
                updPb0Delegate,
                System.Windows.Threading.DispatcherPriority.Background, new object[]
                {
                    ProgressBar.ValueProperty, value0
                }
            );
        }

        public void ResetProgressBar()
        {
            value0 = 0;
            Dispatcher.Invoke(
                updPb0Delegate,
                System.Windows.Threading.DispatcherPriority.Background, new object[]
                {
                    ProgressBar.ValueProperty, value0
                }
            );
        }

        private delegate void UpdateProgressBarDelegate(System.Windows.DependencyProperty dp, Object value);

        #region IDisposable

        public void Dispose()
        {
            this.Close();
        }

        #endregion IDisposable

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            updPb0Delegate = new UpdateProgressBarDelegate(prgSingleLoadFamily.SetValue);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            HideMinimizeMaximizeAndCloseButtons(this);
        }

        public void SetFamilyMessage(string mess)
        {
            this.tbxMessageLoadFamily.Text = mess;
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (value0 < this.prgSingleLoadFamily.Maximum)
            {
                IsCancel = true;
            }
            this.Dispose();
            return;
        }
    }
}