using Autodesk.Revit.DB;
using Autodesk.Revit.DB.Mechanical;
using Autodesk.Revit.UI;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Forms;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.Data.SaveSettingData;
using TepscoIFCToRevit.UI.ViewModels.VMMappingSetting;
using TepscoIFCToRevit.UI.Views;
using Visibility = System.Windows.Visibility;

namespace TepscoIFCToRevit.UI.ViewModels
{
    public class VMSettingCategory : BindableBase
    {
        #region Variable & Properties

        public UIDocument UIDocument { get; private set; }
        private Document _doc => UIDocument?.Document;
        public BuiltInCategory ProcessBuiltInCategory { get; set; }

        private ProgressBarLoadFamily _progressBar = null;
        private int _incrementValue = 0;
        private int _sumObjectConvert = 0;

        private Dictionary<VMSetingRevitElement, ObservableCollection<VMSetingRevitElement>> _familyData;

        private string _header;

        public string Header
        {
            get => _header;
            set => SetProperty(ref _header, value);
        }

        public BitmapImage ImgSource { get; set; }

        public string GroupName { get; set; }

        private ObservableCollection<VMSettingGroup> _settingGrps;

        public ObservableCollection<VMSettingGroup> SettingGrps
        {
            get => _settingGrps;
            set => SetProperty(ref _settingGrps, value);
        }

        private ObservableCollection<VMSettingLoadOption> _settingLoadFam;

        public ObservableCollection<VMSettingLoadOption> SettingLoadFam
        {
            get => _settingLoadFam;
            set => SetProperty(ref _settingLoadFam, value);
        }

        private VMSettingGroup _selGrp;

        public VMSettingGroup SelGrp
        {
            get => _selGrp;
            set => SetProperty(ref _selGrp, value);
        }

        private bool _isCreateManual = false;

        public bool IsCreateManual
        {
            get { return _isCreateManual; }
            set
            {
                SettingGrps.ForEach(x => x.IsCreateManual = value);
                SetProperty(ref _isCreateManual, value);

                if (IsCreateManual)
                    TextCreatePipeSupport = Define.LABLE_MANUALLY_CREATE_PIPE_SUPPORT;
                else
                    TextCreatePipeSupport = Define.LABLE_AUTO_CREATE_PIPE_SUPPORT;
            }
        }

        public string _textCreatePipeSupport;

        public string TextCreatePipeSupport
        {
            get => _textCreatePipeSupport;
            set
            {
                SetProperty(ref _textCreatePipeSupport, value);
            }
        }

        private Visibility _isPipingSupport;

        public Visibility IsPipingSupport
        {
            get => _isPipingSupport;
            set => SetProperty(ref _isPipingSupport, value);
        }

        private string _contentSelect;

        public string ContentSelect
        {
            get => _contentSelect;
            set => SetProperty(ref _contentSelect, value);
        }

        private string _contentParam;

        public string ContentParam
        {
            get => _contentParam;
            set => SetProperty(ref _contentParam, value);
        }

        private string _contentLoadFamily;

        public string ContentLoadFamily
        {
            get => _contentLoadFamily;
            set => SetProperty(ref _contentLoadFamily, value);
        }

        private string _nameParameterInRevit;

        public string NameParameterInRevit
        {
            get => _nameParameterInRevit;
            set => SetProperty(ref _nameParameterInRevit, value);
        }

        private string _valueParameter;

        public string ValueParameter
        {
            get => _valueParameter;
            set => SetProperty(ref _valueParameter, value);
        }

        public ObservableCollection<VMSetingRevitElement> Types { get; set; }

        public ObservableCollection<VMSetingRevitElement> Familys { get; set; }

        private string _getElementByCategoryContent = string.Empty;

        public string GetElementByCategoryContent
        {
            get => _getElementByCategoryContent;
            set => SetProperty(ref _getElementByCategoryContent, value);
        }

        private string _getParaByCategory = string.Empty;

        public string GetParaByCategory
        {
            get => _getParaByCategory;
            set => SetProperty(ref _getParaByCategory, value);
        }

        private bool _isCheckedGetEleByCategory = false;

        public bool IsCheckedGetEleByCategory
        {
            get => _isCheckedGetEleByCategory;
            set => SetProperty(ref _isCheckedGetEleByCategory, value);
        }

        private bool _isEnabledGetEleByCategory = false;

        public bool IsEnabledGetEleByCategory
        {
            get => _isEnabledGetEleByCategory;
            set => SetProperty(ref _isEnabledGetEleByCategory, value);
        }

        private bool _isCheckedGetParamByCategory = false;

        public bool IsCheckedGetParamByCategory
        {
            get => _isCheckedGetParamByCategory;
            set => SetProperty(ref _isCheckedGetParamByCategory, value);
        }

        private ObservableCollection<VMSetingRevitElement> _settingType = new ObservableCollection<VMSetingRevitElement>();

        public ObservableCollection<VMSetingRevitElement> SettingType
        {
            get => _settingType;
            set
            {
                SetProperty(ref _settingType, value);
                if (SettingType?.Count > 0)
                    SelType = SettingType.FirstOrDefault();
                else
                    SelType = null;
            }
        }

        private VMSetingRevitElement _selType;

        public VMSetingRevitElement SelType
        {
            get => _selType;
            set => SetProperty(ref _selType, value);
        }

        private VMSetingRevitElement _selFamily;

        public VMSetingRevitElement SelFamily
        {
            get => _selFamily;
            set
            {
                SetProperty(ref _selFamily, value);
                //SET CHANGE
                if (value != null && _familyData.ContainsKey(value))
                    SettingType = _familyData[_selFamily];
            }
        }

        private ObservableCollection<VMSetingRevitElement> _settingFamily = new ObservableCollection<VMSetingRevitElement>();

        public ObservableCollection<VMSetingRevitElement> SettingFamily
        {
            get => _settingFamily;
            set
            {
                SetProperty(ref _settingFamily, value);
            }
        }

        private int _beginSelTypeCaseGetByCategoryId = int.MinValue;

        public int BeginSelTypeCaseGetByCategoryId
        {
            get => _beginSelTypeCaseGetByCategoryId;
            set => _beginSelTypeCaseGetByCategoryId = value;
        }

        private string _notification;

        public SolidColorBrush ChangeColorText { get; private set; }

        public string Notification
        {
            get => _notification;
            set => SetProperty(ref _notification, value);
        }

        private System.Windows.Media.Brush _notificationColor;

        public System.Windows.Media.Brush NotificationColor
        {
            get => _notificationColor;
            set
            {
                _notificationColor = value;
                OnPropertyChanged(nameof(NotificationColor));
            }
        }

        private Brush _colorTextRailing;

        public Brush ColorTextRailing
        {
            get => _colorTextRailing;
            set => SetProperty(ref _colorTextRailing, value);
        }

        private List<string> _famInSourceFolder = new List<string>();

        private Dictionary<string, List<FamilySymbol>> _famInSourceProject = new Dictionary<string, List<FamilySymbol>>();

        private Visibility _visibilitySelType;

        public Visibility VisibilitySelType
        {
            get => _visibilitySelType;
            set => SetProperty(ref _visibilitySelType, value);
        }

        private Visibility _isVisibleCategoryRailing;

        public Visibility IsVisibleCategoryRailing
        {
            get => _isVisibleCategoryRailing;
            set => SetProperty(ref _isVisibleCategoryRailing, value);
        }

        public string _textSelectionOrParam;

        public string TextSelectionOrParam
        {
            get => _textSelectionOrParam;
            set
            {
                SetProperty(ref _textSelectionOrParam, value);
            }
        }

        private bool _toggleGroupSelection;

        public bool ToggleGroupSelection
        {
            get { return _toggleGroupSelection; }
            set
            {
                SetProperty(ref _toggleGroupSelection, value);

                if (_toggleGroupSelection)
                    TextSelectionOrParam = Define.LABLE_SELECTION_ELEMENT;
                else
                    TextSelectionOrParam = Define.LABLE_PARAM_IFC;
            }
        }

        private bool _isGroupSelection = false;

        public bool IsGroupSelection
        {
            get { return _isGroupSelection; }
            set
            {
                SetProperty(ref _isGroupSelection, value);
            }
        }

        public BindableBase _contentGroup;

        /// <summary>
        /// Content will change when data context change
        /// </summary>
        public BindableBase ContentGroup
        {
            get => _contentGroup;
            set => SetProperty(ref _contentGroup, value);
        }

        #region Category Railings

        private Visibility _isRailings;

        public Visibility IsRailings
        {
            get => _isRailings;
            set => SetProperty(ref _isRailings, value);
        }

        private IFCObjectData _model = null;

        private ObservableCollection<ParameterData> _keyParam;

        public ObservableCollection<ParameterData> KeyParam
        {
            get => _keyParam;
            set
            {
                _keyParam = value;
                OnPropertyChanged(nameof(KeyParam));
            }
        }

        private ParameterData _selParaKey;

        public ParameterData SelParaKey
        {
            get => _selParaKey;
            set
            {
                _selParaKey = value;
                OnPropertyChanged(nameof(SelParaKey));
                if (value != null)
                {
                    NameParameterInRevit = SelParaKey.Name;
                }
            }
        }

        #endregion Category Railings

        #region test

        private ObservableCollection<VMSetingRevitElement> _settingSymbolObjs;

        public ObservableCollection<VMSetingRevitElement> SettingSymbolObjs
        {
            get => _settingSymbolObjs;
            set
            {
                SetProperty(ref _settingSymbolObjs, value);
                SelectedSymbol = _settingSymbolObjs.FirstOrDefault();
            }
        }

        private VMSetingRevitElement _selSymbol;

        public VMSetingRevitElement SelectedSymbol
        {
            get => _selSymbol;
            set
            {
                SetProperty(ref _selSymbol, value);
            }
        }

        public SaveMappingSetting SettingSave { get; set; }

        public Dictionary<string, List<string>> NameFamilyPipeSupports { get; set; }

        #endregion test

        #endregion Variable & Properties

        #region commands

        public ICommand AddGroupConditionCommand { get; set; }

        public ICommand RemoveGroupConditionCommand { get; set; }

        public ICommand AddConditionCommand { get; set; }

        public ICommand RemoveConditionCommand { get; set; }

        public ICommand LoadFamilyCommand { get; set; }

        #endregion commands

        #region Constructor

        public VMSettingCategory(UIDocument uIDocument, int intBuiltInCategory, bool isCreateManual = false)
        {
            UIDocument = uIDocument;
            ProcessBuiltInCategory = (BuiltInCategory)intBuiltInCategory;
            IsCreateManual = isCreateManual;
            SettingSave = null;
            Initialize();
        }

        public VMSettingCategory(UIDocument uidoc, SaveMappingSetting settings)
        {
            UIDocument = uidoc;
            ProcessBuiltInCategory = (BuiltInCategory)settings.ProcessBuiltInCategory;

            if (ProcessBuiltInCategory == BuiltInCategory.OST_GenericModel)
                IsCreateManual = settings.IsCreateManual;
            else
                IsCreateManual = true;

            SettingSave = settings;
            Initialize();
        }

        #endregion Constructor

        #region Init

        private void Initialize()
        {
            NameFamilyPipeSupports = ReadNameFamilyPipeSupportFromFileJson();
            SettingLoadFam = new ObservableCollection<VMSettingLoadOption>();
            SettingGrps = new ObservableCollection<VMSettingGroup>();
            Types = new ObservableCollection<VMSetingRevitElement>();
            Familys = new ObservableCollection<VMSetingRevitElement>();

            if (IsCreateManual)
                TextCreatePipeSupport = Define.LABLE_MANUALLY_CREATE_PIPE_SUPPORT;
            else
                TextCreatePipeSupport = Define.LABLE_AUTO_CREATE_PIPE_SUPPORT;

            ToggleGroupSelection = false;

            ContentParam = Define.LABLE_CONTENT_SELECT_PARAM;
            if (ProcessBuiltInCategory == BuiltInCategory.OST_Railings)
            {
                ContentSelect = Define.LABLE_CONTENT_SELECT_TYPE_IS_RAILING;
                ColorTextRailing = System.Windows.Media.Brushes.Red;
                IsEnabledGetEleByCategory = false;
            }
            else
            {
                IsEnabledGetEleByCategory = true;
                ContentSelect = Define.LABLE_CONTENT_SELECT_TYPE;
                ColorTextRailing = System.Windows.Media.Brushes.Black;
            }

            InitCommands();
            InitAutoMappingText();
            InitHeader();
            InitImage();

            IsPipingSupport = ProcessBuiltInCategory == BuiltInCategory.OST_GenericModel
                            ? Visibility.Visible
                            : Visibility.Collapsed;

            IsRailings = ProcessBuiltInCategory == BuiltInCategory.OST_Railings
                            ? Visibility.Visible
                            : Visibility.Hidden;

            VisibilitySelType = ProcessBuiltInCategory == BuiltInCategory.OST_Railings
                            ? Visibility.Collapsed
                            : Visibility.Visible;

            IsVisibleCategoryRailing = ProcessBuiltInCategory == BuiltInCategory.OST_Railings
                            ? Visibility.Hidden
                            : Visibility.Visible;

            InitTypes();
            InitNotification();
        }

        private void InitImage()
        {
            var uri = FileUtils.GetFileIconRevitFolder(ProcessBuiltInCategory);

            ImgSource = new BitmapImage(new Uri(uri));
        }

        private void InitHeader()
        {
            if (ProcessBuiltInCategory == BuiltInCategory.OST_Ceilings)
            {
                Header = Define.GetCategoryLabel(BuiltInCategory.OST_Ceilings);
            }
            else
                Header = Define.GetCategoryLabel(ProcessBuiltInCategory);

            switch (ProcessBuiltInCategory)
            {
                case BuiltInCategory.OST_Columns:
                case BuiltInCategory.OST_Floors:
                case BuiltInCategory.OST_Ceilings:
                case BuiltInCategory.OST_Railings:
                    GroupName = Define.LabelArchitecture;
                    break;

                case BuiltInCategory.OST_StructuralColumns:
                case BuiltInCategory.OST_StructuralFraming:
                case BuiltInCategory.OST_Walls:
                case BuiltInCategory.OST_ShaftOpening:
                    GroupName = Define.LabelStructure;
                    break;

                case BuiltInCategory.OST_PipeCurves:
                case BuiltInCategory.OST_DuctCurves:
                case BuiltInCategory.OST_CableTray:
                case BuiltInCategory.OST_GenericModel:
                case BuiltInCategory.OST_ElectricalEquipment:
                case BuiltInCategory.OST_PipeAccessory:
                    GroupName = Define.LabelSystem;
                    break;
            }

            ContentLoadFamily = Define.CONTENT_COMMAND_LOAD_FAMILY;
        }

        private void InitTypes()
        {
            switch (ProcessBuiltInCategory)
            {
                case BuiltInCategory.OST_StructuralColumns:
                case BuiltInCategory.OST_Columns:
                case BuiltInCategory.OST_StructuralFraming:
                case BuiltInCategory.OST_PipeAccessory:
                    AddSetingRevitElementWithFamily<FamilySymbol>(ProcessBuiltInCategory, null);
                    break;

                case BuiltInCategory.OST_PipeCurves:
                case BuiltInCategory.OST_Floors:
                case BuiltInCategory.OST_Walls:
                case BuiltInCategory.OST_CableTray:
                    AddSetingRevitElement<ElementType>(ProcessBuiltInCategory);
                    break;

                case BuiltInCategory.OST_Ceilings:
#if DEBUG_2020 || RELEASE_2020
                    AddSetingRevitElement<FloorType>(BuiltInCategory.OST_Floors);
#elif DEBUG_2023 || RELEASE_2023
                    AddSetingRevitElement<CeilingType>(BuiltInCategory.OST_Ceilings);
#endif
                    break;

                case BuiltInCategory.OST_DuctCurves:
                    AddSetingRevitElement<DuctType>(BuiltInCategory.OST_DuctCurves,
                                                   (t) => t.Shape != ConnectorProfileType.Invalid
                                                       && t.Shape != ConnectorProfileType.Oval);
                    break;

                case BuiltInCategory.OST_GenericModel:
                    GenericTypePipingSupport();
                    break;

                case BuiltInCategory.OST_ElectricalEquipment:
                    GenericTypeConduitTerminalBox();
                    break;

                case BuiltInCategory.OST_Railings:
                    GenericTypeRailings();
                    break;

                case BuiltInCategory.OST_ShaftOpening:
                    GenericTypeOpening();
                    break;
            }

            _familyData = GroupPipingSupportTypesInProject();
            if (ProcessBuiltInCategory == BuiltInCategory.OST_GenericModel)
            {
                SettingFamily = new ObservableCollection<VMSetingRevitElement>(_familyData.Keys);

                SelFamily = SettingFamily.FirstOrDefault();

                if (SettingSave != null)
                {
                    int? idSelType = SettingSave.SelTypeCaseGetByCategory;

                    if (idSelType != null) // get save setting for family from setting before
                    {
                        bool isBreak = false;
                        foreach (var pair in _familyData)
                        {
                            if (isBreak)
                                break;

                            foreach (var symbol in pair.Value)
                            {
                                if (symbol.Id.IntegerValue == idSelType)
                                {
                                    SelFamily = pair.Key;
                                    isBreak = true;
                                    break;
                                }
                            }
                        }

                        if (!isBreak)
                        {
                            SelFamily = null; // setting default for family
                            SelType = null; // setting default for type
                        }
                    }
                    else
                    {
                        SelFamily = null; // setting default for family
                        SelType = null; // setting default for type
                    }
                }
            }
            else
            {
                Types.ForEach(x => SettingType.Add(x));
            }

            try
            {
                List<RevitLinkInstance> linkModels = ElementQueryUtils.GetAllLinkInstances(_doc);
                IFCObjectData mappingData = new IFCObjectData(UIDocument, linkModels, App.Global_IFCObjectMergeData);

                _model = mappingData;
                KeyParam = _model.KeyParameters;
                if (KeyParam != null)
                {
                    ParameterData ifcMaterial = null;

                    foreach (var item in KeyParam)
                    {
                        if (item.Name == "IfcMaterial")
                        {
                            ifcMaterial = item;
                        }
                    }
                    if (SettingSave != null)
                    {
                        if (SettingSave.SelParaKey != string.Empty)
                            SelParaKey = mappingData.KeyParameters.FirstOrDefault(item => item.Name == (SettingSave.SelParaKey));

                        if (SettingSave.NameParameterInRevit != null)
                            NameParameterInRevit = SettingSave.NameParameterInRevit;
                        if (SettingSave.ValueParameter != null)
                            ValueParameter = SettingSave.ValueParameter;
                    }
                    else if (SelParaKey == null)
                    {
                        if (ifcMaterial != null)
                            SelParaKey = ifcMaterial;
                        else
                            SelParaKey = KeyParam.FirstOrDefault();

                        NameParameterInRevit = SelParaKey.Name;
                    }
                }
            }
            catch (Exception) { }
        }

        private void InitAutoMappingText()
        {
            string category = ProcessBuiltInCategory == BuiltInCategory.OST_GenericModel ? "一般モデル"
                            : ProcessBuiltInCategory == BuiltInCategory.OST_PipeAccessory ? "配管付属品"
                            : Define.GetCategoryLabel(ProcessBuiltInCategory);

            GetParaByCategory = Define.TEPSCO_HEADER_GET_PARAM_BY_CATEGORY + category;
            GetElementByCategoryContent = Define.TEPSCO_HEADER_GET_ELEMENT_BY_CATEGORY + category;
        }

        private void InitCommands()
        {
            AddGroupConditionCommand = new RelayCommand<object>(AddGroupConditionCommandInvoke);
            RemoveGroupConditionCommand = new RelayCommand<object>(RemoveGroupConditionCommandInvoke);
            AddConditionCommand = new RelayCommand<object>(AddConditionCommandInvoke);
            RemoveConditionCommand = new RelayCommand<object>(RemoveConditionCommandInvoke);
            LoadFamilyCommand = new RelayCommand<object>(CommandLoadAndCheckInvoke);
        }

        private void InitNotification()
        {
            if (ProcessBuiltInCategory == BuiltInCategory.OST_Ceilings)
            {
#if DEBUG_2020 || RELEASE_2020
                NotificationColor = System.Windows.Media.Brushes.Red;
                Notification = Define.MESS_CREATE_TYPE_FLOOR_CHANGE_TYPE_CELLING;
#endif
            }
            else if (ProcessBuiltInCategory == BuiltInCategory.OST_GenericModel)
            {
                _famInSourceProject = GetAllFamPipeSupportInProject();

                _famInSourceFolder = Directory.GetFiles(FileUtils.GetPipingSupportFamilyFolder(), "*.rfa", SearchOption.AllDirectories).ToList();

                Dictionary<string, List<string>> missFamilys = GetFamilyPipeSupportMiss(NameFamilyPipeSupports);
                if (missFamilys?.Count > 0
                    || _famInSourceProject?.Count == 0)
                {
                    Notification = Define.MESS_LOAD_FAMILY_PIPE_SUPPORT_BEFORE_MAPPING;
                    NotificationColor = System.Windows.Media.Brushes.Red;
                }
                else
                {
                    Notification = Define.MESS_HAS_BEEN_LOAD_FAMILY;
                    NotificationColor = System.Windows.Media.Brushes.Green;
                }
            }
        }

        public Dictionary<string, List<string>> ReadNameFamilyPipeSupportFromFileJson()
        {
            string familyDir = FileUtils.GetFamilyFolder();
            string path = Path.Combine(familyDir, "NameFamilyPipeSupport" + ".json");

            Dictionary<string, List<string>> nameFamilys = new Dictionary<string, List<string>>();
            if (File.Exists(path))
                FileUtils.ReadJSONCad2DData(path, out nameFamilys);

            return nameFamilys;
        }

        public Dictionary<string, List<string>> GetFamilyPipeSupportMiss(Dictionary<string, List<string>> nameFamilys)
        {
            Dictionary<string, List<string>> familyMissInProjects = new Dictionary<string, List<string>>();

            _famInSourceProject = GetAllFamPipeSupportInProject();

            if (nameFamilys?.Count > 0
                && _famInSourceProject?.Count >= 0)
            {
                string direction = FileUtils.GetPipingSupportFamilyFolder();
                foreach (var pair in nameFamilys)
                {
                    string directionFamilyMiss = string.Empty;
                    foreach (var directionFamily in _famInSourceFolder)
                    {
                        string directionJson = direction + "\\" + pair.Key + ".rfa";
                        if (string.Equals(directionFamily, directionJson))
                            directionFamilyMiss = directionFamily;
                    }

                    if (!string.IsNullOrEmpty(directionFamilyMiss))
                    {
                        if (_famInSourceProject.Keys.Contains(pair.Key))
                        {
                            List<string> symbolExits = new List<string>();
                            if (_famInSourceProject[pair.Key]?.Count > 0)
                            {
                                _famInSourceProject[pair.Key].ForEach(x => symbolExits.Add(x.Name));
                            }

                            foreach (var nameSymbol in pair.Value)
                            {
                                if (!symbolExits.Contains(nameSymbol))
                                {
                                    if (familyMissInProjects.ContainsKey(directionFamilyMiss))
                                        familyMissInProjects[directionFamilyMiss].Add(nameSymbol);
                                    else
                                        familyMissInProjects.Add(directionFamilyMiss, new List<string>() { nameSymbol });
                                }
                            }
                        }
                        else
                            familyMissInProjects.Add(directionFamilyMiss, new List<string>());
                    }
                }
            }
            return familyMissInProjects;
        }

        #endregion Init

        #region Load and Active Family

        private void GenericTypeOpening()
        {
            using (Transaction transaction = new Transaction(_doc, "Load Family Opening"))
            {
                transaction.Start();
                CheckFamilyOpening();
                AddSetingRevitElementWithFamily<FamilySymbol>(BuiltInCategory.OST_GenericModel, IsValidOpeningType);
                transaction.Commit();
            }
        }

        public void GenericTypePipingSupport()
        {
            string folder = FileUtils.GetPipingSupportFamilyFolder();
            var famNames = new List<string>();

            if (Directory.Exists(folder))
            {
                famNames = Directory.GetFiles(folder, "*.rfa", SearchOption.AllDirectories)
                                    .Select(x => Path.GetFileNameWithoutExtension(x))
                                    .ToList();
            }

            AddSetingRevitElement<FamilySymbol>(BuiltInCategory.OST_GenericModel, (x) => famNames.Contains(x.FamilyName));
        }

        public void GenericTypeConduitTerminalBox()
        {
            using (Transaction transaction = new Transaction(_doc, "Load Family ElectricalEquipment"))
            {
                transaction.Start();
                CheckFamilyConduitTerminalBox();
                AddSetingRevitElementWithFamily<FamilySymbol>(BuiltInCategory.OST_ElectricalEquipment, IsValidconduitTerminalBoxType);
                AddSetingRevitElementWithFamily<FamilySymbol>(BuiltInCategory.OST_MechanicalEquipment, IsValidconduitTerminalBoxType);
                transaction.Commit();
            }
        }

        private bool IsValidOpeningType(FamilySymbol famSym)
        {
            return GetOpeningFamilyTypeNames()
                    .Any(x => famSym.FamilyName == x.Key && famSym.Name == x.Value);
        }

        private bool IsValidconduitTerminalBoxType(FamilySymbol famSym)
        {
            return GetConduitTerminalFamilyTypeNames()
                    .Any(x => famSym.FamilyName == x.Key && famSym.Name == x.Value);
        }

        private void CheckFamilyOpening()
        {
            string familyDir = FileUtils.GetOpeningFamilyFolder();
            List<FamilySymbol> famSymsElectric = ElementQueryUtils.GetTypeByCategory<FamilySymbol>(_doc, BuiltInCategory.OST_GenericModel);

            GetOpeningFamilyTypeNames()
                .Where(x => !IsFamilyLoaded(x.Key, x.Value, famSymsElectric))
                .ForEach(x =>
                {
                    string path = Path.Combine(familyDir, x.Key + ".rfa");
                    if (File.Exists(path))
                        LoadFamily(path, out Family family);
                });
        }

        private void CheckFamilyConduitTerminalBox()
        {
            string familyDir = FileUtils.GetElectricalEquipmentFamilyFolder();

            List<FamilySymbol> famSymsElectric = ElementQueryUtils.GetTypeByCategory<FamilySymbol>(_doc, BuiltInCategory.OST_ElectricalEquipment);
            List<FamilySymbol> famSymMechanical = ElementQueryUtils.GetTypeByCategory<FamilySymbol>(_doc, BuiltInCategory.OST_MechanicalEquipment);

            famSymMechanical.ForEach(x => famSymsElectric.Add(x));

            GetConduitTerminalFamilyTypeNames()
                .Where(x => !IsFamilyLoaded(x.Key, x.Value, famSymsElectric))
                .ForEach(x =>
                {
                    string path = Path.Combine(familyDir, x.Key + ".rfa");
                    if (File.Exists(path))
                        LoadFamily(path, out Family family);
                });
        }

        private void GenericTypeRailings()
        {
            var lstTypeRailings = new Dictionary<string, ElementId> {
                {RaillingType.Auto.ToString(),  ElementId.InvalidElementId},
                {RaillingType.Pipe.ToString(),  ElementId.InvalidElementId},
                {RaillingType.Duct.ToString(), ElementId.InvalidElementId},
                {RaillingType.ModelInPlace.ToString() , ElementId.InvalidElementId}
            };

            lstTypeRailings.ForEach(x => Types.Add(new VMSetingRevitElement(x.Key, x.Value)));
        }

        public bool LoadFamilyForRailing()
        {
            try
            {
                using (Transaction trans = new Transaction(UIDocument.Document, "Load Family Fitting"))
                {
                    trans.Start();
                    string familyDir = FileUtils.GetRailingsFamilyFolder();
                    List<FamilySymbol> famSymsPipe = ElementQueryUtils.GetTypeByCategory<FamilySymbol>(UIDocument.Document, BuiltInCategory.OST_PipeFitting);
                    FamilyPipeFitting().Where(x => !IsFamilyLoaded(x.Key, x.Value, famSymsPipe))
                                                                            .ForEach(x =>
                                                                            {
                                                                                string path = Path.Combine(familyDir, x.Key + ".rfa");
                                                                                if (File.Exists(path))
                                                                                    LoadFamily(path, out Family family);
                                                                            });

                    List<FamilySymbol> famSymsDuct = ElementQueryUtils.GetTypeByCategory<FamilySymbol>(UIDocument.Document, BuiltInCategory.OST_DuctFitting);
                    FamilyDuctFitting().Where(x => !IsFamilyLoaded(x.Key, x.Value, famSymsDuct))
                                                                            .ForEach(x =>
                                                                            {
                                                                                string path = Path.Combine(familyDir, x.Key + ".rfa");
                                                                                if (File.Exists(path))
                                                                                    LoadFamily(path, out Family family);
                                                                            });

                    trans.Commit();
                }

                return true;
            }
            catch (Exception)
            {
                return false;
            }
        }

        public Dictionary<string, string> FamilyPipeFitting()
        {
            var dic = new Dictionary<string, string>
             {
                  { "Family-Pipe-Fitting-Tee", "Family-Pipe-Fitting-Tee" },
                  { "Family-PipeFitting-Crossroads", "Family-PipeFitting-Crossroads" },
                  { "Family-PipeFitting-Elbow", "Family-PipeFitting-Elbow" },
             };
            return dic;
        }

        public Dictionary<string, string> FamilyDuctFitting()
        {
            var dic = new Dictionary<string, string>
            {
                  { "DuctFitting_Rectangle_Cross", "DuctFitting_Rectangle_Cross" },
                  { "DuctFitting_Rectangle_Elbow", "DuctFitting_Rectangle_Elbow" },
                  { "DuctFitting_Rectangle_Tee", "DuctFitting_Rectangle_Tee" },
                  { "DuctFitting_Round_Cross", "DuctFitting_Round_Cross" },
                  { "DuctFitting_Round_Elbow", "DuctFitting_Round_Elbow" },
                  { "DuctFitting_Round_Wye", "DuctFitting_Round_Wye" },
            };
            return dic;
        }

        public bool IsFamilyLoaded(string famName, string typeName, List<FamilySymbol> symbols)
        {
            return symbols.Any(x => x.FamilyName == famName && x.Name == typeName);
        }

        private Dictionary<string, string> GetOpeningFamilyTypeNames()
        {
            var dic = new Dictionary<string, string>
            {
                { "貫通部_丸形", "貫通部_丸形" },
                { "貫通部_角形", "貫通部_角形" },
            };
            return dic;
        }

        private Dictionary<string, string> GetConduitTerminalFamilyTypeNames()
        {
            var dic = new Dictionary<string, string>
            {
                { "21300_制御盤", "両扉" },
                { "M_電線管端子箱 - ティー - アルミニウム_2", "標準" },
                { "M_電線管端子箱 - 置換 - アルミニウム", "標準 2" },
                { "M_電線管端子箱 - 置換 - アルミニウム_1", "標準 2" },
                { "プルボックス", "プルボックス" },
                { "盤", "盤" },
                { "盤_機械設備", "盤_機械設備" },
                { "conduit", "標準" },
            };
            return dic;
        }

        public bool LoadFamily(string filePath, out Family family) => _doc.LoadFamily(filePath, out family);

        #region add piping support familys

        private void LoadPipingSupportFamily()
        {
            string TitleProcessBar = Define.MESS_PROGESSBAR_PROCESS_TITLE;

            StartProgressBarLoad(TitleProcessBar);
            _incrementValue = 0;
            _progressBar.Show();

            _famInSourceFolder.ForEach(x =>
            {
                var famLoadOption = new VMSettingLoadOption();

                if (!_progressBar.IsCancel)
                {
                    _sumObjectConvert = _famInSourceFolder.Count;
                    _incrementValue++;
                    _doc.LoadFamily(x, famLoadOption, out Family family);
                    string mess = string.Format("{0} / {1} {2}", _incrementValue, _sumObjectConvert, Path.GetFileNameWithoutExtension(x));
                    _progressBar.SetFamilyMessage(mess);
                    _progressBar.IncrementProgressBarLoad();
                }
            });

            if (_progressBar.IsCancel)
            {
                _progressBar.Dispose();
            }
            else
            {
                _progressBar.SetFamilyMessage(Define.LoadFamilyComplete);
            }

            UpdateDataToView();

            _progressBar?.Dispose();
        }

        private void LoadPipingSupportOverWriteSymbol(Dictionary<string, List<string>> missFamilys, string TitleProcessBar)
        {
            StartProgressBarLoad(TitleProcessBar);
            _incrementValue = 0;
            _progressBar.Show();

            _sumObjectConvert = missFamilys.Count;
            foreach (var pair in missFamilys)
            {
                if (!_progressBar.IsCancel)
                {
                    var famLoadOption = new VMSettingLoadOption();

                    _incrementValue++;

                    if (pair.Value?.Count > 0)
                    {
                        foreach (var nameSymbol in pair.Value)
                        {
                            _doc.LoadFamilySymbol(pair.Key, nameSymbol, famLoadOption, out FamilySymbol _);
                        }
                    }
                    else
                        _doc.LoadFamily(pair.Key, famLoadOption, out Family _);

                    string mess = string.Format("{0} / {1} {2}", _incrementValue, _sumObjectConvert, Path.GetFileNameWithoutExtension(pair.Key));
                    _progressBar.SetFamilyMessage(mess);
                    _progressBar.IncrementProgressBarLoad();
                }
            }

            if (_progressBar.IsCancel)
            {
                _progressBar.Dispose();
            }
            else
            {
                _progressBar.SetFamilyMessage(Define.LoadFamilyComplete);
            }

            _doc.Regenerate();

            UpdateDataToView();

            _progressBar?.Dispose();
        }

        private void UpdateDataToView()
        {
            _familyData = GroupPipingSupportTypesInProject();

            if (SettingGrps?.Count > 0)
            {
                for (int i = 0; i < SettingGrps.Count; i++)
                {
                    int? idSelType = null;
                    if (SettingSave != null
                        && SettingSave.SettingGrps?.Count == SettingGrps.Count
                        && SettingSave.SettingGrps[i].Type?.Count > 0)
                    {
                        idSelType = SettingSave.SettingGrps[i].Type[0].SelType;
                    }

                    SettingGrps[i].UpdateLoadFamilies(_familyData, idSelType);
                }
            }

            AddSetingRevitElement<FamilySymbol>(BuiltInCategory.OST_GenericModel, (x) => _famInSourceFolder.Contains(x.Name));
            SettingFamily = new ObservableCollection<VMSetingRevitElement>(_familyData.Keys);

            SelFamily = SettingFamily.FirstOrDefault();

            if (SettingSave != null)
            {
                int? idSelType = SettingSave.SelTypeCaseGetByCategory;

                if (idSelType != null) // get save setting for family from setting before
                {
                    bool isBreak = false;
                    foreach (var pair in _familyData)
                    {
                        if (isBreak)
                            break;

                        foreach (var symbol in pair.Value)
                        {
                            if (symbol.Id.IntegerValue == idSelType)
                            {
                                SelFamily = pair.Key;
                                isBreak = true;
                                break;
                            }
                        }
                    }

                    if (!isBreak)
                    {
                        SelFamily = null; // setting default for family
                        SelType = null; // setting default for type
                    }
                }
            }
            else
            {
                SelFamily = null; // setting default for family
                SelType = null; // setting default for type
            }

            InitNotification();
        }

        private void StartProgressBarLoad(string TitleProcessBar)
        {
            // Initialize Progress Bar Load
            System.Diagnostics.Process process = System.Diagnostics.Process.GetCurrentProcess();
            IntPtr intPtr = process.MainWindowHandle;
            _progressBar = new ProgressBarLoadFamily(TitleProcessBar, Define.LABLE_PROCESS);

            _progressBar.prgSingleLoadFamily.Minimum = 1;
            _progressBar.prgSingleLoadFamily.Maximum = _famInSourceFolder.Count;
            _progressBar.prgSingleLoadFamily.Value = 1;

            // set Revit window as parent for progressBar window
            WindowInteropHelper helper = new WindowInteropHelper(_progressBar)
            {
                Owner = intPtr
            };
        }

        private Dictionary<VMSetingRevitElement, ObservableCollection<VMSetingRevitElement>> GroupPipingSupportTypesInProject()
        {
            return new FilteredElementCollector(_doc)
                    .OfClass(typeof(Family))
                    .Cast<Family>()
                    .Where(x => x.FamilyCategory.Id.IntegerValue == (int)BuiltInCategory.OST_GenericModel
                                && NameFamilyPipeSupports.ContainsKey(x.Name))
                    .ToDictionary(x => new VMSetingRevitElement(x), x => GetTypesFromFamily(x));
        }

        private Dictionary<string, List<FamilySymbol>> GetAllFamPipeSupportInProject()
        {
            return ElementQueryUtils.GetTypeByCategory<FamilySymbol>(_doc, BuiltInCategory.OST_GenericModel)
                                    .GroupBy(x => x.Family.Name)
                                    .Where(x => NameFamilyPipeSupports.ContainsKey(x.Key))
                                    .ToDictionary(x => x.Key, x => x.ToList());
        }

        private ObservableCollection<VMSetingRevitElement> GetTypesFromFamily(Family family)
        {
            var symbols = family.GetFamilySymbolIds()
                                .Select(id => App._UIDoc.Document.GetElement(id))
                                .Select(x => new VMSetingRevitElement(x));
            ObservableCollection<VMSetingRevitElement> col = new ObservableCollection<VMSetingRevitElement>(symbols);
            return col;
        }

        #endregion add piping support familys

        #endregion Load and Active Family

        #region Condition

        private void RemoveConditionCommandInvoke(object obj)
        {
            if (SelGrp != null
                && SelGrp.SelObj != null
                && SelGrp.SettingObjs != null)
            {
                if (SelGrp.SettingObjs?.Count > 1)
                    SelGrp.SettingObjs.Remove(SelGrp.SelObj);
                else if (SelGrp.SettingObjs?.Count == 1)
                {
                    if (SelGrp.SelObj != null)
                    {
                        //SelGrp.SelObj.SelParaKey = null;
                        SelGrp.SelObj.KeyValue = String.Empty;
                    }
                }
            }
        }

        private void AddConditionCommandInvoke(object obj)
        {
            if (UIDocument != null
                && SelGrp != null
                && SelGrp.SettingObjs != null
                && SettingGrps != null
                && SelGrp is VMSettingGroupCondition)
            {
                List<RevitLinkInstance> linkModels = ElementQueryUtils.GetAllLinkInstances(_doc);
                if (linkModels != null && linkModels.Count > 0)
                {
                    IFCObjectData mappingData = new IFCObjectData(UIDocument, linkModels, App.Global_IFCObjectMergeData);
                    VMSettingIfc vMSetObj = new VMSettingIfc(mappingData, SelGrp);
                    SelGrp.SettingObjs.Add(vMSetObj);
                }
            }
        }

        private void RemoveGroupConditionCommandInvoke(object obj)
        {
            if (SettingGrps != null && SelGrp != null)
            {
                SettingGrps.Remove(SelGrp);
            }
        }

        private void AddGroupConditionCommandInvoke(object obj)
        {
            if (UIDocument != null && SettingGrps != null)
            {
                List<RevitLinkInstance> linkModels = ElementQueryUtils.GetAllLinkInstances(_doc);

                VMSettingGroup vMSetGrp;

                if (!ToggleGroupSelection)
                {
                    vMSetGrp = new VMSettingGroupCondition(this);
                    (vMSetGrp as VMSettingGroupCondition).ContentGroup = vMSetGrp as VMSettingGroupCondition;
                    IsGroupSelection = false;
                }
                else
                {
                    vMSetGrp = new VMSettingGroupSelection(this);
                    (vMSetGrp as VMSettingGroupSelection).ContentGroup = vMSetGrp as VMSettingGroupSelection;
                    IsGroupSelection = true;
                }

                IFCObjectData mappingData = new IFCObjectData(UIDocument, linkModels, App.Global_IFCObjectMergeData);
                VMSettingIfc vMSetObj = new VMSettingIfc(mappingData, vMSetGrp);

                //vMSetGrp.ContentGroup = vMSetGrp;

                vMSetGrp.SettingObjs.Add(vMSetObj);
                SettingGrps.Add(vMSetGrp);
                SetProperty(ref _settingGrps, _settingGrps);
            }
        }

        private void CommandLoadAndCheckInvoke(object obj)
        {
            using (Transaction trans = new Transaction(_doc))
            {
                trans.Start("load family pipe support");

                _famInSourceProject = GetAllFamPipeSupportInProject();

                string TitleProcessBar = Define.MESS_PROGESSBAR_PROCESS_TITLE;

                if (obj is Window win)
                {
                    win.IsEnabled = false;

                    win.Topmost = false;

                    if (!CheckFamilyPipeSupport(TitleProcessBar))
                    {
                        if (_famInSourceProject?.Count > 0 || _famInSourceProject.Count == _famInSourceFolder.Count)
                        {
                            DialogResult dialogResult = RevitUtilities.IO.ShowQuestion(Define.MESS_OVERWRITE_FAMILY, Define.CONTENT_COMMAND_LOAD_FAMILY);

                            if (dialogResult == DialogResult.Yes)
                                LoadPipingSupportFamily();
                        }
                        else
                            LoadPipingSupportFamily();
                    }

                    win.Topmost = true;

                    win.IsEnabled = true;
                }

                trans.Commit();
            }
        }

        public bool CheckFamilyPipeSupport(string TitleProcessBar)
        {
            Dictionary<string, List<string>> missFamilys = GetFamilyPipeSupportMiss(NameFamilyPipeSupports);

            if (missFamilys?.Count > 0)
            {
                try
                {
                    using (Transaction trans = new Transaction(_doc, "Load Over Write family"))
                    {
                        trans.Start();

                        LoadPipingSupportOverWriteSymbol(missFamilys, TitleProcessBar);

                        trans.Commit();
                        return true;
                    }
                }
                catch (Exception)
                {
                }
            }

            return false;
        }

        #endregion Condition

        #region Utils

        private void AddSetingRevitElement<T>(BuiltInCategory category, Func<T, bool> condition = null) where T : ElementType
        {
            ElementQueryUtils.GetTypeByCategory<T>(_doc, category, condition)
                             .ForEach(x => Types.Add(new VMSetingRevitElement(x)));
        }

        private void AddSetingRevitElementWithFamily<T>(BuiltInCategory category, Func<T, bool> condition = null) where T : FamilySymbol
        {
            string separator = " | ";
            ElementQueryUtils.GetTypeByCategory<T>(_doc, category, condition)
                            .ForEach(x =>
                            {
                                string name = x.FamilyName + separator + x.Name;
                                Types.Add(new VMSetingRevitElement(name, x.Id));
                            });
        }

        #endregion Utils
    }
}