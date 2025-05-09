using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using ClosedXML.Excel;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Windows.Input;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.Data.SaveSettingData;
using MessageBox = System.Windows.Forms.MessageBox;
using Visibility = System.Windows.Visibility;

namespace TepscoIFCToRevit.UI.ViewModels.VMShowDialog
{
    public class VMConvertManager : BindableBase
    {
        public Document _doc;
        public UIDocument _uiDoc;
        public UIApplication _uiApp;
        public UIView _uiView;
        public string NameFilePath { get; set; }

        private ObservableCollection<VMRule> _rules;

        public ObservableCollection<VMRule> Rules
        {
            get { return _rules; }
            set { SetProperty(ref _rules, value); }
        }

        private VMRule _ruleSel;

        public VMRule RuleSel
        {
            get { return _ruleSel; }
            set
            {
                SetProperty(ref _ruleSel, value);
            }
        }

        protected List<RevitLinkInstance> _linkInstance = null;

        public List<RevitLinkInstance> LinkInstance
        {
            get => _linkInstance;
            set => SetProperty(ref _linkInstance, value);
        }

        public Transform RevLinkTransform = null;

        private string _exportAOrReadContent;

        public string ExportAOrReadContent
        {
            get { return _exportAOrReadContent; }
            set { SetProperty(ref _exportAOrReadContent, value); }
        }

        private bool IsExportObjectList { get; set; }

        private Visibility _visibleBtnImport = Visibility.Visible;

        public Visibility VisibleBtnImport
        {
            get { return _visibleBtnImport; }
            set { SetProperty(ref _visibleBtnImport, value); }
        }

        public ICommand ShowElementInRevitCommand { get; set; }
        public ICommand ExportCommand { get; set; }
        public ICommand ImportCommand { get; set; }

        public VMConvertManager(List<IFCConvertHandleData> iFCConvertHandleDatas = null, bool isExportObjectList = false, string filePath = null)
        {
            _uiApp = App._UIApp;
            _uiDoc = App._UIDoc;
            _doc = _uiDoc.Document;
            _uiView = _uiDoc.GetOpenUIViews().FirstOrDefault();
            Rules = new ObservableCollection<VMRule>();
            IsExportObjectList = isExportObjectList;

            if (IsExportObjectList)
            {
                VisibleBtnImport = Visibility.Hidden;
                ExportAOrReadContent = Define.ExportXMLContent;
                LoadData(iFCConvertHandleDatas);
            }
            else
            {
                VisibleBtnImport = Visibility.Visible;
                ExportAOrReadContent = Define.ImportXMLContent;
                LoadDataFromExcel(filePath);
            }

            Initialize();
        }

        private void Initialize()
        {
            ShowElementInRevitCommand = new RelayCommand<object>(ShowElementInRevit);
            ExportCommand = new RelayCommand<object>(ExportDataToExcel);
            ImportCommand = new RelayCommand<object>(ImportDataToExcel);
        }

        private void LoadData(List<IFCConvertHandleData> iFCConvertHandleDatas)
        {
            Rules.Clear();
            LinkInstance = RevitUtils.GetLinkInstances(App._UIDoc.Document).ToList();

            VMSettingMain vMSettingMain = SaveSetting.GetMainSettings(LinkInstance);
            if (vMSettingMain != null)
            {
                foreach (var item in vMSettingMain.SettingCategories)
                {
                    int count = CountElementConvert(item.ProcessBuiltInCategory, iFCConvertHandleDatas);
                    if (count > 0)
                    {
                        Rules.Add(new VMRule(_uiDoc, Define.GetCategoryLabel(item.ProcessBuiltInCategory), iFCConvertHandleDatas, item.ProcessBuiltInCategory));
                    }
                }

                RuleSel = Rules.FirstOrDefault();
            }
        }

        private int CountElementConvert(BuiltInCategory builtInCategory, List<IFCConvertHandleData> iFCConvertHandleDatas)
        {
            int count = 0;

            switch (builtInCategory)
            {
                case BuiltInCategory.OST_PipeCurves:
                    count = iFCConvertHandleDatas.Sum(item => item.PipeDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_DuctCurves:
                    count = iFCConvertHandleDatas.Sum(item => item.DuctDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_StructuralColumns:
                    count = iFCConvertHandleDatas.Sum(item => item.ColumnDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_Columns:
                    count = iFCConvertHandleDatas.Sum(item => item.ArchiColumnDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_Floors:
                    count = iFCConvertHandleDatas.Sum(item => item.FloorDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_Walls:
                    count = iFCConvertHandleDatas.Sum(item => item.WallDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_StructuralFraming:
                    count = iFCConvertHandleDatas.Sum(item => item.BeamDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_Ceilings:
                    count = iFCConvertHandleDatas.Sum(item => item.CeilingDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_GenericModel:
                    count = iFCConvertHandleDatas.Sum(item => item.PipingSpDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_ElectricalEquipment:
                    count = iFCConvertHandleDatas.Sum(item => item.ConduitTmnBoxDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_Railings:
                    count = iFCConvertHandleDatas.Sum(item => item.GroupRailingsDatasBeforeConvert.Sum(x => x.Count));
                    break;

                case BuiltInCategory.OST_CableTray:
                    count = iFCConvertHandleDatas.Sum(item => item.CableTrayDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_PipeAccessory:
                    count = iFCConvertHandleDatas.Sum(item => item.AccessoryDatasBeforeConvert.Count);
                    break;

                case BuiltInCategory.OST_ShaftOpening:
                    count = iFCConvertHandleDatas.Sum(item => item.OpeningDatasBeforeConvert.Count);
                    break;
            }

            return count;
        }

        private void ShowElementInRevit(object obj)
        {
            if (Rules?.Count > 0)
            {
                if (RuleSel.SelElement == null)
                {
                    MessageBox.Show(Define.RuleSelectedIsNull, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var selElem = RuleSel.SelElement;
                if (selElem != null)
                {
                    if (selElem.LinkInstance != null)
                        RevLinkTransform = selElem.LinkInstance.GetTotalTransform();

                    ElementId elemid = new ElementId(Int32.Parse(selElem.Id));

                    HighlightElementInRevit(elemid, selElem.LinkInstance);
                }
            }
            else
                MessageBox.Show(Define.RuleIsNull, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }

        private void HighlightElementInRevit(ElementId elementId, RevitLinkInstance revitLink)
        {
            try
            {
                _uiDoc.RefreshActiveView();

                if (revitLink != null)
                {
                    Document docLinked = revitLink.GetLinkDocument();
                    Element linkedelement = docLinked.GetElement(elementId);

                    if (linkedelement != null)
                    {
                        LinkElementId linkElementId = new LinkElementId(linkedelement.Id);
                        LinkElementData linkElementData = new LinkElementData(docLinked.GetElement(linkElementId.HostElementId));
                        List<GeometryObject> geometries = GeometryUtils.GetIfcGeometriess(linkElementData.LinkElement);

                        BoundingBoxXYZ boundingBoxLinkElem = new BoundingBoxXYZ();

                        boundingBoxLinkElem = linkElementData.LinkElement.get_BoundingBox(null);

                        _uiView.ZoomAndCenterRectangle(RevLinkTransform.OfPoint(boundingBoxLinkElem.Max), RevLinkTransform.OfPoint(boundingBoxLinkElem.Min));
                    }
                    else
                    {
                        _uiApp.ActiveUIDocument.Selection.SetElementIds(new List<ElementId> { elementId });
                        _uiApp.ActiveUIDocument.ShowElements(elementId);
                        _uiDoc.RefreshActiveView();
                    }
                }
                else
                {
                    MessageBox.Show(Define.ElementNotExist, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Warning);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(Define.ElementNotExist, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        private void ImportDataToExcel(object obj)
        {
            try
            {
                OpenFileDialog openFileDialog = new OpenFileDialog
                {
                    Filter = "Excel files (*.xlsx;*.xls;*.xlsm;*.xlt)|*.xlsx;*.xls;*.xlsm;*.xlt",
                    Title = Define.ImportExcelFile
                };

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    LoadDataFromExcel(openFileDialog.FileName);
                }
            }
            catch (Exception)
            {
                MessageBox.Show(Define.FileNotSupport, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void ExportDataToExcel(object obj)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog
            {
                Filter = "Excel files (*.xlsx)|*.xlsx",
                Title = Define.SelectPathExport,
                FileName = "ExportFile",
            };

            var fileName = string.Empty;

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
                fileName = saveFileDialog.FileName;

            if (string.IsNullOrEmpty(fileName))
                return;

            NameFilePath = Path.GetFileName(fileName);

            if (!IsFileInUse(fileName))
            {
                try
                {
                    using (var workbook = new XLWorkbook())
                    {
                        foreach (var rule in Rules)
                        {
                            var worksheet = workbook.Worksheets.Add(rule.NameCategory);

                            // Define headers
                            worksheet.Cell(1, 1).Value = Define.ElementId;
                            worksheet.Cell(1, 2).Value = Define.ElementType;
                            worksheet.Cell(1, 3).Value = Define.ElementStatus;
                            worksheet.Cell(1, 4).Value = Define.ElementRevitLnkIns;

                            // Set bold headers
                            worksheet.Row(1).Style.Font.Bold = true;

                            // Set alignment
                            worksheet.Columns(1, 4).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Left;

                            // Set column widths
                            worksheet.Column(1).Width = 20;
                            worksheet.Column(2).Width = 20;
                            worksheet.Column(3).Width = 20;

                            // Set color for the selected rule
                            if (RuleSel != null && worksheet.Name == RuleSel.NameCategory)
                            {
                                worksheet.TabColor = XLColor.Red;
                            }

                            var listElementSort = rule.OriginalList?.OrderByDescending(x => x.Status).ToList();
                            if (listElementSort != null && listElementSort.Count > 0)
                            {
                                for (int i = 0; i < listElementSort.Count; i++)
                                {
                                    int.TryParse(listElementSort[i].Id.ToString(), out int intId);

                                    // Highlight row if selected element matches
                                    if (intId.ToString() == RuleSel?.SelElement?.Id)
                                    {
                                        worksheet.Row(i + 2).Style.Fill.BackgroundColor = XLColor.Yellow;
                                    }

                                    // Write data to cells
                                    worksheet.Cell(i + 2, 1).Value = intId;
                                    worksheet.Cell(i + 2, 2).Value = listElementSort[i].Type;
                                    worksheet.Cell(i + 2, 3).Value = listElementSort[i].Status;
                                    worksheet.Cell(i + 2, 4).Value = listElementSort[i].LinkInstance.Name;
                                }
                            }
                        }

                        // Save the workbook
                        workbook.SaveAs(fileName);

                        MessageBox.Show(Define.ExportSaveSuccess, Define.StatusAlert, MessageBoxButtons.OK, MessageBoxIcon.Information);
                    }
                }
                catch (Exception)
                {
                    MessageBox.Show(Define.ExportSaveFail, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Error);
                }
            }
            else
            {
                MessageBox.Show(Define.CloseXMLBeforeSave, Define.StatusAlert, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }

        /// <summary>
        /// Check file in using
        /// </summary>
        public static bool IsFileInUse(string path)
        {
            if (File.Exists(path))
            {
                FileStream stream = null;
                try
                {
                    FileInfo file = new FileInfo(path);
                    stream = file.Open(FileMode.Open, FileAccess.ReadWrite, FileShare.None);
                }
                catch (IOException)
                {
                    return true;
                }
                finally
                {
                    stream?.Close();
                }
            }

            return false;
        }

        private void LoadDataFromExcel(string filePath)
        {
            try
            {
                if (filePath == null)
                    return;

                NameFilePath = Path.GetFileName(filePath);

                if (!System.IO.File.Exists(filePath))
                {
                    MessageBox.Show(Define.FileNotFound, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                LinkInstance = RevitUtils.GetLinkInstances(App._UIDoc.Document).ToList();
                List<BuiltInCategory> lstLaBelNameCategory = new List<BuiltInCategory>()
                {
                    BuiltInCategory.OST_PipeCurves,
                    BuiltInCategory.OST_DuctCurves,
                    BuiltInCategory.OST_StructuralColumns,
                    BuiltInCategory.OST_Columns,
                    BuiltInCategory.OST_Floors,
                    BuiltInCategory.OST_Walls,
                    BuiltInCategory.OST_StructuralFraming,
                    BuiltInCategory.OST_Ceilings,
                    BuiltInCategory.OST_GenericModel,
                    BuiltInCategory.OST_ElectricalEquipment,
                    BuiltInCategory.OST_Railings,
                    BuiltInCategory.OST_CableTray,
                    BuiltInCategory.OST_PipeAccessory,
                    BuiltInCategory.OST_ShaftOpening
                };

                // Open workbook using ClosedXML
                using (var workbook = new XLWorkbook(filePath))
                {
                    Rules.Clear();
                    foreach (var item in lstLaBelNameCategory)
                    {
                        var rule = new VMRule(_uiDoc, Define.GetCategoryLabel(item), item, workbook);
                        if (rule.DisplayLstElement?.Count > 0)
                        {
                            Rules.Add(rule);
                        }
                    }

                    var redColor = XLColor.Red;
                    var yellowColor = XLColor.Yellow;

                    foreach (var sheet in workbook.Worksheets)
                    {
                        // Check tab color
                        if (sheet.TabColor != redColor)
                            continue;

                        var releSel = Rules.FirstOrDefault(x => x.NameCategory == sheet.Name) ?? Rules.FirstOrDefault();
                        if (releSel == null)
                            continue;

                        RuleSel = releSel;

                        var usedRange = sheet.RangeUsed();
                        foreach (var row in usedRange.Rows())
                        {
                            var firstCell = row.Cell(1);
                            if (!firstCell.IsEmpty())
                            {
                                var cellColor = firstCell.Style.Fill.BackgroundColor;

                                if (cellColor == yellowColor)
                                {
                                    var select = RuleSel.OriginalList.FirstOrDefault(x => x.Id == firstCell.GetValue<string>());
                                    if (select == null)
                                        continue;

                                    if (select.Status == Define.StatusSuccess)
                                    {
                                        RuleSel.SelectedSuccess = select.Id;
                                        RuleSel.ShowSuccess = true;
                                    }
                                    else
                                    {
                                        RuleSel.SelectedFailed = select.Id;
                                        RuleSel.ShowSuccess = false;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception)
            {
                MessageBox.Show(Define.FileNotSupport, Define.StatusErr, MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }
    }
}