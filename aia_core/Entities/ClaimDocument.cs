using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimDocument
{
    public Guid Id { get; set; }

    public Guid? MainClaimId { get; set; }
    public Guid? ClaimId { get; set; }

    public Guid? UploadId { get; set; }
    public string? DocTypeName { get; set; }
    public string? DocTypeId { get; set; }

    public string? DocName { get; set; }

    public DateTime? CreatedOn { get; set; }

    public string? UploadStatus { get; set; }

    public string? CmsRequest { get; set; }

    public DateTime? CmsRequestOn { get; set; }

    public string? CmsResponse { get; set; }

    public DateTime? CmsResponseOn { get; set; }

    public virtual ClaimTran? Claim { get; set; }

    public string? DocName2 { get; set; }
}
