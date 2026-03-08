namespace AuthCore.API.Models;

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }

    public PaginationMetadata(PagedList<object> pagedList)
    {
        CurrentPage = pagedList.PageNumber;
        TotalPages = pagedList.TotalPages;
        PageSize = pagedList.PageSize;
        TotalCount = pagedList.TotalCount;
        HasPrevious = pagedList.HasPreviousPage;
        HasNext = pagedList.HasNextPage;
    }
}