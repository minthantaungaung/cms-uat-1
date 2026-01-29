namespace aia_core.Model.Cms.Response
{
    public class PropositionBenefitResponse
    {
        public Guid? Id { get; set; }
        public Guid? PropositionId { get; set; }
        public string? NameEn { get; set; }
        public string? NameMm { get; set; }
        public string? Type { get; set; }
        public string? GroupNameEn { get; set; }
        public string? GroupNameMm { get; set; }
        public PropositionBenefitResponse() { }
        public PropositionBenefitResponse(Entities.PropositionBenefit entity) 
        {
            Id = entity.Id;
            PropositionId = entity.PropositionId;
            NameEn = entity.NameEn;
            NameMm = entity.NameMm;
            Type = entity.Type;
            GroupNameEn = entity.GroupNameEn;
            GroupNameMm = entity.GroupNameMm;
        }
    }
}
