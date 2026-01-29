using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ServicingStatus
{
    public decimal id { get; set; }

    public string short_desc { get; set; } = null!;

    public string long_desc { get; set; } = null!;
    public string crm_code { get; set; } = null!;
}
