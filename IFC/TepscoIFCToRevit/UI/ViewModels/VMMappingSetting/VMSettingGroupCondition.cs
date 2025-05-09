using TepscoIFCToRevit.Data.SaveSettingData;

namespace TepscoIFCToRevit.UI.ViewModels.VMMappingSetting
{
    public class VMSettingGroupCondition : VMSettingGroup
    {
        public BindableBase _contentGroup;

        /// <summary>
        /// Content will change when data context change
        /// </summary>
        public BindableBase ContentGroup
        {
            get => _contentGroup;
            set => SetProperty(ref _contentGroup, value);
        }

        public VMSettingGroupCondition(VMSettingCategory categoryParent) : base(categoryParent)
        {
        }

        public VMSettingGroupCondition(VMSettingCategory categoryParent, SaveSettingGrp saveGrp) : base(categoryParent)
        {
        }
    }
}