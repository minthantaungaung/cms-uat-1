using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using aia_core.Entities;

namespace aia_core.Model.Mobile.Response.Master
{
    public class MasterResponse
    {
        public AppVersionResponse? AppVersionResponse { get; set; }
        public ConfigResponse? ConfigResponse { get; set; }

        public bool? IsShowGetHelpsButton { get; set; }
    }

    public class CountryResponse
    {
        public List<Country> list {get;set;}
    }

    public class ProvinceResponse
    {
        public List<Province> list {get;set;}
    }

    public class DistrictResponse
    {
        public List<District> list {get;set;}
    }

    public class TownshipResponse
    {
        public List<Township> list {get;set;}
    }
}
