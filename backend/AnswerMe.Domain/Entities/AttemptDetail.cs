namespace AnswerMe.Domain.Entities;

/// <summary>
/// 答题详情实体
/// </summary>
public class AttemptDetail : BaseEntity
{
    public int AttemptId { get; set; }
    public int QuestionId { get; set; }
    public string? UserAnswer { get; set; }
    public bool? IsCorrect { get; set; }
    public int? TimeSpent { get; set; } // 秒

    // 导航属性
    public Attempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
