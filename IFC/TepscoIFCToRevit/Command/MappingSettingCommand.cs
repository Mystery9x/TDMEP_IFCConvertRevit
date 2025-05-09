using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Interop;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.Data.SaveSettingData;
using TepscoIFCToRevit.UI.ViewModels;
using TepscoIFCToRevit.UI.ViewModels.VMMappingSetting;
using TepscoIFCToRevit.UI.Views;

namespace TepscoIFCToRevit.Command
{
    [Transaction(TransactionMode.Manual)]
    public class MappingSettingCommand : IExternalCommand
    {
        public static SettingMappingUI dlg;

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //if (!CheckLicenseUltils.CheckLicense())
            //{
            //    return Result.Cancelled;
            //}

            App._UIApp = commandData.Application;
            App._UIDoc = App._UIApp.ActiveUIDocument;

            dlg = new SettingMappingUI();
            ShowMappingSettingDialog();

            return Result.Succeeded;
        }

        /// <summary>
        /// Shows the mapping setting dialog.
        /// </summary>
        /// <returns>True if the dialog was shown; otherwise false.</returns>
        public bool ShowMappingSettingDialog()
        {
            try
            {
                FileUtils.LoadResources();
                // Find all revit link instance
                List<RevitLinkInstance> revLnkInss = RevitUtils.GetLinkInstances(App._UIDoc.Document).ToList();

                if (revLnkInss?.Count > 0)
                {
                    using (TransactionGroup group = new TransactionGroup(App._UIDoc.Document, "Load Family For IFC"))
                    {
                        group.Start();
                        // Initialize datacontext
                        App.Global_IFCObjectMergeData = new IFCObjectData(App._UIDoc, revLnkInss, null);

                        // Get mapping setting from Properties Settings
                        VMSettingMain vMSettingMain = SaveSetting.GetMainSettings(revLnkInss);
                        VMSettingTabMain vmMainView = new VMSettingTabMain(vMSettingMain);

                        // set Revit window as parent for mapping window
                        Process process = Process.GetCurrentProcess();
                        IntPtr intPtr = process.MainWindowHandle;
                        WindowInteropHelper helper = new WindowInteropHelper(dlg)
                        {
                            Owner = intPtr
                        };

                        dlg.DataContext = vmMainView;

                        if (dlg.ShowDialog() == true)
                            group.Assimilate();
                        else
                            group.RollBack();
                    }
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
            finally
            {
                dlg?.Close();
            }
        }
    }
}