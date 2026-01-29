using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ProductType
{
    public decimal Id { get; set; }

    public string ShortDesc { get; set; } = null!;

    public string LongDesc { get; set; } = null!;

    public virtual ICollection<Policy> Policies { get; set; } = new List<Policy>();
}
