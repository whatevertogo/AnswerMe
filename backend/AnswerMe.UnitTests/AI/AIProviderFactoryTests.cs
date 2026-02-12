using AnswerMe.Application;
using AnswerMe.Application.AI;
using AnswerMe.Infrastructure;
using FluentAssertions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AnswerMe.UnitTests.AI;

public class AIProviderFactoryTests
{
    [Fact]
    public void GetProvider_Should_Return_Registered_Provider()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ConnectionStrings:DefaultConnection"] = "Data Source=:memory:"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddInfrastructure(configuration);
        services.AddApplication();

        using var serviceProvider = services.BuildServiceProvider();
        var factory = serviceProvider.GetRequiredService<AIProviderFactory>();

        factory.GetProvider("OpenAI").Should().NotBeNull();
        factory.GetProvider("Qwen").Should().NotBeNull();
        factory.GetProvider("Zhipu").Should().NotBeNull();
        factory.GetProvider("anthropic").Should().NotBeNull();
        factory.GetProvider("Minimax").Should().NotBeNull();
    }
}
