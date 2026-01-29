using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Role
{
    public Guid Id { get; set; }

    public string? Title { get; set; }

    public string? Permissions { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public virtual ICollection<Staff> Staff { get; set; } = new List<Staff>();
}
