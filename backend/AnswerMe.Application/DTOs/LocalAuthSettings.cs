namespace AnswerMe.Application.DTOs;

/// <summary>
/// 本地模式认证配置
/// </summary>
public class LocalAuthSettings
{
    public const string SectionName = "LocalAuth";

    /// <summary>
    /// 是否启用本地登录模式
    /// </summary>
    public bool EnableLocalLogin { get; set; } = false;

    /// <summary>
    /// 本地用户的默认用户名
    /// </summary>
    public string DefaultUsername { get; set; } = "LocalUser";

    /// <summary>
    /// 本地用户的默认邮箱
    /// </summary>
    public string DefaultEmail { get; set; } = "local@answerme.local";

    /// <summary>
    /// 本地用户的默认密码（仅用于首次创建）
    /// </summary>
    public string DefaultPassword { get; set; } = "local123";
}
