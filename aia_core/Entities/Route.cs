using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Route
{
    public Guid Id { get; set; }

    public string Permission { get; set; } = null!;

    public string Route1 { get; set; } = null!;
}
