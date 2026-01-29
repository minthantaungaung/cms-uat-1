using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class DefaultCmsImage
{
    public Guid id { get; set; }

    public string? image_for { get; set; }

    public string? image_url { get; set; }

    public DateTime? created_at { get; set; }
}
