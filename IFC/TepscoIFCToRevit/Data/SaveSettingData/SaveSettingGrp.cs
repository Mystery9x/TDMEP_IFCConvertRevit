using Newtonsoft.Json;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Data.SaveSettingData
{
    public class SaveSettingGrp
    {
        [JsonProperty("SettingObjs", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual List<SaveSettingObj> SettingObjs { get; set; }

        [JsonProperty("IsGroupSelection", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool IsGroupSelection { get; set; }

        [JsonProperty("Type", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual List<SaveTypeElement> Type { get; set; }

        [JsonProperty("IsCreateManual", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool IsCreateManual { get; set; }
    }
}