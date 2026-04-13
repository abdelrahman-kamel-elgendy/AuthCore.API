namespace AuthCore.API.Models;

public class PagedList<T>
{
    public List<T> Items { get; set; } = new();
    public PaginationMetadata Metadata { get; set; } = new();

    public PagedList(List<T> items, int totalCount, int pageNumber, int pageSize)
    {
        Items = items;
        Metadata = new PaginationMetadata
        {
            CurrentPage = pageNumber,
            TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize),
            PageSize = pageSize,
            TotalCount = totalCount,
            HasPrevious = pageNumber > 1,
            HasNext = pageNumber < (int)Math.Ceiling(totalCount / (double)pageSize)
        };
    }
}

public class PaginationMetadata
{
    public int CurrentPage { get; set; }
    public int TotalPages { get; set; }
    public int PageSize { get; set; }
    public int TotalCount { get; set; }
    public bool HasPrevious { get; set; }
    public bool HasNext { get; set; }
}