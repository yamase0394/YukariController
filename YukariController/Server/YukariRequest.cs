
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YukariController
{
    [JsonObject("yukariRequest")]
    public class YukariRequest
    {
        [JsonProperty("command")]
        [JsonConverter(typeof(StringEnumConverter))]
        public YukariManager.Command Command { get; set; }

        [JsonProperty("text")]
        public string Text { get; set; }
    }
}
