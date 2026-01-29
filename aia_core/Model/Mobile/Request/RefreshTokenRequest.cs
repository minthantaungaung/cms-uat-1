using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Mobile.Request
{
    public class RefreshTokenRequest
    {
        [Required]
        public string? RefreshToken { get; set; }

        [Required]
        public string? RedirectUri { get; set; }

        [Required]
        public string? ClientId { get; set; }
    }
}
