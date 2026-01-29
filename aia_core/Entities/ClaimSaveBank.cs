using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class ClaimSaveBank
{
    public Guid Id { get; set; }

    public string? ClientNo { get; set; }

    public string? BankCode { get; set; }

    public string? AccountName { get; set; }

    public string? AccountNumber { get; set; }


    public string? BankAccHolderIdValue { get; set; }

    public DateTime? BankAccHolderDob { get; set; }

    public Guid? AppMemberId { get; set; }
}
