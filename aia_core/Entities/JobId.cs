using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class JobId
{
    public Guid Id { get; set; }

    public string JobId1 { get; set; } = null!;

    public string PromotionId { get; set; } = null!;

    public DateTime? CreatedDate { get; set; }

    public bool? IsDeleted { get; set; }
}
