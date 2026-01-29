using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class ServiceStatusUpdate
{
    [Key]
    public Guid ID { get; set; }
    public Guid? ServiceMainID { get; set; }
    public Guid? ServiceID { get; set; }
    public string? ServiceType { get; set; }
    public string? PolicyNumber { get; set; }
    public string? OldStatus { get; set; }
    public string? NewStatus { get; set; }
    public DateTime? CreatedDate { get; set; }
    public bool IsDone { get; set; }
    public Guid? MemberID {get;set;}
}
