using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class ServiceMain
{
    [Key]
    public Guid ID { get; set; }
    public Guid? MainID { get; set; }
    public Guid? ServiceID { get; set; }
    public string? ServiceType { get; set; }
    public string? ServiceStatus { get; set; }
    public DateTime? CreatedDate { get; set; }
    public string? ProductType { get; set; }
    public string? MemberType { get; set; }
    public string? GroupMemberID { get; set; }
    public string? MemberID { get; set; }
    public Guid? LoginMemberID { get; set; }
    public string? PolicyNumber { get; set; }
    public string? PolicyStatus { get; set; }
    public string? MobileNumber { get; set; }

    public string? MemberName { get; set; }
    public string? FERequest { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public DateTime? EstimatedCompletedDate { get; set; }
    public string? ILStatus { get; set; }
    public string? ILMessage { get; set; }
    public string? InternalRemark { get; set; }

    public bool? IsPending { get; set; }

    public Guid? UpdatedBy { get; set; }

    public DateTime? OriginalCreatedDate { get; set; }

    public string? UpdateChannel { get; set; }

    public bool? SentSms { get; set; }
    public DateTime? SentSmsAt { get; set; }
}
