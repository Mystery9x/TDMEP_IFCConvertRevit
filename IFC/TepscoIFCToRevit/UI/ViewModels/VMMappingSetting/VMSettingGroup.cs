using Autodesk.Revit.DB;
using Autodesk.Revit.UI;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using TepscoIFCToRevit.Common;
using TepscoIFCToRevit.Data;
using TepscoIFCToRevit.Data.SaveSettingData;

namespace TepscoIFCToRevit.UI.ViewModels.VMMappingSetting
{
    public class VMSettingGroup : BindableBase
    {
        #region Variable & Properties

        public VMSettingCategory CategoryParent { get; set; }

        private ObservableCollection<VMSettingRevit> _typeItems;

        public ObservableCollection<VMSettingRevit> SettingTypeItems
        {
            get => _typeItems;
            set => SetProperty(ref _typeItems, value);
        }

        private VMSetingRevitElement _selType = null;

        public VMSetingRevitElement SelType
        {
            get => _selType;
            set => SetProperty(ref _selType, value);
        }

        private int _beginSelTypeId = int.MinValue;

        public int BeginSelTypeId
        {
            get => _beginSelTypeId;
            set => SetProperty(ref _beginSelTypeId, value);
        }

        private bool _isCreatManual = false;

        public bool IsCreateManual
        {
            get => _isCreatManual;
            set
            {
                SettingTypeItems.ForEach(x => x.IsEnableSymbol = value);
                SetProperty(ref _isCreatManual, value);
            }
        }

        private bool _isGroupSelection = false;

        public bool IsGroupSelection
        {
            get => _isGroupSelection;
            set => SetProperty(ref _isGroupSelection, value);
        }

        private ObservableCollection<VMSettingIfc> _settingObjs;

        public ObservableCollection<VMSettingIfc> SettingObjs
        {
            get => _settingObjs;
            set => SetProperty(ref _settingObjs, value);
        }

        private VMSettingIfc _selObj;

        public VMSettingIfc SelObj
        {
            get => _selObj;
            set => SetProperty(ref _selObj, value);
        }

        private List<string> _lstSelectElemId;

        public List<string> LstSelectElemId
        {
            get => _lstSelectElemId;
            set => SetProperty(ref _lstSelectElemId, value);
        }

        #endregion Variable & Properties

        #region Constructor

        /// <summary>
        /// get save setting from setting before
        /// </summary>
        /// <param name="categoryParent"></param>
        /// <param name="settingGroupSave"></param>
        public VMSettingGroup(VMSettingCategory categoryParent, SaveSettingGrp settingGroupSave)
        {
            CategoryParent = categoryParent;
            IsCreateManual = categoryParent.IsCreateManual;
            IsGroupSelection = settingGroupSave.IsGroupSelection;
            LstSelectElemId = new List<string>();
            SettingObjs = new ObservableCollection<VMSettingIfc>();

            if (settingGroupSave.IsGroupSelection == true && settingGroupSave.SettingObjs.FirstOrDefault().LstElementIdSel != null)
            {
                LstSelectElemId.AddRange(settingGroupSave.SettingObjs.SelectMany(x => x.LstElementIdSel));
            }

            int? idType = null;
            if (settingGroupSave.Type?.Count > 0)
                idType = settingGroupSave.Type.FirstOrDefault()?.SelType;

            if (!IsCreateManual)
            {
                var supportType = RevitUtils.GetIShapePipingSupport(categoryParent.UIDocument.Document);
                if (supportType != null)
                    SelType = new VMSetingRevitElement(supportType);
            }

            SettingTypeItems = new ObservableCollection<VMSettingRevit>
            {
                new VMSettingRevit(this,idType )
            };
        }

        /// <summary>
        /// deafault setting for group
        /// </summary>
        /// <param name="categoryParent"></param>
        public VMSettingGroup(VMSettingCategory categoryParent)
        {
            CategoryParent = categoryParent;

            IsCreateManual = categoryParent.IsCreateManual;

            IsGroupSelection = categoryParent.IsGroupSelection;

            if (!IsCreateManual)
            {
                var supportType = RevitUtils.GetIShapePipingSupport(categoryParent.UIDocument.Document);
                if (supportType != null)
                    SelType = new VMSetingRevitElement(supportType);
            }

            SettingObjs = new ObservableCollection<VMSettingIfc>();

            SettingTypeItems = new ObservableCollection<VMSettingRevit>
            {
                new VMSettingRevit(this,null)
            };
        }

        #endregion Constructor

        public void UpdateLoadFamilies(Dictionary<VMSetingRevitElement, ObservableCollection<VMSetingRevitElement>> famData, int? idType)
        {
            foreach (var groups in SettingTypeItems)
            {
                groups.UpdateLoadFamilies(famData, idType);
            }
        }
    }
}