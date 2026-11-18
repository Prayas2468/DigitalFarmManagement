using Microsoft.EntityFrameworkCore;

namespace DigitalFarmManagement.Infrastructure;

public class PaginatedList<T> : List<T>
{
    public int PageIndex { get; }
    public int TotalPages { get; }
    public int TotalCount { get; }
    public bool HasPrevious => PageIndex > 1;
    public bool HasNext => PageIndex < TotalPages;

    public PaginatedList(List<T> items, int count, int pageIndex, int pageSize)
    {
        PageIndex = pageIndex;
        TotalCount = count;
        TotalPages = (int)Math.Ceiling(count / (double)pageSize);
        AddRange(items);
    }

    public static async Task<PaginatedList<T>> CreateAsync(IQueryable<T> source, int pageIndex, int pageSize)
    {
        if (pageIndex < 1) pageIndex = 1;
        var count = await source.CountAsync();
        var items = await source.Skip((pageIndex - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PaginatedList<T>(items, count, pageIndex, pageSize);
    }
}

public static class PageSize
{
    public const int Default = 8;
}

public static class RouteBag
{
    public static Dictionary<string, string> From(params (string Key, string? Value)[] pairs)
    {
        var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        foreach (var (key, value) in pairs)
        {
            if (!string.IsNullOrWhiteSpace(value))
                dict[key] = value;
        }
        return dict;
    }
}
