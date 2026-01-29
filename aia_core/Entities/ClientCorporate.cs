using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClientCorporate
{
    public string ClientNo { get; set; } = null!;

    public string? CorporateClientNo { get; set; }

    public string? Name { get; set; }

    public string? Email { get; set; }

    public string? PhoneNo { get; set; }

    public string? Nrc { get; set; }

    public string? PassportNo { get; set; }

    public string? Other { get; set; }

    public DateTime? Dob { get; set; }

    public string? Gender { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public string? MemberType { get; set; }

    public string? MemberTierType { get; set; }

    public DateTime? ScheduledDate { get; set; }
    public string? ClientNoList { get; set; } = null!;
}
