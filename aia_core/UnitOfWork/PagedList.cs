namespace aia_core.UnitOfWork
{
    public class PagedList<T>
    {
        public PagedList(IEnumerable<T> source, int pageIndex, int pageSize)
        {
            this.TotalCount = source.Count();
            this.CurrentPage = pageIndex == 0 ? 1 : (int)Math.Ceiling((double)pageIndex / pageSize) + 1;
            this.PageSize = pageSize;
            this.TotalPage = (this.TotalCount > 0 && this.PageSize > 0) ? (int)Math.Ceiling((double)this.TotalCount / this.PageSize) : 0;
            this.DataList = source.Skip(pageSize * pageIndex).Take(pageSize).ToList();
        }

        public PagedList(IEnumerable<T> source, long totalCount, int pageNumber, int pageSize)
        {
            this.TotalCount = totalCount;
            this.PageSize = pageSize;
            this.CurrentPage = pageNumber;
            this.TotalPage = (this.TotalCount > 0 && this.PageSize > 0) ? (int)Math.Ceiling((double)this.TotalCount / this.PageSize) : 1;
            this.DataList = source.ToList();
        }

        public long TotalCount { get; }
        public int TotalPage { get; }
        public int CurrentPage { get; }
        public int PageSize { get; }
        public bool HasNextPage { get { return TotalPage > CurrentPage ? true : false; } }
        public bool HasPreviousPage { get { return CurrentPage > 1 ? true : false; } }
        public List<T> DataList { get; }
    }
}
