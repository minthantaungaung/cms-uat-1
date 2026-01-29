using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Localization
{
    public string Key { get; set; } = null!;

    public string English { get; set; } = null!;

    public string Burmese { get; set; } = null!;

    public Guid Id { get; set; }

    public bool IsDeleted { get; set; }

    public DateTime CreatedAt { get; set; }
}
