using System.Text.Json;
using System.Text.Json.Serialization;
using AnswerMe.Domain.Models;

namespace AnswerMe.Domain.Models;

/// <summary>
/// 题目数据抽象基类
/// 使用多态序列化支持所有题型数据
/// </summary>
[JsonPolymorphic(TypeDiscriminatorPropertyName = "type")]
[JsonDerivedType(typeof(ChoiceQuestionData), typeDiscriminator: "choice")]
[JsonDerivedType(typeof(BooleanQuestionData), typeDiscriminator: "boolean")]
[JsonDerivedType(typeof(FillBlankQuestionData), typeDiscriminator: "fillBlank")]
[JsonDerivedType(typeof(ShortAnswerQuestionData), typeDiscriminator: "shortAnswer")]
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
