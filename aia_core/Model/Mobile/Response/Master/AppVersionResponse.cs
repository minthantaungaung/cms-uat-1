using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Master
{
    public class AppVersionResponse
    {
        public string? AndroidMinVersion { get; set; }
        public string? AndroidLatestVersion { get; set; }
        public string? IosMinVersion { get; set; }
        public string? IosLatestVersion { get; set; }
    }
}
