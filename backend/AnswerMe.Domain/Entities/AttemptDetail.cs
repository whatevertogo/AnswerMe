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
    public bool IsMastered { get; set; } // 是否已掌握
    public DateTime? MasteredAt { get; set; } // 掌握时间

    // 导航属性
    public Attempt Attempt { get; set; } = null!;
    public Question Question { get; set; } = null!;
}
