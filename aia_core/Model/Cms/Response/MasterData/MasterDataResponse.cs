using aia_core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace aia_core.Model.Cms.Response.MasterData
{
    public class CountryResponse
    {
        public List<Country> list { get; set; }
    }

    public class ProvinceResponse
    {
        public List<Province> list { get; set; }
    }

    public class DistrictResponse
    {
        public List<District> list { get; set; }
    }

    public class TownshipResponse
    {
        public List<Township> list { get; set; }
    }


    public class ProductCodeResponse
    { 
        public string? code { get; set; }
        public string? name { get; set; }
        public string? ProductCode { get; set; }

        [JsonIgnore]
        public DateTime? CreatedOn { get; set; }
    }

    public class PolicyStatusResponse
    {
        public string code { get; set; }
        public string name { get; set; }
    }
}
