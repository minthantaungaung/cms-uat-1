namespace aia_core.Model.Mobile.Response
{
    public class PropositionDetailResponse
    {
        public Guid ID { get; set; }
        public string Name_EN { get; set; }
        public string Name_MM { get; set; }
        public string Description_EN { get; set; }
        public string Description_MM { get; set; }

        public string HotlineType { get; set; }
        public string PartnerPhoneNumber { get; set; }
        public string PartnerWebsiteLink { get; set; }
        public string PartnerFacebookUrl { get; set; }
        public string PartnerInstagramUrl { get; set; }
        public string PartnerTwitterUrl { get; set; }
        public string HotlineButtonTextEn { get; set; }
        public string HotlineButtonTextMm { get; set; }
        public string HotlineNumber { get; set; }

        public string BackgroudImage { get; set; }
        public string LogoImage { get; set; }

        public string Eligibility { get; set; }
        public string Type { get; set; }

        public List<PropositionBenefitDetailGroup> benefits { get; set; }
        public List<PropositonBranchResponse> branchs { get; set; }
        public List<PropositonAddressResponse> address { get; set; }
        public string? AddressLabel { get; set; }
        public string? AddressLabelMm { get; set; }

        public List<PropositionsResponse> relatedPropositions { get; set; }

        public bool? AllowToShowCashlessClaim { get; set; }
        public string? CashlessClaimProcedureInfo { get; set; }
        public CashlessClaimInfoResponse? cashlessClaimInfo { get; set; }
    }

    public class PropositionBenefitDetailGroup
    {
        public string GroupName_EN { get; set; }
        public string GroupName_MM { get; set; }
        public List<string> benefits_EN { get; set; }
        public List<string> benefits_MM { get; set; }
    }

    public class PropositonBranchResponse
    {
        public Guid Branch_ID { get; set; }
        public string Name_EN {get;set;}
        public string Name_MM { get; set; }
    }

    public class PropositonAddressResponse
    {
        public string Name_EN {get;set;}
        public string Name_MM { get; set; }
        public string PhoneNumber_EN { get; set; }
        public string PhoneNumber_MM { get; set; }
        public string Address_EN { get; set; }
        public string Address_MM { get; set; }
        public string Longitude { get; set; }
        public string Latitude { get; set; }
    }
    public class CashlessClaimInfo
    {
        public string? TitleEn { get; set; }
        public string? TitleMm { get; set; }
        public string? DescriptionEn { get; set; }
        public string? DescriptionMm { get; set; }
        public string? ButtonTextEn { get; set; }
        public string? ButtonTextMm { get; set; }
        public string? Deeplink { get; set; }
    }

    public class CashlessClaimInfoResponse
    {
        public CashlessClaimInfo? local { get; set; }
        public CashlessClaimInfo? overseas { get; set; }
    }
}
