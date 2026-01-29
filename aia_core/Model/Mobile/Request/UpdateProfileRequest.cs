using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using Microsoft.AspNetCore.Http;

namespace aia_core.Model.Mobile.Request
{
    public class UpdateProfileRequest
    {
        [Required, StringLength(255), DefaultValue(null)]
        public string? FullName { get; set; }

        [Required, DefaultValue(null)]
        public DateTime? Dob { get; set; }

        [Required, DefaultValue(null)]
        public EnumGender? Gender { get; set; }

        public IFormFile? Image { get; set; }
    }

    public class ChangePasswordRequest
    {
        [Required]
        public string? CurrentPassword { get; set; }

        [Required, DefaultValue(null)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Password does not match security requirements.")]
        [StringLength(50, ErrorMessage = "Your password must be at least {2} characters long.", MinimumLength = 8)]
        public string? NewPassword { get; set; }

        [Required, StringLength(50), DefaultValue(null)]
        [Compare("NewPassword")]
        public string? ConfirmNewPassword { get; set; }
    }

    public class ChangeEmailRequest
    {
        [Required, StringLength(255), DefaultValue(null)]
        public string? Email { get; set; }

        [Required]
        public string OtpToken { get; set; }
    }

    public class ChangePhoneRequest
    {
        [Required, StringLength(255), DefaultValue(null)]
        public string? Phone { get; set; }

        [Required]
        public string OtpToken { get; set; }
    }

    public class ResetPasswordRequest
    {
        [Required, DefaultValue(null)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Password does not match security requirements.")]
        [StringLength(50, ErrorMessage = "Your password must be at least {2} characters long.", MinimumLength = 8)]
        public string? Password { get; set; }

        [Required, StringLength(50), DefaultValue(null)]
        [Compare("Password")]
        public string? ConfirmPassword { get; set; }
    }
}
