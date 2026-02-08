namespace AnswerMe.Domain.Entities;

/// <summary>
/// AI数据源配置实体
/// </summary>
public class DataSource : BaseEntity
{
    public int UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty; // openai, qwen, custom_api
    public string Config { get; set; } = string.Empty; // JSON配置
    public bool IsDefault { get; set; } = false;

    // 导航属性
    public User User { get; set; } = null!;
    public ICollection<QuestionBank> QuestionBanks { get; set; } = new List<QuestionBank>();
}
