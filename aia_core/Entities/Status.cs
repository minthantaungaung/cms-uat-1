using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Status
{
    public decimal Id { get; set; }

    public string StatusType { get; set; } = null!;

    public string ShortDesc { get; set; } = null!;

    public string LongDesc { get; set; } = null!;
}
