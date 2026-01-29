using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class PaymentChangeConfig
{
    public Guid Id { get; set; }

    public decimal? Value { get; set; }

    public string? DescEn { get; set; }

    public string? DescMm { get; set; }

    public string? Code { get; set; }

    public string? Type { get; set; }

    public bool? Status { get; set; }

    public DateTime? CreatedOn { get; set; }
    public DateTime? UpdatedOn { get; set; }
}
