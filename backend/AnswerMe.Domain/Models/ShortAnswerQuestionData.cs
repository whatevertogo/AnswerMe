namespace AnswerMe.Domain.Models;

/// <summary>
/// 简答题数据
/// </summary>
public class ShortAnswerQuestionData : QuestionData
{
    /// <summary>
    /// 参考答案
    /// </summary>
    public string ReferenceAnswer { get; set; } = string.Empty;
}
