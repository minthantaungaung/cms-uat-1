using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class MemberClient
{
    public Guid Id { get; set; }

    public Guid? MemberId { get; set; }

    public string? ClientNo { get; set; }

    public virtual Member? Member { get; set; }

    public virtual Client? Client { get; set; }
}
