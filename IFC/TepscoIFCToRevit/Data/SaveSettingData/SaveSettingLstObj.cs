using Newtonsoft.Json;

namespace TepscoIFCToRevit.Data.SaveSettingData
{
    public class SaveSettingLstObj
    {
        [JsonProperty("NameFile", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string NameFile { get; set; }

        [JsonProperty("ProcessBuiltInCategory", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string ProcessBuiltInCategory { get; set; }

        [JsonProperty("IndexCateSelected", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool IndexCatSelected { get; set; }

        [JsonProperty("IndexRowSelected", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string IndexRowSelected { get; set; }
    }
}