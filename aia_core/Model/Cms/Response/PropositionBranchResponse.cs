namespace aia_core.Model.Cms.Response
{
    public class PropositionBranchResponse
    {
        public Guid? Id { get; set; }

        public Guid? PropositionId { get; set; }

        public string? NameEn { get; set; }

        public string? NameMm { get; set; }
        public PropositionBranchResponse() { }
        public PropositionBranchResponse(Entities.PropositionBranch entity)
        {
            Id = entity.Id;
            PropositionId = entity.PropositionId;
            NameEn = entity.NameEn;
            NameMm = entity.NameMm;
        }
    }
}
