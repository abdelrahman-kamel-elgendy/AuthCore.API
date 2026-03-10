using Microsoft.EntityFrameworkCore;

namespace AuthCore.API.Models;

public class PagedList<T>(List<T> items, int totalCount, int pageNumber, int pageSize)
{
    public List<T> Items { get; set; } = items;
    public int PageNumber { get; set; } = pageNumber;
    public int PageSize { get; set; } = pageSize;
    public int TotalCount { get; set; } = totalCount;

    public int TotalPages => (int)Math.Ceiling(TotalCount / (double)PageSize);
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static async Task<PagedList<T>> CreateAsync(
        IQueryable<T> source, int pageNumber, int pageSize)
    {
        var totalCount = await source.CountAsync();
        var items = await source
            .Skip((pageNumber - 1) * pageSize)
            .Take(pageSize)
            .ToListAsync();

        return new PagedList<T>(items, totalCount, pageNumber, pageSize);
    }
}