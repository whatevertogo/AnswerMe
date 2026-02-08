namespace AnswerMe.Application.DTOs;

/// <summary>
/// JWT配置
/// </summary>
public class JwtSettings
{
    public const string SectionName = "JWT";

    public string Secret { get; set; } = string.Empty;
    public int ExpiryDays { get; set; } = 30;
    public string Issuer { get; set; } = string.Empty;
    public string Audience { get; set; } = string.Empty;

    // 无参构造函数(用于配置绑定)
    public JwtSettings() { }

    // 带参数构造函数(用于依赖注入)
    public JwtSettings(string issuer, string audience, string secret, int expiryDays)
    {
        Issuer = issuer;
        Audience = audience;
        Secret = secret;
        ExpiryDays = expiryDays;
    }
}
