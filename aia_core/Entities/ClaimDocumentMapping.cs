using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimDocumentMapping
{
    public Guid Id { get; set; }

    public string DocTypeIdlist { get; set; } = null!;

    public string BenefitFormType { get; set; } = null!;

    public string TypeNameList { get; set; } = null!;
}
