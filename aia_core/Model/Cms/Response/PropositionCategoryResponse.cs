namespace aia_core.Model.Cms.Response
{
    public class PropositionCategoryResponse
    {
        public Guid? Id { get; set; }
        public string? NameEn { get; set; }
        public string? NameMm { get; set; }
        public string? IconImage { get; set; }
        public string? BackgroundImage { get; set; }
        public PropositionCategoryResponse() { }
        public PropositionCategoryResponse(Entities.PropositionCategory entity, Func<EnumFileType, string, string> blobUrl) 
        {
            Id = entity.Id;
            NameEn = entity.NameEn;
            NameMm = entity.NameMm;
            if(!string.IsNullOrEmpty(entity.IconImage)) IconImage = $"{blobUrl(EnumFileType.PropositionCategory, entity.IconImage)}";
            if(!string.IsNullOrEmpty(entity.BackgroundImage)) BackgroundImage = $"{blobUrl(EnumFileType.PropositionCategory, entity.BackgroundImage)}";
        }
    }
}
