using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ProductCoverage
{
    public Guid Id { get; set; }

    public Guid? ProductId { get; set; }

    public Guid? CoverageId { get; set; }

    public virtual Coverage? Coverage { get; set; }

    public virtual Product? Product { get; set; }
}
