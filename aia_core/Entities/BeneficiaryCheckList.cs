using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class BeneficiaryCheckList
{
    public Guid Id { get; set; }
    public Guid? ScheduleId { get; set; }

    public Guid? ServiceId { get; set; }

    public Guid? ShareItemId { get; set; }

    public string? ClientNo { get; set; }

    public string? Type { get; set; }

    public bool? IsCompleted { get; set; }

    public string? UpdateValue { get; set; }

    public string? UpdateValueType { get; set; }

    public DateTime? CreatedOn { get; set; }
}
