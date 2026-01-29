using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ServicePaymentFrequencyValidateMessage
{
    public Guid Id { get; set; }

    public string? PolicyNumber { get; set; }

    public string? Old { get; set; }

    public string? New { get; set; }

    public string? Message { get; set; }

    public DateTime? Date { get; set; }

    public string? ClientNo { get; set; }

    public string? MobileNumber { get; set; }
}
