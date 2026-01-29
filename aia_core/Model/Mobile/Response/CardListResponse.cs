namespace aia_core.Model.Mobile.Response
{
    public class CardListResponse
    {
        public string ClientNo { get; set; }
        public string Name { get; set; }
        public string MemberSince { get; set; }
        public string MemberType { get; set; }
        public bool? IsRubyMember { get; set; }
        public List<CorporateSolutions> corporateSolutions { get; set; }
        public List<PolicyData> policyList { get; set; }
        public int? Sort { get; set; }
    }

    public class CorporateSolutions
    {
        public string PolicyNumber { get; set; }
        public string ProductName { get; set; }
        public string ProductNameMm { get; set; }
    }


    public class PolicyData
    {
        public string PolicyNumber { get; set; }
        public string ProductName { get; set; }
        public string ProductNameMm { get; set; }
    }
}
