using aia_core.Extension.aia_core.Extension;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class StaffRequest
    {
        [Required]
        public Guid? RoleId { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Email { get; set; }

        [Required]
        public bool? Status { get; set; }

        [Required]
        [StrongPassword]
        [NotEqualToProperty("Name", ErrorMessage = "Password cannot be the same as the Name.")]
        public string Password {get;set;}
    }

    public class CreateStaffRequest : StaffRequest { }
    public class UpdateStaffRequest
    {
        [Required]
        public Guid? Id { get; set; }
        [Required]
        public Guid? RoleId { get; set; }

        [Required]
        public string? Name { get; set; }

        [Required]
        public string? Email { get; set; }
       
        public bool? Status { get; set; }

        [StrongPassword]
        [NotEqualToProperty("Name", ErrorMessage = "Password cannot be the same as the Name.")]
        public string? Password { get; set; }
    }
}
