using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimFollowup
{
    public Guid Id { get; set; }

    public Guid? ClaimId { get; set; }

    public string? DocId { get; set; }

    public string? DocName { get; set; }

    public string? DocName2 { get; set; }

    public string? DocTypeName { get; set; }

    public string? RequiredInfo { get; set; }

    public string? CmsRequest { get; set; }
    public string? CmsResponse { get; set; }
    public string? CmsStatus { get; set; }
    public DateTime? CmsRequestOn { get; set; }
    public DateTime? CmsResponseOn { get; set; }
}
