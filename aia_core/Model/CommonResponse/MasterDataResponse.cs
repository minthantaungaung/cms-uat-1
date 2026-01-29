using aia_core.Entities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.CommonResponse
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
}
