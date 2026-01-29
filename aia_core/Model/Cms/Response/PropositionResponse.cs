using aia_core.Entities;
using System.ComponentModel.DataAnnotations;

namespace aia_core.Model.Cms.Response
{
    public class PropositionResponse
    {
        public Guid? Id { get; set; }
        public string? Type { get; set; }
        public string? NameEn { get; set; }
        public string? NameMm { get; set; }
        public Guid? PropositionCategoryId { get; set; }
        public DateTime? CategoryCreatedOn { get; set; }
        public string? CategoryName { get; set; }
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
        public string? AddressLabel { get; set; }
        public string? AddressLabelMm { get; set; }
        public DateTime? CreatedDate { get; set; }
        public DateTime? UpdatedDate { get; set; }
        public bool? IsActive { get; set; }
        public int? Sort { get; set; }
        public PropositionCategoryResponse? PropositionCategory { get; set; }
        public PropositionBenefitResponse[]? PropositionBenefits { get; set; }
        public PropositionAddressResponse[]? Addresses { get; set; }
        public PropositionBranchResponse[]? PartnerBranches { get; set; }

        public bool? AllowToShowCashlessClaim { get; set; }
        public string? CashlessClaimProcedureInfo { get; set; }

        public PropositionResponse() { }
        public PropositionResponse(Entities.Proposition entity, Func<EnumFileType, string, string> blobUrl)
        {
            Id = entity.Id;
            Type = entity.Type;
            NameEn = entity.NameEn;
            NameMm = entity.NameMm;
            if(!string.IsNullOrEmpty(entity.LogoImage)) LogoImage = $"{blobUrl(EnumFileType.Proposition, entity.LogoImage)}";
            if(!string.IsNullOrEmpty(entity.BackgroudImage)) BackgroudImage = $"{blobUrl(EnumFileType.Proposition, entity.BackgroudImage)}";
            PropositionCategoryId = entity.PropositionCategoryId;
            CategoryCreatedOn = entity.PropositionCategory?.CreatedOn ?? Utils.GetDefaultDate();
            CategoryName = entity.PropositionCategory?.NameEn;
            DescriptionEn = entity.DescriptionEn;
            DescriptionMm = entity.DescriptionMm;
            Eligibility = entity.Eligibility;
            AddressLabel = entity.AddressLabel;
            AddressLabelMm = entity.AddressLabelMm;

            HotlineType = entity.HotlineType;
            HotlineNumber = entity.HotlineNumber;
            HotlineButtonTextEn = entity.HotlineButtonTextEn;
            HotlineButtonTextMm = entity.HotlineButtonTextMm;

            PartnerPhoneNumber = entity.PartnerPhoneNumber;
            PartnerWebsiteLink = entity.PartnerWebsiteLink;
            PartnerFacebookUrl = entity.PartnerFacebookUrl;
            PartnerTwitterUrl = entity.PartnerTwitterUrl;
            PartnerInstagramUrl = entity.PartnerInstagramUrl;
            Sort = entity.Sort;

            if(entity.PropositionCategory!= null
                && entity?.PropositionCategory?.IsDelete == false)
            {
                PropositionCategory = new PropositionCategoryResponse(entity.PropositionCategory, blobUrl);
            }

            if(entity.PropositionBenefits != null
                && entity.PropositionBenefits.Any() == true)
            {
                PropositionBenefits = entity.PropositionBenefits.OrderBy(x => x.Sort)
                    .Select(s=> new PropositionBenefitResponse(s)).ToArray();
            }
            if(entity.PropositionAddresses != null
                && entity.PropositionAddresses.Any())
            {
                Addresses = entity.PropositionAddresses.Select(s=> new PropositionAddressResponse(s)).ToArray();
            }
            if(entity.PropositionBranches != null
                && entity.PropositionBranches.Any())
            {
                PartnerBranches = entity.PropositionBranches.OrderBy(o=> o.Sort).Select(s=> new PropositionBranchResponse(s)).ToArray();
            }

            IsActive = entity.IsActive;
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;

            #region #CashlessClaim
            AllowToShowCashlessClaim = entity.AllowToShowCashlessClaim;
            CashlessClaimProcedureInfo = entity.CashlessClaimProcedureInfo;
            #endregion
        }
    }

    public class DuplicateCheckResponse
    {
        public bool? IsDuplicate { get; set; } = false;
        public string? By { get; set; } = "En";
    }


    public class DuplicateCheckRequest
    {
        public string? NameEn { get; set; }
        public string? NameMM { get; set; }
        public string? Type { get; set; }
        public string? Id { get; set; }
    }
}
