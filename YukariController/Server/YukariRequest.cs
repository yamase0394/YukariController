
using Newtonsoft.Json;
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
        [DefaultValue("Play")]
        public string Command { get; set; }

        [JsonProperty("text")]
        [DefaultValue("")]
        public string Text { get; set; }
    }
}
