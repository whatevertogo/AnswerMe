using AnswerMe.Application.Services;
using AnswerMe.Domain.Entities;
using AnswerMe.Infrastructure.Data;
using FluentAssertions;
using NetArchTest.Rules;

namespace AnswerMe.UnitTests.Architecture;

public class ArchitectureRulesTests
{
    [Fact]
    public void Domain_Should_Not_Depend_On_Other_Layers()
    {
        var assembly = typeof(User).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny("AnswerMe.Application", "AnswerMe.Infrastructure", "AnswerMe.API")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is { Count: > 0 }
            ? $"Failing types: {string.Join(", ", result.FailingTypeNames)}"
            : null);
    }

    [Fact]
    public void Application_Should_Not_Depend_On_Infrastructure_Or_Api()
    {
        var assembly = typeof(AuthService).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny("AnswerMe.Infrastructure", "AnswerMe.API")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is { Count: > 0 }
            ? $"Failing types: {string.Join(", ", result.FailingTypeNames)}"
            : null);
    }

    [Fact]
    public void Infrastructure_Should_Not_Depend_On_Api()
    {
        var assembly = typeof(AnswerMeDbContext).Assembly;

        var result = Types.InAssembly(assembly)
            .ShouldNot()
            .HaveDependencyOnAny("AnswerMe.API")
            .GetResult();

        result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is { Count: > 0 }
            ? $"Failing types: {string.Join(", ", result.FailingTypeNames)}"
            : null);
    }

    [Fact]
    public void Api_Should_Not_Be_Dependent_On_By_Other_Layers()
    {
        var otherAssemblies = new[]
        {
            typeof(User).Assembly,
            typeof(AuthService).Assembly,
            typeof(AnswerMeDbContext).Assembly
        };

        foreach (var other in otherAssemblies)
        {
            var result = Types.InAssembly(other)
                .ShouldNot()
                .HaveDependencyOnAny("AnswerMe.API")
                .GetResult();

            result.IsSuccessful.Should().BeTrue(result.FailingTypeNames is { Count: > 0 }
                ? $"Assembly {other.GetName().Name} failing types: {string.Join(", ", result.FailingTypeNames)}"
                : null);
        }
    }
}
