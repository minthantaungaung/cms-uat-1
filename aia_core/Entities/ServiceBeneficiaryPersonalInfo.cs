using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class ServiceBeneficiaryPersonalInfo
{
    [Key]
    public Guid ID { get; set; }
    public Guid? ServiceBeneficiaryID { get; set; }
    public Guid? ServiceBeneficiaryShareID { get; set; }
    public string? ClientNo { get; set; }
    public bool? IsNewBeneficiary { get; set; }
    public string? Name { get; set; }
    public string? Gender { get; set; }
    public DateTime? Dob { get; set; }
    public string? MobileNumber { get; set; }
    public string? IdType { get; set; }
    public string? IdValue { get; set; }
    public string? OldMobileNumber { get; set; }
    public string? NewMobileNumber { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? IdFrontImageName { get; set; }
    public string? IdBackImageName { get; set; }
    public string? IdFrontImageDisplayName { get; set; }
    public string? IdBackImageDisplayName { get; set; }
    public string? Front_CMS_Request { get; set; }
    public string? Front_CMS_Response { get; set; }
    public DateTime? Front_CMS_RequestOn { get; set; }
    public DateTime? Front_CMS_ResponseOn { get; set; }
    public string? Back_CMS_Request { get; set; }
    public string? Back_CMS_Response { get; set; }
    public DateTime? Back_CMS_RequestOn { get; set; }
    public DateTime? Back_CMS_ResponseOn { get; set; }
}

