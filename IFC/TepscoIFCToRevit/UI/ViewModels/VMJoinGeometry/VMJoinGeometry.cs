using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using RevitUtilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Input;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.UI.ViewModels.Interface;

namespace TepscoIFCToRevit.UI.ViewModels.VMJoinGeometry
{
    internal class VMJoinGeometry : BindableBase
    {
        #region Property

        private Document _doc;

        private Dictionary<string, IEnumerable<Element>> _comboBoxItems;

        public Dictionary<string, IEnumerable<Element>> ComboBoxItems
        {
            get { return _comboBoxItems; }
            set
            {
                _comboBoxItems = value;
                OnPropertyChanged(nameof(ComboBoxItems));
            }
        }

        private KeyValuePair<string, IEnumerable<Element>> _selectedType1;

        public KeyValuePair<string, IEnumerable<Element>> SelectedType1
        {
            get { return _selectedType1; }
            set
            {
                _selectedType1 = value;
                OnPropertyChanged(nameof(SelectedType1));
            }
        }

        private KeyValuePair<string, IEnumerable<Element>> _selectedType2;

        public KeyValuePair<string, IEnumerable<Element>> SelectedType2
        {
            get { return _selectedType2; }
            set
            {
                _selectedType2 = value;
                OnPropertyChanged(nameof(SelectedType2));
            }
        }

        private KeyValuePair<string, IEnumerable<Element>> _selectedType3;

        public KeyValuePair<string, IEnumerable<Element>> SelectedType3
        {
            get { return _selectedType3; }
            set
            {
                _selectedType3 = value;
                OnPropertyChanged(nameof(SelectedType3));
            }
        }

        private KeyValuePair<string, IEnumerable<Element>> _selectedType4;

        public KeyValuePair<string, IEnumerable<Element>> SelectedType4
        {
            get { return _selectedType4; }
            set
            {
                _selectedType4 = value;
                OnPropertyChanged(nameof(SelectedType4));
            }
        }

        // string lable
        public string Tille { get; set; }

        public string Category1 { get; set; }
        public string Category2 { get; set; }
        public string Category3 { get; set; }
        public string Category4 { get; set; }

        public string ApplyContent { get; set; }
        public string CancelContent { get; set; }

        #endregion Property

        public ICommand JoinCommand { get; set; }

        public VMJoinGeometry(Document doc)
        {
            _doc = doc;

            InitCombobox();
            InitLables();

            JoinCommand = new RelayCommand<object>(JoinGeometryCommand);
        }

        #region Init

        public void InitLables()
        {
            Tille = Define.TILLE_UI_JOINT_GEOMERTRY;
            Category1 = Define.UI_CATERGORY1;
            Category2 = Define.UI_CATERGORY2;
            Category3 = Define.UI_CATERGORY3;
            Category4 = Define.UI_CATERGORY4;
            ApplyContent = Define.Apply_CONTENT;
            CancelContent = Define.COLSE_CONTENT;
        }

        public void InitCombobox()

        {
            ComboBoxItems = new Dictionary<string, IEnumerable<Element>>();

            ComboBoxItems.Add(Define.GetCategoryLabel(BuiltInCategory.OST_Walls), RevitUtils.GetAllWall(_doc));
            ComboBoxItems.Add(Define.GetCategoryLabel(BuiltInCategory.OST_Columns), RevitUtils.GetAllColumns(_doc));
            ComboBoxItems.Add(Define.GetCategoryLabel(BuiltInCategory.OST_StructuralFraming), RevitUtils.GetAllBeams(_doc));
            ComboBoxItems.Add(Define.GetCategoryLabel(BuiltInCategory.OST_Floors), RevitUtils.GetAllFloors(_doc));
            try
            {
                if (!string.IsNullOrWhiteSpace(Properties.Settings.Default.JoinSaveData))
                {
                    string value = Properties.Settings.Default.JoinSaveData;
                    string[] values = value.Split(',');

                    SelectedType1 = ComboBoxItems.FirstOrDefault(X => X.Key == values[0]);
                    SelectedType2 = ComboBoxItems.FirstOrDefault(X => X.Key == values[1]);
                    SelectedType3 = ComboBoxItems.FirstOrDefault(X => X.Key == values[2]);
                    SelectedType4 = ComboBoxItems.FirstOrDefault(X => X.Key == values[3]);
                }
                else // Default setting
                {
                    SelectedType1 = ComboBoxItems.ElementAtOrDefault(0);
                    SelectedType2 = ComboBoxItems.ElementAtOrDefault(1);
                    SelectedType3 = ComboBoxItems.ElementAtOrDefault(2);
                    SelectedType4 = ComboBoxItems.ElementAtOrDefault(3);
                }
            }
            catch (Exception e)
            {
                MessageBox.Show(e.Message + e.StackTrace);
            }
        }

        #endregion Init

        private void JoinGeometryCommand(object obj)
        {
            if (SelectedType1.Value != null && SelectedType2.Value != null && SelectedType3.Value != null && SelectedType4.Value != null)
            {
                IEnumerable<Element> SelectElement1 = SelectedType1.Value;
                List<Element> element1 = SelectElement1.ToList();

                IEnumerable<Element> SelectElement2 = SelectedType2.Value;
                List<Element> element2 = SelectElement2.ToList();

                IEnumerable<Element> SelectElement3 = SelectedType3.Value;
                List<Element> element3 = SelectElement3.ToList();

                IEnumerable<Element> SelectElement4 = SelectedType4.Value;
                List<Element> element4 = SelectElement4.ToList();

                if (IsDuplicateSelection())
                    IO.ShowWanring(Define.WARNING_SELECT_CATEGORY_DUPLICATE);
                else
                {
                    //Lưu trữ các element theo loại BuiltInCategory
                    Dictionary<BuiltInCategory, List<Element>> data = new Dictionary<BuiltInCategory, List<Element>>();
                    if (element1.Count > 0)
                        data.Add((BuiltInCategory)element1.FirstOrDefault().Category.Id.IntegerValue, element1);

                    if (element2.Count > 0)
                        data.Add((BuiltInCategory)element2.FirstOrDefault().Category.Id.IntegerValue, element2);

                    if (element3.Count > 0)
                        data.Add((BuiltInCategory)element3.FirstOrDefault().Category.Id.IntegerValue, element3);

                    if (element4.Count > 0)
                        data.Add((BuiltInCategory)element4.FirstOrDefault().Category.Id.IntegerValue, element4);

                    Properties.Settings.Default.JoinSaveData = SelectedType1.Key + "," + SelectedType2.Key + "," + SelectedType3.Key + "," + SelectedType4.Key;

                    Properties.Settings.Default.Save();

                    using (Transaction trans = new Transaction(_doc, "JoinElement"))
                    {
                        trans.Start();

                        HandleWarning handleWarning = new HandleWarning();
                        FailureHandlingOptions options = trans.GetFailureHandlingOptions();
                        options.SetFailuresPreprocessor(handleWarning);

                        JoinCutElements(data);

                        trans.Commit(options);
                    }

                    if (obj is Window window)
                    {
                        window.Close();
                    }

                    //show result
                    TaskDialog taskDialog = new TaskDialog("結果を表示");
                    taskDialog.MainInstruction = "へジオメトリ結合:" + "\n" + SelectedType1.Key + " -> " + SelectedType2.Key + " -> "
                                                                             + SelectedType3.Key + " -> " + SelectedType4.Key;
                    taskDialog.MainIcon = TaskDialogIcon.TaskDialogIconInformation;
                    taskDialog.CommonButtons = TaskDialogCommonButtons.Ok;

                    TaskDialogResult result = taskDialog.Show();
                }
            }
            else
                IO.ShowWanring(Define.PLEASE_SELECT_OPTIONS);
        }

        private bool IsDuplicateSelection()
        {
            HashSet<IEnumerable<Element>> selectedValues = new HashSet<IEnumerable<Element>>();

            selectedValues.Add(SelectedType1.Value);
            selectedValues.Add(SelectedType2.Value);
            selectedValues.Add(SelectedType3.Value);
            selectedValues.Add(SelectedType4.Value);

            return selectedValues.Count < 4;
        }

        private void JoinCutElements(Dictionary<BuiltInCategory, List<Element>> data)
        {
            foreach (KeyValuePair<BuiltInCategory, List<Element>> pair in data)
            {
                // elem của category ưu tiên
                foreach (var elem in pair.Value)
                {
                    // elem của các category bị cắt
                    List<KeyValuePair<BuiltInCategory, List<Element>>> cutElems = GetCutElementData(data, pair.Key);
                    foreach (var cutElem in cutElems)
                    {
                        JoinElements(elem, cutElem.Value.ConvertAll(x => x.Id));
                    }
                }
            }
        }

        private List<KeyValuePair<BuiltInCategory, List<Element>>> GetCutElementData(Dictionary<BuiltInCategory, List<Element>> data, BuiltInCategory currentKey)
        {
            var retVal = new List<KeyValuePair<BuiltInCategory, List<Element>>>();
            bool isFound = false;
            foreach (var pair in data)
            {
                if (pair.Key == currentKey)
                {
                    isFound = true;
                    continue;
                }
                if (isFound)
                {
                    retVal.Add(pair);
                }
            }
            return retVal;
        }

        private ElementFilter GetBoxFilter(Element elem)
        {
            BoundingBoxXYZ box = elem.get_BoundingBox(null);
            if (box != null)
            {
                XYZ min = box.Transform.OfPoint(box.Min);
                XYZ max = box.Transform.OfPoint(box.Max);

                Outline outline = new Outline(min, max);

                BoundingBoxIntersectsFilter filter = new BoundingBoxIntersectsFilter(outline);
                return filter;
            }
            return null;
        }

        private void JoinElements(Element elem, List<ElementId> idsToSwitchJoin)
        {
            FilteredElementCollector col = new FilteredElementCollector(elem.Document, idsToSwitchJoin).WhereElementIsNotElementType();
            ElementFilter boxFilter = GetBoxFilter(elem);

            FilteredElementCollector elems = col;
            if (boxFilter != null)
                elems = col.WherePasses(boxFilter);

            foreach (Element e in elems)
            {
                JoinGeometry(elem, e);
            }
        }

        private void JoinGeometry(Element elem1, Element elem2)
        {
            try
            {
                if (!AreElementsJoined(elem1, elem2))
                {
                    JoinGeometryUtils.JoinGeometry(_doc, elem1, elem2);
                }
                else
                {
                    if (JoinGeometryUtils.IsCuttingElementInJoin(_doc, elem2, elem1) == true)
                    {
                        JoinGeometryUtils.SwitchJoinOrder(_doc, elem1, elem2);
                    }
                }
            }
            catch (Exception)
            {
            }
        }

        private bool AreElementsJoined(Element elem1, Element elem2)
        {
            return JoinGeometryUtils.AreElementsJoined(_doc, elem1, elem2);
        }
    }
}