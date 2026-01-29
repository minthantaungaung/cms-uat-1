using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class ServicePartialWithdraw
{
    [Key]
    public Guid ID { get; set; }
    public string PolicyNumber { get; set; }
    public int Amount { get; set; }
    public string? Reason { get; set; }
    public string? SignatureImage { get; set; }
    public string? BankName { get; set; }
    public string? BankCode {get;set;}
    public string? BankAccountName { get; set; }
    public string? BankAccountNumber { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public Guid? MemberID { get; set; }
    public string Status { get; set; }
    public string? ILRequest { get; set; }
    public string? ILResponse { get; set; }
    public DateTime? ILRequestOn { get; set; }
    public DateTime? ILResponseOn { get; set; }
    public string? CMS_Request { get; set; }
    public string? CMS_Response { get; set; }
    public DateTime? CMS_RequestOn { get; set; }
    public DateTime? CMS_ResponseOn { get; set; }
    public string? CrmRequest { get; set; }
    public string? CrmResponse { get; set; }
    public DateTime? CrmRequestOn { get; set; }
    public DateTime? CrmResponseOn { get; set; }
}
