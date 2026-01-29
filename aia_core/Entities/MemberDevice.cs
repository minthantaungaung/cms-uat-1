using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class MemberDevice
{
    public Guid Id { get; set; }

    public string? MemberId { get; set; }

    public string? DeviceType { get; set; }

    public string? PushToken { get; set; }

    public DateTime? CreatedDate { get; set; }
}
