using Autodesk.Revit.DB;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TepscoIFCToRevit.UI.ViewModels.VMMappingSetting
{
    public class VMSettingRevit : BindableBase
    {
        private Dictionary<VMSetingRevitElement, ObservableCollection<VMSetingRevitElement>> _familyData;
        public VMSettingGroup GroupParent { get; set; }

        private bool _isEnableSymbol = true;

        public bool IsEnableSymbol
        {
            get { return _isEnableSymbol; }
            set
            {
                SetProperty(ref _isEnableSymbol, value);
            }
        }

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
                GroupParent.SelType = _selSymbol;
            }
        }

        private VMSetingRevitElement _selFamily;

        public VMSetingRevitElement SelectedFamily
        {
            get => _selFamily;
            set
            {
                SetProperty(ref _selFamily, value);
                if (value != null && _familyData.ContainsKey(value))
                    SettingSymbolObjs = _familyData[_selFamily];
            }
        }

        private ObservableCollection<VMSetingRevitElement> _families;

        public ObservableCollection<VMSetingRevitElement> Families
        {
            get => _families;
            set => SetProperty(ref _families, value);
        }

        public VMSettingRevit(VMSettingGroup groupParent, int? idSelType)
        {
            GroupParent = groupParent;
            SettingSymbolObjs = new ObservableCollection<VMSetingRevitElement>();
            if (GroupParent != null
               && GroupParent.CategoryParent != null)
            {
                IsEnableSymbol = groupParent.IsCreateManual;
                if (GroupParent.CategoryParent.ProcessBuiltInCategory != BuiltInCategory.OST_GenericModel)
                {
                    SettingSymbolObjs = GroupParent.CategoryParent.Types;
                }
                else
                {
                    _familyData = new FilteredElementCollector(App._UIDoc.Document)
                                    .OfClass(typeof(Family))
                                    .Cast<Family>()
                                    .Where(x => x.FamilyCategoryId.IntegerValue == (int)BuiltInCategory.OST_GenericModel
                                            && groupParent.CategoryParent.NameFamilyPipeSupports.ContainsKey(x.Name))
                                    .ToDictionary(x => new VMSetingRevitElement(x), x => GetTypesFromFamily(x));

                    Families = new ObservableCollection<VMSetingRevitElement>(_familyData.Keys);

                    SelectedFamily = Families.FirstOrDefault(); // setting default for family
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
                                    SelectedFamily = pair.Key;
                                    isBreak = true;
                                    break;
                                }
                            }
                        }

                        if (!isBreak)
                        {
                            SelectedFamily = null; // setting default for family
                            SelectedSymbol = null; // setting default for symbol
                        }
                    }
                    else
                    {
                        SelectedFamily = null; // setting default for family
                        SelectedSymbol = null; // setting default for symbol
                    }
                }
            }
        }

        private ObservableCollection<VMSetingRevitElement> GetTypesFromFamily(Family family)
        {
            var symbols = family.GetFamilySymbolIds()
                                .Select(id => App._UIDoc.Document.GetElement(id))
                                .Select(x => new VMSetingRevitElement(x));
            ObservableCollection<VMSetingRevitElement> col = new ObservableCollection<VMSetingRevitElement>(symbols);
            return col;
        }

        public void UpdateLoadFamilies(Dictionary<VMSetingRevitElement, ObservableCollection<VMSetingRevitElement>> famData, int? idSelType)
        {
            SettingSymbolObjs.Clear();
            _familyData = famData;
            Families = new ObservableCollection<VMSetingRevitElement>(_familyData.Keys);

            SelectedFamily = Families.FirstOrDefault();// setting default for family

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
                            SelectedFamily = pair.Key;
                            isBreak = true;
                            break;
                        }
                    }
                }

                if (!isBreak)
                {
                    SelectedFamily = null; // setting default for family
                    SelectedSymbol = null; // setting default for symbol
                }
            }
            else
            {
                SelectedFamily = null; // setting default for family
                SelectedSymbol = null; // setting default for symbol
            }
        }
    }
}