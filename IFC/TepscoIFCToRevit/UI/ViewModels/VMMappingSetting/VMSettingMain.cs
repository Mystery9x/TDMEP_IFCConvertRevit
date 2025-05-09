using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace TepscoIFCToRevit.UI.ViewModels
{
    public class VMSettingMain : BindableBase
    {
        #region Variable & Properties

        private ObservableCollection<VMSettingCategory> _settingCategories;

        public ObservableCollection<VMSettingCategory> SettingCategories
        {
            get => _settingCategories;
            set => SetProperty(ref _settingCategories, value);
        }

        private VMSettingCategory _selSettingCategory;

        public VMSettingCategory SelectedSettingCategory
        {
            get => _selSettingCategory;
            set => SetProperty(ref _selSettingCategory, value);
        }

        public bool IsLoadFamily = false;

        #endregion Variable & Properties

        public VMSettingMain(List<VMSettingCategory> categories)
        {
            SettingCategories = new ObservableCollection<VMSettingCategory>(categories);
            SelectedSettingCategory = SettingCategories.FirstOrDefault();
        }
    }
}