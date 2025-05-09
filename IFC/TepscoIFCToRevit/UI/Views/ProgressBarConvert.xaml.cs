using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;

namespace TepscoIFCToRevit.UI.Views
{
    /// <summary>
    /// Interaction logic for ProgressBarConvert.xaml
    /// </summary>
    public partial class ProgressBarConvert : Window, IDisposable
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

        public string ContentApplyButton { get; set; }

        public string ContentCanCelButton { get; set; }

        public ProgressBarConvert(string title, string message)
        {
            InitializeComponent();
            DataContext = this;
            this.Title = title;
            this.tbxMessage.Text = message;
            ContentApplyButton = Define.Apply_CONTENT;
            ContentCanCelButton = Define.CANCEL_CONTENT;
        }

        public void IncrementProgressBar()
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

        #region IDisposable メンバー

        public void Dispose()
        {
            this.Close();
        }

        #endregion IDisposable メンバー

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            updPb0Delegate = new UpdateProgressBarDelegate(prgSingle.SetValue);
        }

        private void Window_SourceInitialized(object sender, EventArgs e)
        {
            HideMinimizeMaximizeAndCloseButtons(this);
        }

        private void ButtonCancel_Click(object sender, RoutedEventArgs e)
        {
            if (value0 < this.prgSingle.Maximum)
            {
                IsCancel = true;
            }
            this.Dispose();
            return;
        }

        private void ButtonSkip_Click(object sender, RoutedEventArgs e)
        {
            if (value0 < this.prgSingle.Maximum)
            {
                IsSkip = true;
            }
            this.Dispose();
            return;
        }

        public void SetMessage(string mess)
        {
            tbxMessage.Text = mess;
        }
    }
}