using aia_core.UnitOfWork;

namespace aia_core.Model.Mobile.Response
{
    public class ProductDetailResponse
    {
        public ProductDetailModel detail { get; set; }
        public List<ProductBenefitsModels> benefits {get;set;}

        public List<ProductDetailModel> OtherProducts { get; set; }

    }

    public class ProductDetailModel
    {
        public string ID {get;set;} 
        public string ProductName_EN {get;set;}
        public string ProductName_MM {get;set;}
        public string Short_EN {get;set;}
        public string Short_MM {get;set;}
        public string Intro_EN {get;set;}
        public string Intro_MM {get;set;}
        public string IconImage { get; set; }
        public string LogoImage {get;set;}
        public string CoverImage {get;set;}
        public string TagLine_EN {get;set;}
        public string TagLine_MM {get;set;}
        public string IssuedAgeFrom_EN {get;set;}
        public string IssuedAgeFrom_MM {get;set;}
        public string IssuedAgeEnd_EN {get;set;}
        public string IssuedAgeEnd_MM {get;set;}
        public string PolicyTermUp_EN { get; set; }
        public string PolicyTermUp_MM { get; set; }
        public string WebSiteLink { get; set; }
        public string Brochure { get; set; }
        public string CreditingLink { get; set; }
        public string? ProductCode { get; set; }
    }

    public class ProductBenefitsModels
    {
        public string Title_EN { get; set; }
        public string Title_MM { get; set; }
        public string Description_EN { get; set; }
        public string Description_MM { get; set; }
    }

}
