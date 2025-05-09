using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using Newtonsoft.Json;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.Data.SaveSettingData;
using TepscoIFCToRevit.UI.Views;
using MessageBox = System.Windows.Forms.MessageBox;

namespace TepscoIFCToRevit.UI.ViewModels.VMMappingSetting
{
    public class VMSettingTabMain : BindableBase
    {
        private readonly UIDocument _uiDoc = null;
        public string Title { get; set; }

        private ObservableCollection<VMMainTab> _mainTabs;

        public ObservableCollection<VMMainTab> MainTabs
        {
            get => _mainTabs;
            set => SetProperty(ref _mainTabs, value);
        }

        private VMMainTab _selectedTab;

        public VMMainTab SelectedTab
        {
            get => _selectedTab;
            set => SetProperty(ref _selectedTab, value);
        }

        public static SettingMappingUI dlg;

        public ICommand ApplyCommand { get; set; }
        public ICommand CancelCommand { get; set; }
        public ICommand ImportSettingCommand { get; set; }
        public ICommand ExportSettingCommand { get; set; }

        public VMSettingTabMain(VMSettingMain vMSettingMain)
        {
            dlg = new SettingMappingUI();
            _uiDoc = App._UIDoc;

            MainTabs = new ObservableCollection<VMMainTab>
            {
                new VMMainTab(vMSettingMain, Define.LabelArchitecture),
                new VMMainTab(vMSettingMain, Define.LabelStructure),
                new VMMainTab(vMSettingMain, Define.LabelSystem),
            };

            Initialize();
        }

        private void Initialize()
        {
            Title = Define.TITLE_SETTING;

            ApplyCommand = new RelayCommand<object>(ApplyCommandInvoke);
            CancelCommand = new RelayCommand<object>(CancelCommandInvoke);
            ImportSettingCommand = new RelayCommand<object>(ImportSettingCommandInvoke);
            ExportSettingCommand = new RelayCommand<object>(ExportSettingCommandInvoke);
        }

        private void ExportSettingCommandInvoke(object obj)
        {
            try
            {
                SaveSetting saveSetting = new SaveSetting() { SaveMappingSetting = new List<SaveMappingSetting>() };

                string szToJsonSaveSetting = GetSaveSettingMapping(saveSetting);

                szToJsonSaveSetting = JsonConvert.SerializeObject(saveSetting, Formatting.Indented);

                SaveFileDialog saveFileDialog = new SaveFileDialog
                {
                    Title = Define.SelectPathExport,
                    Filter = "JSON files (*.json)|*.json",
                    DefaultExt = "json",
                    AddExtension = true
                };
                if (saveFileDialog.ShowDialog() == DialogResult.OK)
                {
                    string filePath = saveFileDialog.FileName;

                    try
                    {
                        File.WriteAllText(filePath, szToJsonSaveSetting);
                        MessageBox.Show(Define.ExportSaveSuccess, Define.StatusAlert, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                    catch (Exception)
                    {
                        MessageBox.Show(Define.ExportSaveFail, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    }
                }
            }
            catch (System.Exception)
            {
            }
        }

        private void ImportSettingCommandInvoke(object obj)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog
            {
                Title = Define.SelectFileImport,
                Filter = "JSON files (*.json)|*.json",
                DefaultExt = "json"
            };
            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                string openFilePath = openFileDialog.FileName;

                try
                {
                    if (SetNewSettingMappingByFile(openFilePath, obj, out string tabSelected, out string categorySelected))
                    {
                        if (dlg.DataContext is VMSettingTabMain vMSettingTab)
                        {
                            vMSettingTab.SelectedTab = vMSettingTab.MainTabs.First(x => x.Header == tabSelected);
                            vMSettingTab.SelectedTab.SettingMain.SelectedSettingCategory = vMSettingTab.SelectedTab.SettingMain.SettingCategories.First(x => x.Header == categorySelected);
                        }
                    }

                    MessageBox.Show(Define.FileImportSuccess, Define.StatusAlert, MessageBoxButtons.OK, MessageBoxIcon.Information);
                }
                catch (Exception)
                {
                    MessageBox.Show(Define.FileImportFail, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
        }

        /// <summary>
        /// Cancel Command Invoke
        /// </summary>
        /// <param name="obj"></param>
        private void CancelCommandInvoke(object obj)
        {
            if (obj is Window wND)
            {
                wND.Close();
            }
        }

        /// <summary>
        /// Apply Command Invoke
        /// </summary>
        /// <param name="obj"></param>

        private void ApplyCommandInvoke(object obj)
        {
            if (obj is Window wND)
            {
                try
                {
                    SaveSetting saveSetting = new SaveSetting() { SaveMappingSetting = new List<SaveMappingSetting>() };
                    string szToJson = GetSaveSettingMapping(saveSetting);

                    szToJson = SaveSetting.ToJson(saveSetting);
                    Properties.Settings.Default.SettingMapping = szToJson;
                    Properties.Settings.Default.Save();
                    wND.DialogResult = true;

                    wND.Close();
                    IO.ShowInfor(Define.TEPSCO_MESS_SAVED_SETTING_SUCCESS);
                }
                catch (System.Exception)
                {
                    IO.ShowInfor(Define.TEPSCO_MESS_SAVED_SETTING_FAILED);
                }
            }
        }

        private bool SetNewSettingMappingByFile(string openFilePath, object obj, out string tabSelected, out string categorySelected)
        {
            // Đọc nội dung file JSON vào biến string
            string jsonString = File.ReadAllText(openFilePath);

            string TitleLoadSetting = Define.MESS_PROGESSBAR_PROCESS_LOAD_SETTING;

            // Find all revit link instance
            List<RevitLinkInstance> revLnkInss = RevitUtils.GetLinkInstances(App._UIDoc.Document).ToList();

            foreach (var tab in MainTabs)
            {
                foreach (var item in tab.SettingMain.SettingCategories)
                {
                    if (item.ProcessBuiltInCategory == BuiltInCategory.OST_GenericModel)
                    {
                        item.CheckFamilyPipeSupport(TitleLoadSetting);
                    }
                }
            }

            tabSelected = SelectedTab.Header;
            categorySelected = SelectedTab.SettingMain.SelectedSettingCategory.Header;

            if (obj is Window win)
            {
                // set Revit window as parent for mapping window
                Process process = Process.GetCurrentProcess();
                IntPtr intPtr = process.MainWindowHandle;
                WindowInteropHelper helper = new WindowInteropHelper(dlg)
                {
                    Owner = intPtr
                };

                //var newTabs = SaveSetting.ListMainTabSetting(revLnkInss, jsonString);

                if (win.DataContext is VMSettingTabMain vMSettingTabMain)
                {
                    //// Lưu giá trị SelectedTab hiện tại
                    //var currentSelectedTab = vMSettingTabMain.SelectedTab;

                    VMSettingMain vMSettingMain = SaveSetting.ImportMainSetting(revLnkInss, jsonString);
                    vMSettingTabMain = new VMSettingTabMain(vMSettingMain);

                    //vMSettingTabMain.SelectedTab = currentSelectedTab ?? vMSettingTabMain.MainTabs.FirstOrDefault();

                    win.DataContext = vMSettingTabMain;
                    dlg.DataContext = win.DataContext;
                }

                return true;
            }

            return false;
        }

        private string GetSaveSettingMapping(SaveSetting saveSetting)
        {
            string szToJson = string.Empty;

            try
            {
                List<VMSettingCategory> categories = new List<VMSettingCategory>();

                foreach (var tab in MainTabs)
                {
                    foreach (var setCat in tab.SettingMain.SettingCategories)
                    {
                        SaveMappingSetting saveMappingSetting = new SaveMappingSetting()
                        {
                            ProcessBuiltInCategory = (int)setCat.ProcessBuiltInCategory,
                            SettingGrps = new List<SaveSettingGrp>(),
                            IsCheckedGetEleByCategory = setCat.IsCheckedGetEleByCategory,
                            IsCheckedGetParamByCategory = setCat.IsCheckedGetParamByCategory,
                            SelTypeCaseGetByCategory = setCat.SettingType?.FirstOrDefault() != null && setCat?.SelType != null
                                                    ? setCat.SelType.Id.IntegerValue
                                                    : setCat.BeginSelTypeCaseGetByCategoryId,
                            SelParaKey = setCat.SelParaKey?.Name,
                            NameParameterInRevit = setCat.NameParameterInRevit ?? string.Empty,
                            ValueParameter = setCat.ValueParameter ?? string.Empty,
                            IsCreateManual = setCat.IsCreateManual,
                            ToggelBtnGrp = setCat.ToggleGroupSelection,
                        };

                        foreach (var setGrp in setCat.SettingGrps)
                        {
                            if (setCat.ProcessBuiltInCategory == BuiltInCategory.OST_Railings)
                            {
                                if (setCat.LoadFamilyForRailing() == true)
                                {
                                    tab.SettingMain.IsLoadFamily = true;
                                }
                            }

                            SaveSettingGrp saveSetGrp = new SaveSettingGrp()
                            {
                                SettingObjs = new List<SaveSettingObj>(),
                                Type = new List<SaveTypeElement>(),
                                IsGroupSelection = setGrp is VMSettingGroupSelection,
                            };

                            foreach (var setObj in setGrp.SettingObjs)
                            {
                                SaveSettingObj saveSetObj = new SaveSettingObj()
                                {
                                    FlagContain = setObj.KeyFormat_Contain,
                                    FlagEqual = setObj.KeyFormat_Equal,
                                    KeyValue = setObj.KeyValue
                                };

                                if (setGrp is VMSettingGroupSelection)
                                {
                                    saveSetObj.KeyValue = "";
                                    saveSetObj.FlagEqual = false;
                                    saveSetObj.FlagContain = false;

                                    saveSetObj.LstElementIdSel = setGrp.LstSelectElemId;

                                    if (!string.IsNullOrWhiteSpace(setObj.SelParaKey?.Name))
                                        saveSetObj.SelParaKey = setObj.SelParaKey.Name;
                                    else
                                        saveSetObj.SelParaKey = setObj.BeginSelParaKey;
                                }
                                else
                                {
                                    if (!string.IsNullOrWhiteSpace(setObj.SelParaKey?.Name))
                                        saveSetObj.SelParaKey = setObj.SelParaKey.Name;
                                    else
                                        saveSetObj.SelParaKey = setObj.BeginSelParaKey;
                                }

                                saveSetGrp.SettingObjs.Add(saveSetObj);
                            }

                            SaveTypeElement saveType = new SaveTypeElement
                            {
                                SelType = setGrp.SettingTypeItems?.FirstOrDefault() != null && setGrp.SettingTypeItems?.FirstOrDefault().SelectedSymbol != null
                                                ? setGrp.SettingTypeItems.First().SelectedSymbol.Id.IntegerValue
                                                : setGrp.BeginSelTypeId,

                                NameType = setGrp.SettingTypeItems?.FirstOrDefault() != null && setGrp.SettingTypeItems?.FirstOrDefault().SelectedSymbol != null
                                                ? setGrp.SettingTypeItems.First().SelectedSymbol.Name
                                                : string.Empty
                            };

                            FamilySymbol famSymbol = new FilteredElementCollector(_uiDoc.Document)
                                                    .OfCategory(setCat.ProcessBuiltInCategory)
                                                    .OfClass(typeof(FamilySymbol))
                                                    .Cast<FamilySymbol>().FirstOrDefault(x => x.Id.IntegerValue == saveType.SelType);

                            saveType.SelFamily = famSymbol != null ? famSymbol.Family.Id.IntegerValue : 0;
                            saveType.NameFamily = famSymbol != null ? famSymbol.Family.Name : string.Empty;

                            saveSetGrp.Type.Add(saveType);
                            saveMappingSetting.SettingGrps.Add(saveSetGrp);
                        }

                        saveSetting.SaveMappingSetting.Add(saveMappingSetting);
                    }
                }
            }
            catch (Exception) { }

            return szToJson;
        }
    }

    public class VMMainTab : BindableBase
    {
        public string Header { get; set; }

        private VMSettingMain _settingMain;

        public VMSettingMain SettingMain
        {
            get => _settingMain;
            set => SetProperty(ref _settingMain, value);
        }

        public VMMainTab(VMSettingMain setting, string header)
        {
            Header = header;

            FilterGroupCategory(setting);
        }

        private void FilterGroupCategory(VMSettingMain setting)
        {
            var categories = setting.SettingCategories.Where(x => x.GroupName == Header).ToList();
            SettingMain = new VMSettingMain(categories);
        }
    }
}