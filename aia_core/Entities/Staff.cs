using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Staff
{
    public Guid Id { get; set; }

    public Guid? RoleId { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? PasswordHash { get; set; }

    public string? PasswordSalt { get; set; }

    public bool? IsActive { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual Role? Role { get; set; }
}
