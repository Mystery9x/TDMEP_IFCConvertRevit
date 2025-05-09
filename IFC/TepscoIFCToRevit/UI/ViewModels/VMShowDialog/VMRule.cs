using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.Data.CableTraysData;
using TepscoIFCToRevit.Data.RailingsData;
using Visibility = System.Windows.Visibility;

namespace TepscoIFCToRevit.UI.ViewModels.VMShowDialog
{
    public class VMRule : BindableBase
    {
        public UIDocument _uiDoc;
        public Document _doc;

        public string NameCategory { get; set; }

        private int _countLstElement;

        public int CountLstElement
        {
            get => _countLstElement;
            set
            {
                SetProperty(ref _countLstElement, value);
                VisibilityNoData = _countLstElement > 0 ? Visibility.Collapsed : Visibility.Visible;
            }
        }

        private bool _showSuccess;

        public bool ShowSuccess
        {
            get { return _showSuccess; }
            set
            {
                SetProperty(ref _showSuccess, value);
                FilterItems();
            }
        }

        private double _heightNoData;

        public double HeightNoData
        {
            get { return _heightNoData; }
            set
            {
                SetProperty(ref _heightNoData, value);
            }
        }

        private ObservableCollection<VMElement> _displayLstElement;

        public ObservableCollection<VMElement> DisplayLstElement
        {
            get { return _displayLstElement; }
            set { SetProperty(ref _displayLstElement, value); }
        }

        public ObservableCollection<VMElement> OriginalList { get; set; }

        public string SelectedSuccess { get; set; }
        public string SelectedFailed { get; set; }

        private VMElement _selElement;

        public VMElement SelElement
        {
            get => _selElement;
            set
            {
                SetProperty(ref _selElement, value);

                if (ShowSuccess && value != null)
                    SelectedSuccess = value.Id;
                else if (!ShowSuccess && value != null)
                    SelectedFailed = value.Id;
            }
        }

        private Visibility _visibilityNoData;

        public Visibility VisibilityNoData
        {
            get => _visibilityNoData;
            set => SetProperty(ref _visibilityNoData, value);
        }

        public VMRule(UIDocument uidoc, string nameCategory,
                      List<IFCConvertHandleData> iFCConvertHandleDatas,
                      BuiltInCategory builtInCategory)
        {
            _uiDoc = uidoc;
            _doc = uidoc.Document;
            NameCategory = nameCategory;
            DisplayLstElement = new ObservableCollection<VMElement>();
            OriginalList = LoadObjectConvertData(builtInCategory, iFCConvertHandleDatas);
            CountLstElement = DisplayLstElement.Count(x => x.Status == Define.StatusSuccess);
            ShowSuccess = true;
        }

        public VMRule(UIDocument uidoc, string nameCategory, BuiltInCategory builtInCategory, XLWorkbook workbook)
        {
            _uiDoc = uidoc;
            _doc = uidoc.Document;
            NameCategory = nameCategory;
            DisplayLstElement = new ObservableCollection<VMElement>();
            OriginalList = new ObservableCollection<VMElement>();

            ImportObjectData(workbook, builtInCategory);

            CountLstElement = DisplayLstElement.Count(x => x.Status == Define.StatusSuccess);
            ShowSuccess = true;
        }

        public BitmapImage ToggleButtonImage
        {
            get
            {
#if DEBUG_2020
                var uri = "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2020\\TepscoIFCToRevit\\Icons\\down.png";
#elif DEBUG_2023
                var uri = "C:\\Users\\trong\\AppData\\Roaming\\Autodesk\\Revit\\Addins\\2023\\TepscoIFCToRevit\\Icons\\down.png";
#else
                var uri = FileUtils.GetFileIconDownFolder();
#endif
                return new BitmapImage(new Uri(uri));
            }
        }

        private void FilterItems()
        {
            if (OriginalList?.Count > 0)
            {
                if (ShowSuccess)
                {
                    DisplayLstElement = new ObservableCollection<VMElement>(OriginalList.Where(item => item.Status == Define.StatusSuccess));
                    CountLstElement = DisplayLstElement.Count(x => x.Status == Define.StatusSuccess);
                    SelElement = SelectedSuccess != null ? DisplayLstElement.FirstOrDefault(x => x.Id == SelectedSuccess) : DisplayLstElement.FirstOrDefault();
                }
                else
                {
                    DisplayLstElement = new ObservableCollection<VMElement>(OriginalList.Where(item => item.Status == Define.StatusFailed));
                    CountLstElement = DisplayLstElement.Count(x => x.Status == Define.StatusFailed);
                    SelElement = SelectedFailed != null ? DisplayLstElement.FirstOrDefault(x => x.Id == SelectedFailed) : DisplayLstElement.FirstOrDefault();
                }
            }
            else
            {
                DisplayLstElement = new ObservableCollection<VMElement>();
            }
        }

        private ObservableCollection<VMElement> LoadObjectConvertData(BuiltInCategory builtInCategory, List<IFCConvertHandleData> iFCConvertHandleDatas)
        {
            ObservableCollection<VMElement> items = new ObservableCollection<VMElement>();

            foreach (var itemIFCConvertHandleDatas in iFCConvertHandleDatas)
            {
                List<ElementConvert> itemsConverted = new List<ElementConvert>();
                List<ElementConvert> itemsNotConverted = new List<ElementConvert>();

                switch (builtInCategory)
                {
                    case BuiltInCategory.OST_PipeCurves:
                        if (itemIFCConvertHandleDatas.PipeDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.PipeDatasConverted);
                        if (itemIFCConvertHandleDatas.PipeDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.PipeDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_DuctCurves:
                        if (itemIFCConvertHandleDatas.DuctDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.DuctDatasConverted);
                        if (itemIFCConvertHandleDatas.DuctDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.DuctDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_StructuralColumns:
                        if (itemIFCConvertHandleDatas.ColumnDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.ColumnDatasConverted);
                        if (itemIFCConvertHandleDatas.ColumnDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.ColumnDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_Columns:
                        if (itemIFCConvertHandleDatas.ArchiColumnDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.ArchiColumnDatasConverted);
                        if (itemIFCConvertHandleDatas.ArchiColumnDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.ArchiColumnDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_Floors:
                        if (itemIFCConvertHandleDatas.FloorDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.FloorDatasConverted);
                        if (itemIFCConvertHandleDatas.FloorDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.FloorDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_Walls:
                        if (itemIFCConvertHandleDatas.WallDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.WallDatasConverted);
                        if (itemIFCConvertHandleDatas.WallDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.WallDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_StructuralFraming:
                        if (itemIFCConvertHandleDatas.BeamDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.BeamDatasConverted);
                        if (itemIFCConvertHandleDatas.BeamDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.BeamDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_Ceilings:
                        if (itemIFCConvertHandleDatas.CeilingDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.CeilingDatasConverted);
                        if (itemIFCConvertHandleDatas.CeilingDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.CeilingDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_GenericModel:
                        foreach (var dataConvert in itemIFCConvertHandleDatas.PipingSupportDatasConverted)
                        {
                            foreach (var item in dataConvert.ConvertInstances)
                            {
                                var data = item.IsValidObject == true ?
                                    new VMElement(item.Id.ToString(), item.Category.Name, Define.StatusSuccess, true, dataConvert.LinkInstance) :
                                    new VMElement(dataConvert.LinkEleData.LinkElement.Id.ToString(), "IFC", Define.StatusFailed, false, dataConvert.LinkInstance);
                                items.Add(data);
                            }
                        }
                        foreach (var item in itemIFCConvertHandleDatas.PipingSupportDatasNotConverted)
                        {
                            var data = new VMElement(item.LinkEleData.LinkElement.Id.ToString(), "IFC", Define.StatusFailed, false, item.LinkInstance);
                            items.Add(data);
                        }

                        break;

                    case BuiltInCategory.OST_ElectricalEquipment:
                        foreach (var item in itemIFCConvertHandleDatas.ConduitTmnBoxDatasConverted)
                        {
                            var data = item.ConvertElem?.IsValidObject == true ?
                                new VMElement(item.ConvertElem.Id.ToString(), item.ConvertElem.Category.Name, Define.StatusSuccess, true, item.LinkInstance) :
                                new VMElement(item.LinkEleData.LinkElement.Id.ToString(), "IFC", Define.StatusFailed, false, item.LinkInstance);

                            items.Add(data);
                        }
                        foreach (var item in itemIFCConvertHandleDatas.ConduitTmnBoxDatasNotConverted)
                        {
                            var data = new VMElement(item.LinkEleData.LinkElement.Id.ToString(), "IFC", Define.StatusFailed, false, item.LinkInstance);
                            items.Add(data);
                        }

                        break;

                    case BuiltInCategory.OST_Railings:
                        foreach (var item in itemIFCConvertHandleDatas.RaillingConverted)
                        {
                            if (_doc.GetElement(item.Key) is RevitLinkInstance linkInstance)
                            {
                                foreach (var elem in item.Value)
                                {
                                    if (elem?.IsValidObject != true)
                                    {
                                        continue;
                                    }
                                    var data = new VMElement(elem.Id.ToString(), elem.Category.Name, Define.StatusSuccess, true, linkInstance);
                                    items.Add(data);
                                }
                            }
                        }
                        foreach (var item in itemIFCConvertHandleDatas.RaillingNotConverted)
                        {
                            var data = new VMElement(item.LinkEleData.LinkElement.Id.ToString(), "IFC", Define.StatusFailed, false, item.LinkInstance);
                            items.Add(data);
                        }

                        break;

                    case BuiltInCategory.OST_CableTray:
                        if (itemIFCConvertHandleDatas.CableTrayDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.CableTrayDatasConverted);
                        if (itemIFCConvertHandleDatas.CableTrayDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.CableTrayDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_PipeAccessory:
                        if (itemIFCConvertHandleDatas.AccessoryDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.AccessoryDatasConverted);
                        if (itemIFCConvertHandleDatas.AccessoryDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.AccessoryDatasNotConverted);

                        break;

                    case BuiltInCategory.OST_ShaftOpening:
                        if (itemIFCConvertHandleDatas.OpeningDatasConverted?.Count > 0)
                            itemsConverted.AddRange(itemIFCConvertHandleDatas.OpeningDatasConverted);
                        if (itemIFCConvertHandleDatas.OpeningDatasNotConverted?.Count > 0)
                            itemsNotConverted.AddRange(itemIFCConvertHandleDatas.OpeningDatasNotConverted);

                        break;

                    default:
                        break;
                }

                foreach (var item in itemsConverted)
                {
                    var data = item.ConvertElem?.IsValidObject == true ?
                        new VMElement(item.ConvertElem.Id.ToString(), item.ConvertElem.Category.Name, Define.StatusSuccess, true, item.LinkInstance) :
                        new VMElement(item.LinkEleData.LinkElement.Id.ToString(), "IFC", Define.StatusFailed, false, item.LinkInstance);

                    items.Add(data);
                }
                foreach (var item in itemsNotConverted)
                {
                    var data = new VMElement(item.LinkEleData.LinkElement.Id.ToString(), "IFC", Define.StatusFailed, false, item.LinkInstance);
                    items.Add(data);
                }
            }

            return items;
        }

        public static List<CableTrayData> RemoveDuplicateCableTrays(List<CableTrayData> cableTrays)
        {
            // Create a HashSet to store unique cable trays
            HashSet<CableTrayData> uniqueCableTrays = new HashSet<CableTrayData>();

            foreach (var cableTray in cableTrays)
            {
                // Add cable tray to the HashSet
                uniqueCableTrays.Add(cableTray);
            }

            // Convert the HashSet back to a List
            return uniqueCableTrays.ToList();
        }

        private void ImportObjectData(XLWorkbook workbook, BuiltInCategory builtInCategory)
        {
            foreach (var worksheet in workbook.Worksheets)
            {
                if (Define.GetCategoryLabel(builtInCategory) == worksheet.Name)
                {
                    var usedRange = worksheet.RangeUsed();
                    int rowCount = usedRange.RowCount();
                    int colCount = usedRange.ColumnCount();

                    if (rowCount > 1)
                    {
                        VisibilityNoData = Visibility.Hidden;

                        for (int row = 2; row <= rowCount; row++)
                        {
                            // Reading cell values using ClosedXML
                            var cellId = usedRange.Cell(row, 1).GetValue<string>();
                            var cellType = usedRange.Cell(row, 2).GetValue<string>();
                            var cellStatus = usedRange.Cell(row, 3).GetValue<string>();
                            var cellRevLinkIns = usedRange.Cell(row, 4).GetValue<string>();

                            // Fetch RevitLinkInstance matching the name in the cell
                            RevitLinkInstance revLinkIns = GetAllRevitLinkInstances(_doc)
                                .FirstOrDefault(x => x.Name == cellRevLinkIns);

                            // Create a new VMElement and add it to the list
                            var element = new VMElement(
                                cellId,
                                cellType,
                                cellStatus,
                                cellStatus == "Success",   // Check if the status is "Success"
                                revLinkIns,
                                cellRevLinkIns
                            );

                            OriginalList.Add(element);
                        }
                    }
                }
            }
        }

        private static List<RevitLinkInstance> GetAllRevitLinkInstances(Document doc)
        {
            List<RevitLinkInstance> linkInstances = new List<RevitLinkInstance>();

            FilteredElementCollector collector = new FilteredElementCollector(doc);
            ICollection<ElementId> linkInstanceIds = collector.OfClass(typeof(RevitLinkInstance)).ToElementIds();

            foreach (ElementId linkInstanceId in linkInstanceIds)
            {
                if (doc.GetElement(linkInstanceId) is RevitLinkInstance linkInstance)
                {
                    linkInstances.Add(linkInstance);
                }
            }

            return linkInstances;
        }
    }
}