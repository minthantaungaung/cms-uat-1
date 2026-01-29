using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class PropositionAddress
{
    public Guid Id { get; set; }

    public Guid? PropositionId { get; set; }

    public string? NameEn { get; set; }

    public string? NameMm { get; set; }

    public string? PhoneNumberEn { get; set; }

    public string? PhoneNumberMm { get; set; }

    public string? AddressEn { get; set; }

    public string? AddressMm { get; set; }

    public string? Longitude { get; set; }

    public string? Latitude { get; set; }

    public virtual Proposition? Proposition { get; set; }
}
