using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Occupation
{
    public decimal Id { get; set; }

    public string Code { get; set; } = null!;

    public string Description { get; set; } = null!;

    public virtual ICollection<Client> Clients { get; set; } = new List<Client>();
}
