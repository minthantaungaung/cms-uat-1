using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class AuditLog
{
    public Guid Id { get; set; }

    public string? ObjectGroup { get; set; }

    public string? Action { get; set; }

    public Guid? ObjectId { get; set; }

    public string? ObjectName { get; set; }

    public string? OldData { get; set; }

    public string? NewData { get; set; }

    public Guid? StaffId { get; set; }

    public DateTime? LogDate { get; set; }
}
