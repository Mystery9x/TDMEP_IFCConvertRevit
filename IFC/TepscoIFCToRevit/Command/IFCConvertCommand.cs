using Autodesk.Revit.Attributes;
using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Revit.UI.Events;
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
using TepscoIFCToRevit.UI.ViewModels.VMShowDialog;
using TepscoIFCToRevit.UI.Views.ShowDialogUI;

namespace TepscoIFCToRevit.Command
{
    [Transaction(TransactionMode.Manual)]
    public class IFCConvertCommand : IExternalCommand
    {
        private void Ignore_the_diagore(object o, DialogBoxShowingEventArgs e)
        {
            // DialogBoxShowingEventArgs has two subclasses - TaskDialogShowingEventArgs & MessageBoxShowingEventArgs
            // In this case we are interested in this event if it is TaskDialog being shown.
            if (e is DialogBoxShowingEventArgs)
            {
                // Call OverrideResult to cause the dialog to be dismissed with the specified return value
                // (int) is used to convert the enum TaskDialogResult.No to its integer value which is the data type required by OverrideResult
                e.OverrideResult((int)TaskDialogResult.Ok);
            }
        }

        public Result Execute(ExternalCommandData commandData, ref string message, ElementSet elements)
        {
            //if (!CheckLicenseUltils.CheckLicense())
            //{
            //    return Result.Cancelled;
            //}

            App._UIApp = commandData.Application;
            App._UIDoc = App._UIApp.ActiveUIDocument;

            App._UIApp.DialogBoxShowing += new EventHandler<DialogBoxShowingEventArgs>(Ignore_the_diagore);
            ShowIFCConvertDialog();
            App._UIApp.DialogBoxShowing -= new EventHandler<DialogBoxShowingEventArgs>(Ignore_the_diagore);

            return Result.Succeeded;
        }

        public bool ShowIFCConvertDialog()
        {
            try
            {
                if (App.IFCConvDlg != null)
                    return false;

                // Find all revit link instance
                List<RevitLinkInstance> revLnkInss = RevitUtils.GetLinkInstances(App._UIDoc.Document).ToList();

                if (revLnkInss.Count > 0)
                {
                    // Initialize datacontext
                    VMConvertIFCToRevit vMMain = new VMConvertIFCToRevit(App._UIDoc);
                    App.IFCConvDlg = new UI.Views.IFCConvertUI
                    {
                        DataContext = vMMain
                    };

                    if (App.IFCConvDlg.ShowDialog() == true)
                    {
                        try
                        {
                            // Get information ifc object in revit link instance
                            App.Global_IFCObjectMergeData = new IFCObjectData(App._UIDoc, revLnkInss, App.Global_IFCObjectMergeData);

                            using (TransactionGroup group = new TransactionGroup(App._UIDoc.Document, "Load Family For IFC"))
                            {
                                group.Start();
                                // Get mapping setting from Properties Settings

                                VMSettingMain vMSettingMain = SaveSetting.GetMainSettings(revLnkInss);

                                if (vMSettingMain.IsLoadFamily == false)
                                {
                                    foreach (var setCat in vMSettingMain.SettingCategories)
                                    {
                                        if (setCat.ProcessBuiltInCategory == BuiltInCategory.OST_Railings)
                                        {
                                            if (setCat.LoadFamilyForRailing() == true)
                                            {
                                                vMSettingMain.IsLoadFamily = true;
                                            }
                                        }
                                    }
                                }
                                group.Assimilate();

                                List<IFCConvertHandleData> convertIFCDatas = ConvertIfc(vMMain, vMSettingMain, out bool isCancel, out bool isSkip);

                                if (!isCancel && convertIFCDatas?.Count > 0)
                                {
                                    ShowDialog(convertIFCDatas);
                                }
                            }
                        }
                        catch (Exception e)
                        {
                            IO.ShowException(e);
                        }
                    }
                }
                else
                {
                    IO.ShowWanring(Define.TEPSCO_MESS_HAS_NOT_LINK_FILE);
                }
                return true;
            }
            catch (Exception) { }
            return false;
        }

        /// <summary>
        /// convert Ifc elements from all Ifc linked instances
        /// in current project according to saved mapping
        /// </summary>
        /// <param name="vMMain"></param>
        /// <param name="vMSettingMain"></param>
        /// <param name="isCancel"></param>
        /// <param name="isSkip"></param>
        /// <returns></returns>
        private List<IFCConvertHandleData> ConvertIfc(VMConvertIFCToRevit vMMain,
                                                      VMSettingMain vMSettingMain,
                                                      out bool isCancel,
                                                      out bool isSkip)
        {
            isCancel = false;
            isSkip = false;

            List<IFCConvertHandleData> convertIFCDatas = new List<IFCConvertHandleData>();
            foreach (var linkIFC in vMMain.LinkIFCs)
            {
                if (linkIFC.IsChecked == false) continue;
                IFCConvertHandleData convertIFCData = new IFCConvertHandleData(vMSettingMain, vMMain, linkIFC.LinkIFCName.LinkIfc);
                convertIFCDatas.Add(convertIFCData);

                if (convertIFCData.FlagCancel || convertIFCData.FlagSkip)
                {
                    isCancel = convertIFCData.FlagCancel;
                    isSkip = convertIFCData.FlagSkip;
                    break;
                }
            }
            return convertIFCDatas;
        }

        public bool ShowDialog(List<IFCConvertHandleData> convertIFCDatas)
        {
            FileUtils.LoadResources();

            ConvertManagerUI managerUI = new ConvertManagerUI();

            try
            {
                Process process = Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;
                WindowInteropHelper helper = new WindowInteropHelper(managerUI)
                {
                    Owner = intPtr
                };

                managerUI.DataContext = new VMConvertManager(convertIFCDatas, true);
                managerUI.Show();

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }
}