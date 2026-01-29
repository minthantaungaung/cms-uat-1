using aia_core.UnitOfWork;

namespace aia_core.Model.Mobile.Request
{
    public class PagingRequest
    {
        public int PageSize { get; set; } = 10;
        public int PageIndex { get; set; } = 1;
        //public string SortBy { get; set; }
        //public bool IsDesc { get; set; } = false;

        public int GetSkip()
        { return (PageIndex - 1) * PageSize; }
    }
}
