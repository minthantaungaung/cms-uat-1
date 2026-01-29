using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Mobile.Request
{
    public class RegisterRequest
    {
        [Required, StringLength(255), DefaultValue(null)] 
        public string? FullName { get; set; }

        [Required, DefaultValue(null)]
        public DateTime? Dob { get; set; }

        [Required, DefaultValue(null)]
        public EnumGender? Gender { get; set; }

        //[Required, StringLength(255), DefaultValue(null)]

        [StringLength(255), DefaultValue(null)]
        public string? Email { get; set; }

        [Required, StringLength(255), DefaultValue(null)]
        public string? Phone { get; set; }

        [Required, DefaultValue(null)]
        [RegularExpression(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d).{8,}$", ErrorMessage = "Password does not match security requirements.")]
        [StringLength(50, ErrorMessage = "Your password must be at least {2} characters long.", MinimumLength = 8)]
        public string? Password { get; set; }

        [Required, StringLength(50), DefaultValue(null)]
        [Compare("Password")]
        public string? ConfirmPassword { get; set; }

        [Required, DefaultValue(null)]
        public EnumIdenType? IdentificationType { get; set; }

        [Required, StringLength(255), DefaultValue(null)]
        public string? IdentificationValue { get; set; }
    }

    public class CheckIdentificationRequest
    {
        [Required, DefaultValue(null)]
        public EnumIdenType? IdentificationType { get; set; }

        [Required, StringLength(255), DefaultValue(null)]
        public string? IdentificationValue { get; set; }
    }
}
