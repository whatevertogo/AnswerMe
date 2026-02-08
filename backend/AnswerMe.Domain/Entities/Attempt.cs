namespace AnswerMe.Domain.Entities;

/// <summary>
/// 答题记录实体
/// </summary>
public class Attempt : BaseEntity
{
    public int UserId { get; set; }
    public int QuestionBankId { get; set; }
    public DateTime StartedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CompletedAt { get; set; }
    public decimal? Score { get; set; }
    public int TotalQuestions { get; set; }

    // 导航属性
    public virtual User User { get; set; } = null!;
    public virtual QuestionBank QuestionBank { get; set; } = null!;
    public virtual ICollection<AttemptDetail> AttemptDetails { get; set; } = new List<AttemptDetail>();
}
