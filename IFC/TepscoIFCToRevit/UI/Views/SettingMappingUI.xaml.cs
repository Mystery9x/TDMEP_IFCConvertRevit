using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace TepscoIFCToRevit.UI.Views
{
    /// <summary>
    /// Interaction logic for SettingMappingUI.xaml
    /// </summary>
    public partial class SettingMappingUI : Window
    {
        public SettingMappingUI()
        {
            InitializeComponent();
        }

        /// <summary>
        /// Find child control
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="depencencyObject"></param>
        /// <returns></returns>
        private T FindChild<T>(DependencyObject depencencyObject) where T : DependencyObject
        {
            if (depencencyObject != null)
            {
                for (int i = 0; i < VisualTreeHelper.GetChildrenCount(depencencyObject); ++i)
                {
                    DependencyObject child = VisualTreeHelper.GetChild(depencencyObject, i);
                    T result = (child as T) ?? FindChild<T>(child);
                    if (result != null)
                        return result;
                }
            }

            return null;
        }

        /// <summary>
        /// Find parent control
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="dependencyObject"></param>
        /// <returns></returns>
        private static T FindParent<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);

            if (parent == null) return null;

            var parentT = parent as T;
            return parentT ?? FindParent<T>(parent);
        }

        private void dgvSetting_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListView lVParent = FindParent<ListView>(sender as DependencyObject);
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                ListViewItem item = (ListViewItem)lVParent.ItemContainerGenerator.ContainerFromItem(dataGrid.DataContext);
                if (item != null)
                {
                    lVParent.UnselectAll();
                    item.IsSelected = true;
                }
            }
        }

        private void ScrollViewer_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (sender is ScrollViewer scv && scv.ScrollableHeight > 0)
            {
                scv.ScrollToVerticalOffset(scv.VerticalOffset - e.Delta);
                e.Handled = true;
            }
        }

        private void dgvType_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            ListView lVParent = FindParent<ListView>(sender as DependencyObject);
            DataGrid dataGrid = sender as DataGrid;
            if (dataGrid != null)
            {
                ListViewItem item = (ListViewItem)lVParent.ItemContainerGenerator.ContainerFromItem(dataGrid.DataContext);
                if (item != null)
                {
                    lVParent.UnselectAll();
                    item.IsSelected = true;
                }
            }
        }
    }
}