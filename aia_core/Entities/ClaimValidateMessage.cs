using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimValidateMessage
{
    public Guid Id { get; set; }

    public DateTime? Date { get; set; }

    public string? PolicyNumber { get; set; }

    public string? MemberId { get; set; }

    public string? MemberName { get; set; }

    public string? MemberPhone { get; set; }

    public string? Message { get; set; }

    public string? ClaimType { get; set; }
    public string? ClaimFormType { get; set; }
}
