using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Localization
{
    public class LocalizationResponse
    {
        public Dictionary<string, LableValue> LableName { get; set; }
    }

    public class LableValue
    {
        public string English { get; set; }
        public string Burmese { get; set; }
    }
}
