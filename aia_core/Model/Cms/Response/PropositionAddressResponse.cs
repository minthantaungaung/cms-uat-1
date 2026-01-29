namespace aia_core.Model.Cms.Response
{
    public class PropositionAddressResponse
    {
        public Guid? Id { get; set; }

        public Guid? PropositionId { get; set; }

        public string? NameEn { get; set; }

        public string? NameMm { get; set; }

        public string? PhoneNumberEn { get; set; }

        public string? PhoneNumberMm { get; set; }

        public string? AddressEn { get; set; }

        public string? AddressMm { get; set; }

        public string? Longitude { get; set; }

        public string? Latitude { get; set; }
        public PropositionAddressResponse() { }
        public PropositionAddressResponse(Entities.PropositionAddress entity) 
        {
            Id = entity.Id;
            PropositionId = entity.PropositionId;
            NameEn = entity.NameEn;
            NameMm = entity.NameMm;
            PhoneNumberEn = entity.PhoneNumberEn;
            PhoneNumberMm = entity.PhoneNumberMm;
            AddressEn = entity.AddressEn;
            AddressMm = entity.AddressMm;
            Longitude = entity.Longitude;
            Latitude = entity.Latitude;
        }
    }
}
