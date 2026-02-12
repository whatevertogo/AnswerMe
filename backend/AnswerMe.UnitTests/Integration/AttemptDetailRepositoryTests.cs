using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace AnswerMe.UnitTests.Integration;

public class AttemptDetailRepositoryTests : TestBase
{
    [Fact]
    public async Task GetWeeklyTrendAsync_ShouldReturnExactlyTwelveWeeksAndFillEmptyWeeks()
    {
        // Arrange
        var repository = ServiceProvider.GetRequiredService<IAttemptDetailRepository>();
        var user = await CreateTestUserAsync();
        var bank = await CreateTestQuestionBankAsync(user.Id);
        var question1 = await CreateTestQuestionAsync(bank.Id, QuestionType.SingleChoice);
        var question2 = await CreateTestQuestionAsync(bank.Id, QuestionType.TrueFalse);
        var question3 = await CreateTestQuestionAsync(bank.Id, QuestionType.FillBlank);
        var now = DateTime.UtcNow;
        var currentWeekStart = GetWeekStart(now);
        var firstWeekStart = currentWeekStart.AddDays(-11 * 7);

        var outOfWindowAttempt = new Attempt
        {
            UserId = user.Id,
            QuestionBankId = bank.Id,
            StartedAt = firstWeekStart.AddDays(-1),
            CompletedAt = firstWeekStart.AddDays(-1).AddMinutes(10),
            TotalQuestions = 1
        };

        var firstWeekAttempt = new Attempt
        {
            UserId = user.Id,
            QuestionBankId = bank.Id,
            StartedAt = firstWeekStart.AddDays(1),
            CompletedAt = firstWeekStart.AddDays(1).AddMinutes(10),
            TotalQuestions = 2
        };

        var currentWeekAttempt = new Attempt
        {
            UserId = user.Id,
            QuestionBankId = bank.Id,
            StartedAt = now.AddHours(-2),
            CompletedAt = now.AddHours(-1),
            TotalQuestions = 1
        };

        var currentWeekAttemptWithoutDetails = new Attempt
        {
            UserId = user.Id,
            QuestionBankId = bank.Id,
            StartedAt = now.AddHours(-1),
            CompletedAt = now,
            TotalQuestions = 1
        };

        DbContext.Attempts.AddRange(
            outOfWindowAttempt,
            firstWeekAttempt,
            currentWeekAttempt,
            currentWeekAttemptWithoutDetails);
        await DbContext.SaveChangesAsync();

        DbContext.AttemptDetails.AddRange(
            new AttemptDetail
            {
                AttemptId = outOfWindowAttempt.Id,
                QuestionId = question1.Id,
                IsCorrect = true
            },
            new AttemptDetail
            {
                AttemptId = firstWeekAttempt.Id,
                QuestionId = question1.Id,
                IsCorrect = true
            },
            new AttemptDetail
            {
                AttemptId = firstWeekAttempt.Id,
                QuestionId = question2.Id,
                IsCorrect = false
            },
            new AttemptDetail
            {
                AttemptId = currentWeekAttempt.Id,
                QuestionId = question3.Id,
                IsCorrect = true
            });
        await DbContext.SaveChangesAsync();

        // Act
        var result = await repository.GetWeeklyTrendAsync(user.Id, 12);

        // Assert
        result.Should().HaveCount(12);
        result.First().weekStart.Should().Be(firstWeekStart);
        result.Last().weekStart.Should().Be(currentWeekStart);
        result.Count(x => x.attemptCount == 0).Should().BeGreaterThan(0);

        for (var i = 1; i < result.Count; i++)
        {
            result[i].weekStart.Should().Be(result[i - 1].weekStart.AddDays(7));
        }

        var firstWeekStats = result.First(x => x.weekStart == firstWeekStart);
        firstWeekStats.attemptCount.Should().Be(1);
        firstWeekStats.questionCount.Should().Be(2);
        firstWeekStats.correctCount.Should().Be(1);

        var currentWeekStats = result.First(x => x.weekStart == currentWeekStart);
        currentWeekStats.attemptCount.Should().Be(2);
        currentWeekStats.questionCount.Should().Be(1);
        currentWeekStats.correctCount.Should().Be(1);

        result.Sum(x => x.attemptCount).Should().Be(3);
    }

    private static DateTime GetWeekStart(DateTime date)
    {
        var diff = (7 + (date.DayOfWeek - DayOfWeek.Monday)) % 7;
        return date.AddDays(-diff).Date;
    }
}
