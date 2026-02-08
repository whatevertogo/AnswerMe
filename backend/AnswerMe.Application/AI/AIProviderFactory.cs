using AnswerMe.Application.AI;
using Microsoft.Extensions.Logging;

namespace AnswerMe.Application.AI;

/// <summary>
/// AI Provider工厂
/// </summary>
public class AIProviderFactory
{
    private readonly IEnumerable<IAIProvider> _providers;
    private readonly ILogger<AIProviderFactory> _logger;

    public AIProviderFactory(
        IEnumerable<IAIProvider> providers,
        ILogger<AIProviderFactory> logger)
    {
        _providers = providers;
        _logger = logger;
    }

    /// <summary>
    /// 根据Provider名称获取实例
    /// </summary>
    public IAIProvider? GetProvider(string providerName)
    {
        return _providers.FirstOrDefault(p =>
            p.ProviderName.Equals(providerName, StringComparison.OrdinalIgnoreCase));
    }

    /// <summary>
    /// 获取可用的Provider列表
    /// </summary>
    public List<string> GetAvailableProviders()
    {
        return _providers.Select(p => p.ProviderName).ToList();
    }
}
