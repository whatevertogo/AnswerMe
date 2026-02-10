using System.Text.Json;
using AnswerMe.Domain.Models;

namespace AnswerMe.Domain.Models;

/// <summary>
/// 题目数据抽象基类
/// 注意：不使用多态序列化，而是根据 QuestionType 枚举选择正确的类型
/// </summary>
public abstract class QuestionData
{
    /// <summary>
    /// 题目解析/说明
    /// </summary>
    public string? Explanation { get; set; }

    /// <summary>
    /// 难度级别：easy, medium, hard
    /// </summary>
    public string Difficulty { get; set; } = "medium";
}

/// <summary>
/// JSON 序列化选项配置
/// </summary>
public static class QuestionDataJsonOptions
{
    public static JsonSerializerOptions Default { get; } = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false
    };
}
