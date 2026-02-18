namespace AnswerMe.Domain.Common;

/// <summary>
/// LIKE 模式转义帮助类，防止 SQL 注入和通配符误匹配
/// </summary>
public static class LikePatternHelper
{
    /// <summary>
    /// 转义 LIKE 模式中的特殊字符，防止 SQL 注入和通配符误匹配
    /// 转义顺序：先转义 [，再转义 % 和 _
    /// </summary>
    /// <param name="pattern">原始搜索模式</param>
    /// <returns>转义后的 LIKE 模式（包含前后的 % 通配符）</returns>
    public static string EscapeLikePattern(string? pattern)
    {
        if (string.IsNullOrEmpty(pattern))
            return "";

        // 必须先转义 [ ，否则会破坏其他转义序列
        return "%" + pattern.Replace("[", "[[]")
                            .Replace("%", "[%]")
                            .Replace("_", "[_]") + "%";
    }
}
