using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Entities;

public class ServiceBeneficiaryShareInfo
{
    [Key]
    public Guid ID { get; set; }
    public Guid? ServiceBeneficiaryID { get; set; }
    public string? ClientNo { get; set; }
    public string Type { get; set; }
    public string? OldRelationShipCode { get; set; }
    public string? NewRelationShipCode { get; set; }
    public decimal? OldPercentage { get; set; }
    public decimal? NewPercentage { get; set; }
    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
    public string? IdValue { get; set; }
}



