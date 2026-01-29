using System.Text.Json;

namespace aia_core.Model.Cms.Response
{
    public class StaffResponse
    {
        public Guid? Id { get; set; }
        public Guid? RoleId { get; set; }
        public string? RoleName { get; set; }
        public EnumRoleModule[]? RolePermissions { get; set; }
        public string? Name { get; set; }
        public string? Email { get; set; }
        public bool? IsActive { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public StaffResponse() { }
        public StaffResponse(Entities.Staff entity) 
        {
            Id = entity.Id;
            RoleId = entity.RoleId;
            RoleName = entity?.Role?.Title;
            RolePermissions = JsonSerializer.Deserialize<EnumRoleModule[]>(entity?.Role?.Permissions ?? "[]");
            Name = entity.Name;
            Email = entity.Email;
            IsActive = entity.IsActive;
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;
        }
    }
}
