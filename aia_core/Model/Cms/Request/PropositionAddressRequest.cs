using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class PropositionAddressRequest
    {
        [Required]
        public string? NameEn { get; set; }

        [Required]
        public string? NameMm { get; set; }

        [Required]
        public string? PhoneNumberEn { get; set; }

        [Required]
        public string? PhoneNumberMm { get; set; }

        [Required]
        public string? AddressEn { get; set; }

        [Required]
        public string? AddressMm { get; set; }

        [Required]
        public string? Longitude { get; set; }

        [Required]
        public string? Latitude { get; set; }
    }
}
