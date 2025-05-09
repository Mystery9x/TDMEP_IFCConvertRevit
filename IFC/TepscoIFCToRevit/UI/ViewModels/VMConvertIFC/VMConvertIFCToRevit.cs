using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace TepscoIFCToRevit.UI.ViewModels
{
    public class VMConvertIFCToRevit : BindableBase
    {
        #region Variable

        private readonly UIDocument _uiDoc = null;
        private bool _flagCheckedMEP = true;
        private bool _flagCheckedStructural = true;

        public BuiltInCategory ProcessBuiltInCategory { get; set; }
        public string Tille { get; set; }
        public string CancelContent { get; set; }
        public string OkContent { get; set; }
        public string SelectCategoryMep { get; set; }
        public string SelectCategoryStructure { get; set; }

        #endregion Variable

        #region Property

        private ObservableCollection<VMConvertIFCtoRevTargetObject> targetObjsMEP;

        public ObservableCollection<VMConvertIFCtoRevTargetObject> TargetObjsMEP
        {
            get => targetObjsMEP;
            set => SetProperty(ref targetObjsMEP, value);
        }

        private ObservableCollection<VMConvertIFCtoRevTargetObject> targetObjsStructural;

        public ObservableCollection<VMConvertIFCtoRevTargetObject> TargetObjsStructural
        {
            get => targetObjsStructural;
            set => SetProperty(ref targetObjsStructural, value);
        }

        public bool FlagCheckedMEP
        {
            get => _flagCheckedMEP;
            set
            {
                foreach (var item in TargetObjsMEP)
                {
                    item.IsChecked = value;
                }

                SetProperty(ref _flagCheckedMEP, value);
            }
        }

        public bool FlagCheckedStructural
        {
            get => _flagCheckedStructural;
            set
            {
                foreach (var item in TargetObjsStructural)
                {
                    item.IsChecked = value;
                }

                SetProperty(ref _flagCheckedStructural, value);
            }
        }

        private ObservableCollection<VMConvertObject> _linkIFCs;

        public ObservableCollection<VMConvertObject> LinkIFCs
        {
            get => _linkIFCs;
            set => SetProperty(ref _linkIFCs, value);
        }

        public bool? IsAllItems1Selected
        {
            get
            {
                var selected = LinkIFCs.Select(item => item.IsChecked).Distinct().ToList();
                return selected.Count == 1 ? selected.Single() : (bool?)null;
            }
            set
            {
                if (value.HasValue)
                {
                    SelectAll(value.Value, LinkIFCs);
                    OnPropertyChanged(nameof(IsAllItems1Selected));
                }
            }
        }

        // Apply Click
        public ICommand ApplyCommand { get; set; }

        // Cancel Click
        public ICommand CancelCommand { get; set; }

        #endregion Property

        #region Constructor

        public VMConvertIFCToRevit(UIDocument uIDocument)
        {
            _uiDoc = uIDocument;
            Initialize();
            InitLabels();
        }

        /// <summary>
        /// Initialize all displayed labels and texts based on languages
        /// </summary>
        private void InitLabels()
        {
            Tille = Define.TILLE_CONVERT_IFC_TO_REV;
            CancelContent = Define.COLSE_CONTENT;
            OkContent = Define.Apply_CONTENT;
            SelectCategoryMep = Define.SELECT_CATEGORY_MEP;
            SelectCategoryStructure = Define.SELECT_CATEGORY_STRUCTURE;
        }

        #endregion Constructor

        #region Method

        /// <summary>
        /// Initialize
        /// </summary>
        private void Initialize()
        {
            TargetObjsMEP = new ObservableCollection<VMConvertIFCtoRevTargetObject>
            {
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_PipeCurves)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_DuctCurves)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_GenericModel)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_ElectricalEquipment)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_Railings)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_CableTray)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_PipeAccessory))
            };

            TargetObjsStructural = new ObservableCollection<VMConvertIFCtoRevTargetObject>
            {
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_StructuralColumns)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_Columns)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_Floors)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_Walls)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_StructuralFraming)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_Ceilings)),
                new VMConvertIFCtoRevTargetObject(true, Define.GetCategoryLabel(BuiltInCategory.OST_ShaftOpening))
            };

            LinkIFCs = new ObservableCollection<VMConvertObject>();
            ApplyCommand = new RelayCommand<object>(ApplyCommandInvoke);
            CancelCommand = new RelayCommand<object>(CancelCommandInvoke);

            // Find all revit link instance
            List<RevitLinkInstance> linkModels = new FilteredElementCollector(_uiDoc.Document).OfCategory(BuiltInCategory.OST_RvtLinks)
                                                                                                    .OfClass(typeof(RevitLinkInstance))
                                                                                                    .Cast<RevitLinkInstance>()
                                                                                                    .Where(item => item.GetLinkDocument() != null)
                                                                                                    .ToList();

            foreach (var linkModel in linkModels)
            {
                VMConvertObject vMObj = new VMConvertObject
                {
                    IsChecked = false,
                    LinkIFCName = new SourceLinkIfcData(linkModel)
                };
                LinkIFCs.Add(vMObj);
            }

            foreach (var model in LinkIFCs)
            {
                model.PropertyChanged += (sender, args) =>
                {
                    if (args.PropertyName == nameof(VMConvertObject.IsChecked))
                        OnPropertyChanged(nameof(IsAllItems1Selected));
                };
            }
        }

        /// <summary>
        /// Cancel Command Invoke
        /// </summary>
        /// <param name="obj"></param>
        private void CancelCommandInvoke(object obj)
        {
            if (obj is System.Windows.Window wND)
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
            if (obj is System.Windows.Window wND)
            {
                wND.DialogResult = true;
            }
        }

        private static void SelectAll(bool select, ObservableCollection<VMConvertObject> models)
        {
            foreach (var model in models)
            {
                model.IsChecked = select;
            }
        }

        #endregion Method
    }
}