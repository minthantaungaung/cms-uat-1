using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class AppVersionRequest
    {
        [Required]
        public string? MinimumAndroidVersion { get; set; }

        [Required]
        public string? LatestAndroidVersion { get; set; }

        [Required]
        public string? MinimumIosVersion { get; set; }

        [Required]
        public string? LatestIosVersion { get; set; }
    }
}
