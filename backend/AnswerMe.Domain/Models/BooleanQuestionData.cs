namespace AnswerMe.Domain.Models;

/// <summary>
/// 判断题数据
/// </summary>
public class BooleanQuestionData : QuestionData
{
    /// <summary>
    /// 正确答案：true 或 false
    /// </summary>
    public bool CorrectAnswer { get; set; }
}
