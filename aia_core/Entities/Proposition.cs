using System;
using System.Collections.Generic;

namespace aia_core.Entities;

public partial class Proposition
{
    public Guid Id { get; set; }

    public string? Type { get; set; }

    public string? NameEn { get; set; }

    public string? NameMm { get; set; }

    public Guid? PropositionCategoryId { get; set; }

    public string? LogoImage { get; set; }

    public string? BackgroudImage { get; set; }

    public string? DescriptionEn { get; set; }

    public string? DescriptionMm { get; set; }

    public string? Eligibility { get; set; }

    public string? HotlineType { get; set; }

    public string? PartnerPhoneNumber { get; set; }

    public string? PartnerWebsiteLink { get; set; }

    public string? PartnerFacebookUrl { get; set; }

    public string? PartnerInstagramUrl { get; set; }

    public string? PartnerTwitterUrl { get; set; }

    public string? HotlineButtonTextEn { get; set; }

    public string? HotlineButtonTextMm { get; set; }

    public string? HotlineNumber { get; set; }

    public DateTime? CreatedDate { get; set; }

    public Guid? CreatedBy { get; set; }

    public DateTime? UpdatedDate { get; set; }

    public Guid? UpdatedBy { get; set; }

    public bool? IsActive { get; set; }

    public bool? IsDelete { get; set; }

    public int? Sort { get; set; }
    public string? AddressLabel { get; set; }

    public string? AddressLabelMm { get; set; }

    public bool? AllowToShowCashlessClaim { get; set; }
    public string? CashlessClaimProcedureInfo { get; set; }
    public virtual ICollection<PropositionAddress> PropositionAddresses { get; set; } = new List<PropositionAddress>();

    public virtual ICollection<PropositionBenefit> PropositionBenefits { get; set; } = new List<PropositionBenefit>();

    public virtual ICollection<PropositionBranch> PropositionBranches { get; set; } = new List<PropositionBranch>();

    public virtual PropositionCategory? PropositionCategory { get; set; }
}
