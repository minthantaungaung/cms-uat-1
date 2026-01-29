using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class AppVersion
{
    public string Id { get; set; } = null!;

    public string? MinimumAndroidVersion { get; set; }

    public string? LatestAndroidVersion { get; set; }

    public string? MinimumIosVersion { get; set; }

    public string? LatestIosVersion { get; set; }

    public DateTime? CreatedDate { get; set; }

    public DateTime? UpdatedDate { get; set; }
}
