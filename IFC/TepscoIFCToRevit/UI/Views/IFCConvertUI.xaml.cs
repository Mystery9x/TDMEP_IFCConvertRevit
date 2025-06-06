﻿using System;
using System.Windows;
using System.Windows.Input;
using TepscoIFCToRevit.UI.ViewModels;

namespace TepscoIFCToRevit.UI.Views
{
    /// <summary>
    /// Interaction logic for IFCConvertUI.xaml
    /// </summary>
    public partial class IFCConvertUI : Window
    {
        public IFCConvertUI()
        {
            InitializeComponent();

            if (cb_Mep.Items?.Count > 0)
            {
                cb_Mep.SelectedIndex = 0;
            }
        }

        private void IFCConvertDlg_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                this.Close();
                App.IFCConvDlg = null;
            }
        }

        private void IFCConvertDlg_Closed(object sender, EventArgs e)
        {
            App.IFCConvDlg = null;
        }

        private void ComboBox_DropDownClosed(object sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox combobox)
            {
                combobox.SelectedIndex = 0;
                foreach (var item in combobox.Items)
                {
                    if (item is VMConvertIFCtoRevTargetObject targetObject)
                    {
                        targetObject.VisibilityTarget = Visibility.Hidden;
                        targetObject.VisibilityHint = Visibility.Visible;
                    }
                }
            }
        }

        private void ComboBox_DropDownOpened(object sender, EventArgs e)
        {
            if (sender is System.Windows.Controls.ComboBox combobox)
            {
                combobox.SelectedIndex = -1;
                foreach (var item in combobox.Items)
                {
                    if (item is VMConvertIFCtoRevTargetObject targetObject)
                    {
                        targetObject.VisibilityTarget = Visibility.Visible;
                        targetObject.VisibilityHint = Visibility.Hidden;
                    }
                }
            }
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFile();
        }

        public static void OpenFile()
        {
            var openFileDialog = new Microsoft.Win32.OpenFileDialog
            {
                Title = "Excelファイルを選択",
                Filter = "Excel Files|*.xlsx;*.xls|All Files|*.*",
                FilterIndex = 1,
                DefaultExt = ".xlsx"
            };
        }
    }
}