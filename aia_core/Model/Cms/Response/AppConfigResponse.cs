using aia_core.Entities;
using aia_core.Model.Cms.Request;

namespace aia_core.Model.Cms.Response
{
    public class AppConfigResponse
    {
        public string? SherContactNumber { get; set; }

        public string? AiaCustomerCareEmail { get; set; }

        public string? AiaMyanmarWebsite { get; set; }

        public string? AiaMyanmarFacebookUrl { get; set; }

        public string? AiaMyanmarInstagramUrl { get; set; }

        public string? AiaMyanmarAddresses { get; set; }

        public string? ClaimTatHours { get; set; }

        public string? ServicingTatHours { get; set; }

        public string? ClaimArchiveFrequency { get; set; }

        public string? ImagingIndividualFileSizeLimit { get; set; }

        public string? ImagingTotalFileSizeLimit { get; set; }

        public string? ClaimEmail { get; set; }

        public string? ServicingEmail { get; set; }

        public string? ServicingArchiveFrequency { get; set; }

        public string? Vitamin_Supply_Note { get; set; }
        public string? Doc_Upload_Note { get; set; }
        public string? Bank_Info_Upload_Note { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public string? Proposition_Request_Receiver { get; set; }

        public List<SampleDocumentResponseModel> sampleDocuments {get;set;}


        public CashlessClaimInfo? localCashlessClaimInfo { get; set; }
        public CashlessClaimInfo? overseasCashlessClaimInfo { get; set; }

        public AppConfigResponse() { }
        public AppConfigResponse(Entities.AppConfig entity) 
        {
            SherContactNumber = entity.SherContactNumber;
            AiaCustomerCareEmail = entity.AiaCustomerCareEmail;
            AiaMyanmarWebsite = entity.AiaMyanmarWebsite;
            AiaMyanmarFacebookUrl = entity.AiaMyanmarFacebookUrl;
            AiaMyanmarInstagramUrl = entity.AiaMyanmarInstagramUrl;
            AiaMyanmarAddresses = entity.AiaMyanmarAddresses;
            ClaimTatHours = entity.ClaimTatHours;
            ServicingTatHours = entity.ServicingTatHours;
            ClaimArchiveFrequency = entity.ClaimArchiveFrequency;
            ImagingIndividualFileSizeLimit = entity.ImagingIndividualFileSizeLimit;
            ImagingTotalFileSizeLimit = entity.ImagingTotalFileSizeLimit;
            ClaimEmail = entity.ClaimEmail;
            ServicingEmail = entity.ServicingEmail;
            ServicingArchiveFrequency = entity.ServicingArchiveFrequency;
            Vitamin_Supply_Note = entity.Vitamin_Supply_Note;
            Doc_Upload_Note = entity.Doc_Upload_Note;
            Bank_Info_Upload_Note = entity.Bank_Info_Upload_Note;
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;
            Proposition_Request_Receiver = entity.Proposition_Request_Receiver;
        }
    }

    public class SampleDocumentResponseModel {
        public string DocTypeName { get; set; }
        public List<InsuranceClaimDocumentResponse> sampleDoc {get;set;}
    }

    public class InsuranceClaimDocumentResponse
    {
        public Guid DocumentId { get; set; }

        public string? DocTypeId { get; set; }

        public string? DocTypeName { get; set; }

        public string? DocumentUrl { get; set; }

        public InsuranceClaimDocumentResponse(Entities.InsuranceClaimDocument entity, Func<EnumFileType, string, string> blobUrl) 
        {
            DocumentId = entity.DocumentId;
            DocTypeId = entity.DocTypeId;
            DocTypeName = entity.DocTypeName;
            if (!string.IsNullOrEmpty(entity.DocumentUrl)) DocumentUrl = $"{blobUrl(EnumFileType.Blog, entity.DocumentUrl)}";
        }
    }
}
