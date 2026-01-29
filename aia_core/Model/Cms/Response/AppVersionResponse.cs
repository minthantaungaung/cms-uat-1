namespace aia_core.Model.Cms.Response
{
    public class AppVersionResponse
    {
        public string? MinimumAndroidVersion { get; set; }

        public string? LatestAndroidVersion { get; set; }

        public string? MinimumIosVersion { get; set; }

        public string? LatestIosVersion { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }

        public AppVersionResponse() { }
        public AppVersionResponse(Entities.AppVersion entity)
        {
            MinimumAndroidVersion = entity.MinimumAndroidVersion;
            LatestAndroidVersion = entity.LatestAndroidVersion;
            MinimumIosVersion = entity.MinimumIosVersion;
            LatestIosVersion = entity.LatestIosVersion;
            CreatedDate = entity.CreatedDate;
            UpdatedDate = entity.UpdatedDate;
        }
    }
}
