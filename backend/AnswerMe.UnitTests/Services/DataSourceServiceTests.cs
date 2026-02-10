using System.Text.Json;
using AnswerMe.Application.AI;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Services;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace AnswerMe.UnitTests.Services;

/// <summary>
/// DataSourceService 单元测试
/// </summary>
public class DataSourceServiceTests
{
    private static DataSourceService CreateService(
        Mock<IDataSourceRepository> repo,
        IDataProtectionProvider protectionProvider)
    {
        var providers = Array.Empty<IAIProvider>();
        var factoryLogger = new Mock<ILogger<AIProviderFactory>>();
        var factory = new AIProviderFactory(providers, factoryLogger.Object);
        var logger = new Mock<ILogger<DataSourceService>>();

        return new DataSourceService(repo.Object, protectionProvider, factory);
    }

    private static string BuildConfigJson(string encryptedApiKey, string? endpoint = null, string? model = null)
    {
        var config = new
        {
            ApiKey = encryptedApiKey,
            Endpoint = endpoint,
            Model = model
        };

        return JsonSerializer.Serialize(config);
    }

    [Fact]
    public async Task UpdateAsync_WithTypeChange_ShouldUpdateProviderType()
    {
        var protectionProvider = DataProtectionProvider.Create("AnswerMe.UnitTests");
        var protector = protectionProvider.CreateProtector("DataSourceApiKeys");

        var encrypted = protector.Protect("sk-test-1234567890");
        var dataSource = new DataSource
        {
            Id = 1,
            UserId = 99,
            Name = "Test",
            Type = "openai",
            Config = BuildConfigJson(encrypted),
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var repo = new Mock<IDataSourceRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSource);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repo, protectionProvider);

        var dto = new UpdateDataSourceDto
        {
            Type = "Qwen"
        };

        var result = await service.UpdateAsync(1, 99, dto);

        result.Should().NotBeNull();
        result!.Type.Should().Be("qwen");
        dataSource.Type.Should().Be("qwen");
        repo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task GetByIdAsync_ShouldMaskDecryptedApiKey()
    {
        var protectionProvider = DataProtectionProvider.Create("AnswerMe.UnitTests");
        var protector = protectionProvider.CreateProtector("DataSourceApiKeys");

        var plainApiKey = "sk-1234567890abcd";
        var encrypted = protector.Protect(plainApiKey);
        var dataSource = new DataSource
        {
            Id = 1,
            UserId = 42,
            Name = "Test",
            Type = "openai",
            Config = BuildConfigJson(encrypted),
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var repo = new Mock<IDataSourceRepository>();
        repo.Setup(r => r.GetByIdAsync(1, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSource);

        var service = CreateService(repo, protectionProvider);

        var result = await service.GetByIdAsync(1, 42);

        var expectedMask = $"{plainApiKey.Substring(0, 4)}...{plainApiKey.Substring(plainApiKey.Length - 4)}";
        result.Should().NotBeNull();
        result!.MaskedApiKey.Should().Be(expectedMask);
    }

    [Fact]
    public async Task UpdateAsync_WithEmptyApiKey_ShouldKeepExistingApiKey()
    {
        var protectionProvider = DataProtectionProvider.Create("AnswerMe.UnitTests");
        var protector = protectionProvider.CreateProtector("DataSourceApiKeys");

        var plainApiKey = "sk-original-123456";
        var encrypted = protector.Protect(plainApiKey);
        var dataSource = new DataSource
        {
            Id = 7,
            UserId = 7,
            Name = "Test",
            Type = "openai",
            Config = BuildConfigJson(encrypted),
            IsDefault = false,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        var repo = new Mock<IDataSourceRepository>();
        repo.Setup(r => r.GetByIdAsync(7, It.IsAny<CancellationToken>()))
            .ReturnsAsync(dataSource);
        repo.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = CreateService(repo, protectionProvider);

        var dto = new UpdateDataSourceDto
        {
            ApiKey = ""
        };

        var result = await service.UpdateAsync(7, 7, dto);

        result.Should().NotBeNull();

        var config = JsonSerializer.Deserialize<Dictionary<string, string?>>(dataSource.Config);
        config.Should().NotBeNull();
        var encryptedAfter = config!["ApiKey"];
        encryptedAfter.Should().NotBeNull();
        protector.Unprotect(encryptedAfter!).Should().Be(plainApiKey);
    }
}
