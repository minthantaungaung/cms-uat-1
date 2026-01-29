using aia_core.UnitOfWork;

namespace aia_core.Model.Mobile.Response
{
    public class ProductListResponse
    {
        public PagaingResponseModel<ProductResponse> list { get; set; }

    }

}
