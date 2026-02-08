namespace AnswerMe.Domain.Entities;

/// <summary>
/// 题库实体
/// </summary>
public class QuestionBank : BaseEntity
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int? DataSourceId { get; set; }
    public string Tags { get; set; } = "[]"; // JSON数组
    public byte[] Version { get; set; } = Array.Empty<byte>(); // 乐观锁版本号

    // 导航属性
    public User User { get; set; } = null!;
    public DataSource? DataSource { get; set; }
    public ICollection<Question> Questions { get; set; } = new List<Question>();
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
}
