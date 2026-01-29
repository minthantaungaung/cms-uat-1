using Microsoft.AspNetCore.Components.Forms;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Request
{
    public class RoleRequest:IValidatableObject
    {
        [Required]
        public string? Title { get; set; }

        [Required]
        public EnumRoleModule[]? Permissions { get; set; }
        public IEnumerable<ValidationResult> Validate(ValidationContext context)
        {
            if(Permissions?.Any() == false) yield return new ValidationResult("Required permissions");
        }
    }
    public class CreateRoleRequest : RoleRequest { }
    public class UpdateRoleRequest : RoleRequest 
    {
        [Required]
        public Guid? Id { get; set; }
    }
}
