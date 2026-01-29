using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class AppConfig
{
    public string Id { get; set; } = null!;

    public string? SherContactNumber { get; set; }

    public string? AiaCustomerCareEmail { get; set; }

    public string? AiaMyanmarWebsite { get; set; }

    public string? AiaMyanmarFacebookUrl { get; set; }

    public string? AiaMyanmarInstagramUrl { get; set; }

    public string? AiaMyanmarAddresses { get; set; }

    public string? ClaimTatHours { get; set; }

    public string? ServicingTatHours { get; set; }

    public string? ClaimArchiveFrequency { get; set; }

    public string? ServicingArchiveFrequency { get; set; }

    public string? ImagingTotalFileSizeLimit { get; set; }

    public string? ImagingIndividualFileSizeLimit { get; set; }

    public string? ClaimEmail { get; set; }

    public string? ServicingEmail { get; set; }

    public bool Maintenance_On { get; set; }
    public string? Maintenance_Title { get; set; }
    public string? Maintenance_Desc { get; set; }

    public string? Vitamin_Supply_Note { get; set; }
    public string? Doc_Upload_Note { get; set; }
    public string? Bank_Info_Upload_Note { get; set; }

    public bool? Coast_Claim_IsSystemDate { get; set; }
    public DateTime? Coast_Claim_CustomDate { get; set; }
    public bool? Coast_Servicing_IsSystemDate { get; set; }
    public DateTime? Coast_Servicing_CustomDate { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }
    public string? Proposition_Request_Receiver { get; set; }
}
