using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class CmsUserSession
{
    public Guid? SessionId { get; set; }

    public Guid? UserId { get; set; }

    public string? Token { get; set; }

    public DateTime? GeneratedOn { get; set; }

    public DateTime? ExpiredOn { get; set; }
}
