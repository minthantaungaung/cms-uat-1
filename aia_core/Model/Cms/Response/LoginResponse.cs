namespace aia_core.Model.Cms.Response
{
    public class LoginResponse
    {
        public string accessToken {get;set;}
        public Permission Permission { get; set; }
    }


    public class Permission
    {
        public Guid? StaffId { get; set; }
        public string StaffEmail { get; set; }
        public Guid? RoleId { get; set; }
        public string RoleName { get; set; }
        public string[] Permissions { get; set; }

    }
}