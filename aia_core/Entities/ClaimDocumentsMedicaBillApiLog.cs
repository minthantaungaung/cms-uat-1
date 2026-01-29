using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimDocumentsMedicaBillApiLog
{
    public Guid? Id { get; set; }
    public string? claimId { get; set; }
    public string? admissionDate { get; set; }
    public string? billType { get; set; }
    public string? billingDate { get; set; }
    public string? dischargeDate { get; set; }
    public string? doctorName { get; set; }
    public string? hospitalName { get; set; }
    public string? netAmount { get; set; }
    public string? patientName { get; set; }
    public string? fileName { get; set; }
    public string? response { get; set; }
    public DateTime? SentAt { get; set; }
    public DateTime? ReceivedAt { get; set; }
    public string? status { get; set; }

}
