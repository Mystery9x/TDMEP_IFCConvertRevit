using Newtonsoft.Json;
using System.Collections.Generic;

namespace TepscoIFCToRevit.Data.SaveSettingData
{
    public partial class SaveSettingObj
    {
        [JsonProperty("SelParaKey", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string SelParaKey { get; set; }

        [JsonProperty("FlagContain", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool FlagContain { get; set; }

        [JsonProperty("FlagEqual", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual bool FlagEqual { get; set; }

        [JsonProperty("KeyValue", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual string KeyValue { get; set; }

        [JsonProperty("LstElementIdSel", Required = Required.DisallowNull, NullValueHandling = NullValueHandling.Ignore)]
        public virtual List<string> LstElementIdSel { get; set; }
    }
}