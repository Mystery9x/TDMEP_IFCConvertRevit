using TepscoIFCToRevit.Data.SaveSettingData;

namespace TepscoIFCToRevit.UI.ViewModels.VMMappingSetting
{
    public class VMSettingGroupSelection : VMSettingGroup
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

        public VMSettingGroupSelection(VMSettingCategory categoryParent) : base(categoryParent)
        {
        }

        public VMSettingGroupSelection(VMSettingCategory categoryParent, SaveSettingGrp saveGrp) : base(categoryParent)
        {
        }
    }
}