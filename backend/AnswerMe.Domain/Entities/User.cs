namespace AnswerMe.Domain.Entities;

/// <summary>
/// 用户实体
/// </summary>
public class User : BaseEntity
{
    public string Username { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;

    // 导航属性
    public ICollection<QuestionBank> QuestionBanks { get; set; } = new List<QuestionBank>();
    public ICollection<Attempt> Attempts { get; set; } = new List<Attempt>();
    public ICollection<DataSource> DataSources { get; set; } = new List<DataSource>();
}
