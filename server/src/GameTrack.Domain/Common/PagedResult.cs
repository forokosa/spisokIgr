namespace GameTrack.Domain.Common;

/// <summary>Постраничный результат запроса к хранилищу.</summary>
public sealed class PagedResult<T>
{
    public PagedResult(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
    {
        Items = Guard.NotNull(items, nameof(items));
        Page = Guard.Page(page);
        PageSize = Guard.PageSize(pageSize, int.MaxValue);
        if (totalCount < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalCount), "Общее количество не может быть отрицательным.");
        }

        TotalCount = totalCount;
    }

    public IReadOnlyList<T> Items { get; }

    public int Page { get; }

    public int PageSize { get; }

    public int TotalCount { get; }

    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);

    public bool HasPrevious => Page > 1 && TotalPages > 0;

    public bool HasNext => Page < TotalPages;
}
