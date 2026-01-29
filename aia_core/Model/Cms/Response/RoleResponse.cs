using System.Text.Json;

namespace aia_core.Model.Cms.Response
{
    public class RoleResponse
    {
        public Guid? Id { get; set; }
        public string? Title { get; set; }
        public EnumRoleModule[]? Permissions { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public StaffResponse[]? Staffs { get; set; }
        public RoleResponse() { }
        public RoleResponse(Entities.Role entity)
        {
            Id = entity.Id;
            Title = entity.Title;
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;
            Permissions = JsonSerializer.Deserialize<EnumRoleModule[]>(entity.Permissions ?? "[]");
            if(entity.Staff != null)
            {
                Staffs = entity.Staff.Select(s=> new StaffResponse
                {
                    Id = s.Id,
                    RoleId = s.Id,
                    RoleName = entity.Title,
                    Email = s.Email,
                    Name = s.Name,
                    IsActive = s.IsActive,
                    CreatedDate = s.CreatedDate,
                    UpdatedDate = s.UpdatedDate,
                }).ToArray();
            }
        }
    }
}
