using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace aia_core.Model.Mobile.Response.Master
{
    public class ConfigResponse
    {
        public AiaContactInfo? AiaContactInfo { get; set; }
        public ClaimAndServicing? ClaimAndServicing { get; set; }
        public OtherInfo OtherInfo { get; set; }
    }

    public class AiaContactInfo
    {
        public string? SHERContactNumber { get; set; }
        public string? AiaCustomerCareEmail { get; set; }
        public string? AiaMyanmarWebsite { get; set; }
        public string? AiaMyanmarFacebook { get; set; }
        public string? AiaMyanmarInstagram { get; set; }
        public string? AiaCompanyAddressesAndBranches { get; set; }
    }


    public class ClaimAndServicing
    {
        public string? ClaimTATHours { get; set; }
        public string? ServicingTATHours { get; set; }
        public string? ClaimArchiveFrequency { get; set; }
        public string? ServiceArchiveFrequency { get; set; }
        public string? ImageIndividualFileSizeLimit { get; set; }
        public string? ImageTotalSizeLimit { get; set; }
        public string? ClaimEmail { get; set; }
        public string? ServicingEmail { get; set; }
    }

    public class OtherInfo
    {
        public string? Vitamin_Supply_Note { get; set; }
        public string? Doc_Upload_Note { get; set; }
        public string? Bank_Info_Upload_Note { get; set; }
    }
}
