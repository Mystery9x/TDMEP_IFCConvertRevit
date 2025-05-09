using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using TepscoIFCToRevit.UI.ViewModels.VMMappingSetting;
using DataGrid = System.Windows.Controls.DataGrid;
using ListView = System.Windows.Controls.ListView;
using ListViewItem = System.Windows.Controls.ListViewItem;

namespace TepscoIFCToRevit.UI.Views.SettingGroupUI
{
    /// <summary>
    /// Interaction logic for GroupConditionUI.xaml
    /// </summary>
    public partial class GroupConditionUI : System.Windows.Controls.UserControl
    {
        public GroupConditionUI()
        {
            InitializeComponent();
        }

        private static T FindParent<T>(DependencyObject dependencyObject) where T : DependencyObject
        {
            var parent = VisualTreeHelper.GetParent(dependencyObject);

            if (parent == null) return null;

            var parentT = parent as T;
            return parentT ?? FindParent<T>(parent);
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

        private void dgvType_Loaded(object sender, RoutedEventArgs e)
        {
            System.Windows.Controls.TabControl tabControl = FindParent<System.Windows.Controls.TabControl>(sender as DependencyObject);

            if (tabControl.DataContext is VMMainTab vMSettingTabMain)
            {
                if (sender is DataGrid dgv)
                {
                    DataGridColumn dataGridColumn = dgv.ColumnFromDisplayIndex(0);

                    if (vMSettingTabMain.Header == Define.LabelSystem && tabControl.SelectedIndex == 2)
                        dataGridColumn.Visibility = Visibility.Visible;
                    else
                        dataGridColumn.Visibility = Visibility.Hidden;
                }
            }
        }
    }
}