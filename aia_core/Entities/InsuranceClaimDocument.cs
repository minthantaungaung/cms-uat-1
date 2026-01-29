using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class InsuranceClaimDocument
{
    public Guid DocumentId { get; set; }

    public string? DocTypeId { get; set; }

    public string? DocTypeName { get; set; }

    public string? DocumentUrl { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDeleted { get; set; }

    public DateTime? CreatedOn { get; set; }

    public DateTime? UpdatedOn { get; set; }
}
