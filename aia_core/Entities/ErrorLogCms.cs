using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ErrorLogCms
{
    public Guid ID { get; set; }

    public string? LogMessage { get; set; }
    public string? ExceptionMessage { get; set; }
    public string? Exception { get; set; }
    public string? EndPoint { get; set; }
    public DateTime? LogDate { get; set; }
    public string? UserID { get; set; }

}
