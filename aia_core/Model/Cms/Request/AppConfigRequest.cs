using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class AppConfigRequest
    {
        [Required]
        public string? SherContactNumber { get; set; }

        [Required]
        public string? AiaCustomerCareEmail { get; set; }

        [Required]
        public string? AiaMyanmarWebsite { get; set; }

        [Required]
        public string? AiaMyanmarFacebookUrl { get; set; }

        [Required]
        public string? AiaMyanmarInstagramUrl { get; set; }

        [Required]
        public string? AiaMyanmarAddresses { get; set; }

        [Required]
        public string? ClaimTatHours { get; set; }

        [Required]
        public string? ServicingTatHours { get; set; }

        [Required]
        public string? ClaimArchiveFrequency { get; set; }

        [Required]
        public string? ImagingIndividualFileSizeLimit { get; set; }

        [Required]
        public string? ImagingTotalFileSizeLimit { get; set; }

        [Required]
        public string? ClaimEmail { get; set; }

        [Required]
        public string? ServicingEmail { get; set; }

        [Required]
        public string? ServicingArchiveFrequency { get; set; }

        public string? Vitamin_Supply_Note { get; set; }
        public string? Doc_Upload_Note { get; set; }
        public string? Bank_Info_Upload_Note { get; set; }
        public string? Proposition_Request_Receiver { get; set; }

        [Required]
        public CashlessClaimInfo? localCashlessClaimInfo { get; set; }

        [Required]
        public CashlessClaimInfo? overseasCashlessClaimInfo { get; set; }

    }

    public class CashlessClaimInfo
    {
        [Required]
        public string? TitleEn { get; set; }

        [Required]
        public string? TitleMm { get; set; }

        [Required]
        public string? DescriptionEn { get; set; }

        [Required]
        public string? DescriptionMm { get; set; }

        [Required]
        public string? ButtonTextEn { get; set; }

        [Required]
        public string? ButtonTextMm { get; set; }

        [Required]
        public string? Deeplink { get; set; }
    }
}
