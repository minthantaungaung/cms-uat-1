using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class ServicePolicyLoanRepaymentDoc
{
    [Key]
    public Guid ID { get; set; }
    public Guid ServicePolicyLoanRepaymentID { get; set; }
    public string? DocName { get; set; }
    public DateTime? CreatedOn { get; set; }

    public string? UploadStatus { get; set; }

    public string? CmsRequest { get; set; }

    public DateTime? CmsRequestOn { get; set; }

    public string? CmsResponse { get; set; }

    public DateTime? CmsResponseOn { get; set; }
}
