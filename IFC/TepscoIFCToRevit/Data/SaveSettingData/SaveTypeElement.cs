using Newtonsoft.Json;

namespace TepscoIFCToRevit.Data.SaveSettingData
{
    public partial class SaveTypeElement
    {
        [JsonProperty("SelFamily", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual int SelFamily { get; set; }

        [JsonProperty("NameFamily", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string NameFamily { get; set; }

        [JsonProperty("SelType", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual int SelType { get; set; }

        [JsonProperty("NameType", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string NameType { get; set; }
    }
}