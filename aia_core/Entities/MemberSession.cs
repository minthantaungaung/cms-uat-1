using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class MemberSession
{
    public Guid Id { get; set; }

    public Guid? MemberId { get; set; }

    public Guid? SessionId { get; set; }

    public string? Auth0Userid { get; set; }
}
