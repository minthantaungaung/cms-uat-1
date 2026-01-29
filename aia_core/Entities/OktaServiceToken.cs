namespace aia_core.Entities
{
    public partial class OktaServiceToken
    {
        public string Id { get; set; } = null!;

        public string? TokenType { get; set; }

        public long? ExpiresIn { get; set; }

        public string? AccessToken { get; set; }

        public string? Scope { get; set; }

        public DateTime? CreatedDate { get; set; }

        public DateTime? UpdatedDate { get; set; }
    }
}
