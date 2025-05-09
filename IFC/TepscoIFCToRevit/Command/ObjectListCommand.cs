using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Interop;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.UI.ViewModels.VMShowDialog;
using TepscoIFCToRevit.UI.Views.ShowDialogUI;

namespace TepscoIFCToRevit.Command
{
    [Transaction(TransactionMode.Manual)]
    public class ObjectListCommand : IExternalCommand
    {
        private ConvertManagerUI formDlg;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //if (!CheckLicenseUltils.CheckLicense())
            //{
            //    return Result.Cancelled;
            //}

            App._UIApp = commandData.Application;
            App._UIDoc = App._UIApp.ActiveUIDocument;

            FileUtils.LoadResources();

            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Excel files (*.xlsx;*.xls;*.xlsm;*.xlt)|*.xlsx;*.xls;*.xlsm;*.xlt";
            openFileDialog.FilterIndex = 0;
            openFileDialog.Title = Define.ImportExcelFile;

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                formDlg = new ConvertManagerUI();
                ShowObjectDialog(openFileDialog.FileName);
            }

            return Result.Succeeded;
        }

        public bool ShowObjectDialog(string filePath)
        {
            try
            {
                // Find all revit link instance
                List<RevitLinkInstance> revLnkInss = RevitUtils.GetLinkInstances(App._UIDoc.Document).ToList();

                if (revLnkInss?.Count > 0)
                {
                    // Initialize datacontext
                    App.Global_IFCObjectMergeData = new IFCObjectData(App._UIDoc, revLnkInss, null);

                    // set Revit window as parent for mapping window
                    Process process = Process.GetCurrentProcess();
                    IntPtr intPtr = process.MainWindowHandle;
                    WindowInteropHelper helper = new WindowInteropHelper(formDlg);
                    helper.Owner = intPtr;

                    formDlg.DataContext = new VMConvertManager(null, false, filePath);

                    formDlg.Show();
                }
                else
                {
                    IO.ShowWanring(Define.TEPSCO_MESS_HAS_NOT_LINK_FILE);
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}