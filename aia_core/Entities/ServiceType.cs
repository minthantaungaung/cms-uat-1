using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ServiceType
{
    public Guid ServiceTypeId { get; set; }

    public string? ServiceTypeEnum { get; set; }

    public string? ServiceTypeNameEn { get; set; }

    public string? ServiceTypeNameMm { get; set; }

    public Guid? MainServiceTypeId { get; set; }
}
