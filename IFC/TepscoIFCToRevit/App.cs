#region Namespaces

using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Autodesk.Windows;
using System;
using System.IO;
using System.Reflection;
using System.Windows.Media.Imaging;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.UI.Views;
using RibbonPanel = Autodesk.Revit.UI.RibbonPanel;

#endregion

namespace TepscoIFCToRevit
{
    internal class App : IExternalApplication
    {
        #region Variable

        public static bool LicenseLogin = false;

        public static IFCConvertUI IFCConvDlg = null;
        public static SettingMappingUI MapSetDlg = null;
        public static ProgressBarConvert ProgressBar = null;
        public static IFCObjectData Global_IFCObjectMergeData = null;

        public static UIControlledApplication _UICtrlApp = null;
        public static UIApplication _UIApp = null;
        public static UIDocument _UIDoc = null;

        #endregion

        #region REVIT Event

        public Result OnStartup(UIControlledApplication application)
        {
            _UICtrlApp = application;
            CreateRibbonButtons(application);
            FileUtils.LoadResources();
            return Result.Succeeded;
        }

        public Result OnShutdown(UIControlledApplication application)
        {
            return Result.Succeeded;
        }

        #endregion

        #region Method

        /// <summary>
        /// Create Ribbon Buttons
        /// </summary>
        /// <param name="uIControlledApplication"></param>
        private void CreateRibbonButtons(UIControlledApplication uIControlledApplication)
        {
            string assemblyPath = Assembly.GetExecutingAssembly().Location;
            string iconFolder = GetIconFolder();
            string tabName = Define.TEPSCO_IFC_TO_REV_TABNAME;
            uIControlledApplication.CreateRibbonTab(tabName);

            CreateConverIFCTab(uIControlledApplication, tabName, assemblyPath, iconFolder);
            LicenseLogin = true; // CheckLicenseUltils.CheckLicense(false);
        }

        /// <summary>
        /// Create Conver IFC Tab
        /// </summary>
        /// <param name="uIControlledApplication"></param>
        /// <param name="tabName"></param>
        /// <param name="assemblyPath"></param>
        /// <param name="iconFolder"></param>
        private void CreateConverIFCTab(UIControlledApplication uIControlledApplication,
                                        string tabName,
                                        string assemblyPath,
                                        string iconFolder)
        {
            // Create ribbon panel License
            RibbonPanel LicenseIFCPanel = uIControlledApplication.CreateRibbonPanel(tabName, Define.TEPSCO_IFC_LICENSE_PANELNAME);

            // Button License
            PushButtonData LicenseBtnData = new PushButtonData("btnLogin", Define.ButtonLogin, assemblyPath, Define.TEPSCO_IFC_CMD_LICENSE);
            AddImages(LicenseBtnData, iconFolder, "Signin-01.png", "Signin-01.png");
            LicenseIFCPanel.AddItem(LicenseBtnData);

            // Create ribbon panel Mapping Setting
            RibbonPanel converIFCPanel = uIControlledApplication.CreateRibbonPanel(tabName, Define.TEPSCO_IFC_TO_REV_TABNAME);

            // Button 1 Mapping Setting
            PushButtonData mappingSettingBtnData = new PushButtonData("btnMappingSetting", "マッピング設定", assemblyPath, Define.TEPSCO_IFC_CMD_MAPPING_SETTING);
            AddImages(mappingSettingBtnData, iconFolder, "IconSetting32x32.png", "IconSetting16x16.png");
            converIFCPanel.AddItem(mappingSettingBtnData);

            // Button 2 Convert IFC
            PushButtonData convIFCBtnData = new PushButtonData("btnConvertIFC", "IFCから", assemblyPath, Define.TEPSCO_IFC_CMD_CONVERT_IFC);
            AddImages(convIFCBtnData, iconFolder, "IconConv32x32.png", "IconConv16x16.png");
            converIFCPanel.AddItem(convIFCBtnData);

            //Button 3 Manager
            PushButtonData convIFCListObject = new PushButtonData("btnIFCListObject", "IFCデータ管理", assemblyPath, Define.TEPSCO_IFC_CMD_OBJECT_LIST_IFC);
            AddImages(convIFCListObject, iconFolder, "AecBaseRes32x32.png", "AecBaseRes16x16.png");
            converIFCPanel.AddItem(convIFCListObject);

            // Create ribbon panel Join
            RibbonPanel converIFCPanelJoin = uIControlledApplication.CreateRibbonPanel(tabName, Define.TEPSCO_IFC_TO_REV_TABNAMEJOIN);

            // Button 4 Join
            PushButtonData convIFCJoinGeometry = new PushButtonData("btnIFCJoin", "ジオメトリの結合", assemblyPath, Define.TEPSCO_IFC_CMD_JOIN_IFC);
            AddImages(convIFCJoinGeometry, iconFolder, "IconJoin32x32.png", "IconJoin16x16.png");
            converIFCPanelJoin.AddItem(convIFCJoinGeometry);
        }

        public static void SetImgRibbonButton(string tabName, string panelName, string splitButtonText, string newButtonText, string newImgButton)
        {
            RibbonControl ribbon = ComponentManager.Ribbon;
            foreach (RibbonTab tab in ribbon.Tabs)
            {
                if (tab.Name == tabName)
                {
                    foreach (Autodesk.Windows.RibbonPanel panel in tab.Panels)
                    {
                        if (panel.Source.AutomationName == panelName)
                        {
                            foreach (Autodesk.Windows.RibbonItem item in panel.Source.Items)
                            {
                                if (splitButtonText == item.AutomationName)
                                {
                                    Autodesk.Windows.RibbonItem ribbonButton = item as Autodesk.Windows.RibbonButton;
                                    if (ribbonButton is Autodesk.Windows.RibbonButton rButton)
                                    {
                                        rButton.Text = newButtonText;
                                        rButton.LargeImage = new BitmapImage(new Uri(FileUtils.GetIconsFolder() + "\\" + newImgButton));
                                    }
                                    return;
                                }
                            }
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Get Icon Folder
        /// </summary>
        /// <returns></returns>
        private string GetIconFolder()
        {
            string appDir = GetAppFolder();
            string imageDir = Path.Combine(appDir, "Icons");
            return imageDir;
        }

        /// <summary>
        /// Get App Folder
        /// </summary>
        /// <returns></returns>
        private string GetAppFolder()
        {
            string location = Assembly.GetExecutingAssembly().Location;
            string dir = Path.GetDirectoryName(location);
            if (!Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            return dir;
        }

        /// <summary>
        /// Add Images
        /// </summary>
        /// <param name="buttonData"></param>
        /// <param name="iconFolder"></param>
        /// <param name="largeImage"></param>
        /// <param name="smallImage"></param>
        private void AddImages(ButtonData buttonData,
                               string iconFolder,
                               string largeImage,
                               string smallImage)
        {
            if (!string.IsNullOrEmpty(iconFolder)
                && Directory.Exists(iconFolder))
            {
                string largeImagePath = Path.Combine(iconFolder, largeImage);
                if (File.Exists(largeImagePath))
                    buttonData.LargeImage = new BitmapImage(new Uri(largeImagePath));

                string smallImagePath = Path.Combine(iconFolder, smallImage);
                if (File.Exists(smallImagePath))
                    buttonData.Image = new BitmapImage(new Uri(smallImagePath));
            }
        }

        #endregion
    }
}