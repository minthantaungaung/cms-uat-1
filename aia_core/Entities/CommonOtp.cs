using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class CommonOtp
{
    public Guid Id { get; set; }

    public string? OtpCode { get; set; }

    public DateTime? OtpExpiry { get; set; }

    public string? OtpType { get; set; }

    public string? OtpTo { get; set; }

    public Guid? MemberId { get; set; }
    public DateTime? CreatedOn { get; set; }
    public bool? IsUsed { get; set; }
    public DateTime? UsedOn { get; set; }
}
