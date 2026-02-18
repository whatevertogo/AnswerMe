namespace AnswerMe.Application.Common;

/// <summary>
/// 批量操作限制常量
/// </summary>
public static class BatchLimits
{
    /// <summary>
    /// 批量创建题目最大数量
    /// </summary>
    public const int MaxBatchCreateSize = 100;

    /// <summary>
    /// 批量删除题目最大数量
    /// </summary>
    public const int MaxBatchDeleteSize = 100;

    /// <summary>
    /// 默认分页大小
    /// </summary>
    public const int DefaultPageSize = 50;

    /// <summary>
    /// 最大分页大小
    /// </summary>
    public const int MaxPageSize = 200;

    /// <summary>
    /// 搜索结果最大返回数量
    /// </summary>
    public const int MaxSearchResults = 500;
}
