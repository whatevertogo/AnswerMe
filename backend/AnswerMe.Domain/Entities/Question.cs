namespace AnswerMe.Domain.Entities;

/// <summary>
/// 题目实体
/// </summary>
public class Question : BaseEntity
{
    public int QuestionBankId { get; set; }
    public string QuestionText { get; set; } = string.Empty;
    public string QuestionType { get; set; } = string.Empty; // choice, fill_blank, essay
    public string? Options { get; set; } // JSON数组
    public string CorrectAnswer { get; set; } = string.Empty;
    public string? Explanation { get; set; }
    public string Difficulty { get; set; } = "medium"; // easy, medium, hard
    public int OrderIndex { get; set; }

    // 导航属性
    public QuestionBank QuestionBank { get; set; } = null!;
    public ICollection<AttemptDetail> AttemptDetails { get; set; } = new List<AttemptDetail>();
}
