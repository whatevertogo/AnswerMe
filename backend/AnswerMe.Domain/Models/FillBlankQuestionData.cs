namespace AnswerMe.Domain.Models;

/// <summary>
/// 填空题数据
/// </summary>
public class FillBlankQuestionData : QuestionData
{
    /// <summary>
    /// 可接受的答案列表（支持同义词）
    /// </summary>
    public List<string> AcceptableAnswers { get; set; } = new();
}
