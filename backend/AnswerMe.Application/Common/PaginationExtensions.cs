namespace AnswerMe.Application.Common;

/// <summary>
/// 分页结果
/// </summary>
public class PagedResult<T>
{
    public List<T> Data { get; set; } = new();
    public bool HasMore { get; set; }
    public int? NextCursor { get; set; }
}

/// <summary>
/// 分页扩展方法
/// </summary>
public static class PaginationExtensions
{
    /// <summary>
    /// 将列表转换为分页结果
    /// </summary>
    public static PagedResult<T> ToPagedResult<T>(this IList<T> items, int pageSize)
    {
        var hasMore = items.Count > pageSize;
        var data = items.Take(pageSize).ToList();

        return new PagedResult<T>
        {
            Data = data,
            HasMore = hasMore,
            NextCursor = hasMore ? data.LastOrDefault()?.GetCursorValue() : null
        };
    }

    /// <summary>
    /// 获取游标值 (默认使用 Id 属性)
    /// </summary>
    private static int? GetCursorValue<T>(this T item)
    {
        var idProperty = typeof(T).GetProperty("Id");
        return idProperty?.GetValue(item) as int?;
    }
}
