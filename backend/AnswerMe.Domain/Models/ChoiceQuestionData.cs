namespace AnswerMe.Domain.Models;

/// <summary>
/// 选择题数据（单选/多选共用）
/// </summary>
public class ChoiceQuestionData : QuestionData
{
    private List<string> _options = new();
    private List<string> _correctAnswers = new();

    /// <summary>
    /// 选项列表，如 ["A. 选项1", "B. 选项2", ...]
    /// </summary>
    public List<string> Options
    {
        get => _options;
        set => _options = value ?? new List<string>();
    }

    /// <summary>
    /// 正确答案列表
    /// 单选题：1 个元素，如 ["A"]
    /// 多选题：2-3 个元素，如 ["A", "C"]
    /// </summary>
    public List<string> CorrectAnswers
    {
        get => _correctAnswers;
        set => _correctAnswers = value ?? new List<string>();
    }
}
