using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms
{
    public class CmsAccessUser
    {
        public string ID { get; set; }
        public string Email { get; set; }
        public string Name { get; set; }
        public string RoleID { get; set; }
        public string RoleName { get; set; }
        public string GenerateToken {get;set;}
    }


    public class CmsAccessUserUpdate
    {
       

        [Required]
        public string Email { get; set; }

        [Required]
        [RegularExpression(@"^[a-zA-Z0-9\s]+$", ErrorMessage = "Only letters and numbers are allowed.")]
        public string Name { get; set; }
    }
}