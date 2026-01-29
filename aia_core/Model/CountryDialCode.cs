using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model
{
    public class CountryDialCode
    {
        public string name { get; set; }
        public string dial_code { get; set; }
        public string code { get; set; }
    }

    public class SeparateCountryCodeModel
    {
        public string OriginalNumber {get;set;}
        public string Name { get; set; }
        public string CountryCode {get;set;}
        public string MobileNumber { get; set; }
    }
}