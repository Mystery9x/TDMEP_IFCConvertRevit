using Newtonsoft.Json;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Data.SaveSettingData
{
    public class SaveMappingSetting
    {
        [JsonProperty("ProcessBuiltInCategory", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual int ProcessBuiltInCategory { get; set; }

        [JsonProperty("SettingGrps", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual List<SaveSettingGrp> SettingGrps { get; set; }

        [JsonProperty("IsCheckedGetEleByCategory", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool IsCheckedGetEleByCategory { get; set; }

        [JsonProperty("SelTypeCaseGetByCategory", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual int SelTypeCaseGetByCategory { get; set; }

        [JsonProperty("IsCreateManual", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool IsCreateManual { get; set; }

        [JsonProperty("IsCheckedGetParamByCategory", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool IsCheckedGetParamByCategory { get; set; }

        [JsonProperty("SelParaKey", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string SelParaKey { get; set; }

        [JsonProperty("NameParameterInRevit", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string NameParameterInRevit { get; set; }

        [JsonProperty("ValueParameter", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string ValueParameter { get; set; }

        [JsonProperty("ToggelBtnGrp", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool ToggelBtnGrp { get; set; }
    }
}