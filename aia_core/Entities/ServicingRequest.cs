using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class ServicingRequest
{
    [Key]
    public Guid ServicingID { get; set; }
    public string? ServicingType { get; set; }
    public string? ClientNo { get; set; }
    public string? MaritalStatus_Old { get; set; }
    public string? MaritalStatus_New { get; set; }
    public string? FatherName_Old { get; set; }
    public string? FatherName_New { get; set; }
    public string? PhoneNumber_Old { get; set; }
    public string? PhoneNumber_New { get; set; }
    public string? EmailAddress_Old { get; set; }
    public string? EmailAddress_New { get; set; }
    public string? Country_Old { get; set; }
    public string? Country_New { get; set; }
    public string? Province_Old { get; set; }
    public string? Province_New { get; set; }
    public string? Distinct_Old { get; set; }
    public string? Distinct_New { get; set; }
    public string? Township_Old { get; set; }
    public string? Township_New { get; set; }
    public string? Building_Old { get; set; }
    public string? Building_New { get; set; }
    public string? Street_Old { get; set; }
    public string? Street_New { get; set; }
    public string? SignatureImage { get; set; }
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
    public Guid? MainID { get; set; }

    public string? UpdateChannel { get; set; }
}
