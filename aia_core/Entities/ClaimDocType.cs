using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimDocType
{
    public Guid ID { get; set; }
    public string Code { get; set; }
    public string Name { get; set; }
    public string NameMm { get; set; }
    public string NameSample { get; set; }
    public string NameMmSample { get; set; }
    public int? Sort { get; set; }
}