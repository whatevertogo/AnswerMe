using AnswerMe.Application.AI;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Application.Services;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Domain.Models;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace AnswerMe.UnitTests.Services;

public class AIGenerationServiceTests
{
    [Fact]
    public async Task ExecuteTaskAsync_ShouldReportPartialQuestionsDuringProcessing()
    {
        // Arrange
        var userId = 1;
        var taskId = "task-1";
        var nextQuestionId = 100;
        var snapshots = new List<ProgressSnapshot>();

        var questionRepository = new Mock<IQuestionRepository>();
        var questionBankRepository = new Mock<IQuestionBankRepository>();
        var dataSourceRepository = new Mock<IDataSourceRepository>();
        var dataSourceService = new Mock<IDataSourceService>();
        var taskQueue = new Mock<IAIGenerationTaskQueue>();
        var progressStore = new Mock<IAIGenerationProgressStore>();
        var provider = new Mock<IAIProvider>();

        provider.SetupGet(p => p.ProviderName).Returns("deepseek");
        provider
            .SetupSequence(p => p.GenerateQuestionsAsync(
                It.IsAny<string>(),
                It.IsAny<AIQuestionGenerateRequest>(),
                It.IsAny<string?>(),
                It.IsAny<string?>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateAiResponse("题目 1", "题目 2"))
            .ReturnsAsync(CreateAiResponse("题目 3"));

        var providerFactory = new AIProviderFactory(
            new[] { provider.Object },
            Mock.Of<ILogger<AIProviderFactory>>());

        questionBankRepository
            .Setup(repository => repository.GetByIdAsync(5, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new QuestionBank
            {
                Id = 5,
                UserId = userId,
                Name = "目标题库",
                Description = "desc"
            });

        dataSourceRepository
            .Setup(repository => repository.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<DataSource>
            {
                new()
                {
                    Id = 10,
                    UserId = userId,
                    Name = "Deepseek",
                    Type = "deepseek",
                    Config = "{}",
                    IsDefault = true
                }
            });

        dataSourceService
            .Setup(service => service.GetDecryptedConfigAsync(10, userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DataSourceConfigDto
            {
                ApiKey = "test-key",
                Model = "test-model"
            });

        questionRepository
            .Setup(repository => repository.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question question, CancellationToken _) =>
            {
                question.Id = nextQuestionId++;
                return question;
            });

        questionRepository
            .Setup(repository => repository.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        progressStore
            .Setup(store => store.UpdateAsync(taskId, It.IsAny<Action<AIGenerateProgressDto>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var service = new AIGenerationService(
            questionRepository.Object,
            questionBankRepository.Object,
            dataSourceRepository.Object,
            dataSourceService.Object,
            providerFactory,
            Mock.Of<ILogger<AIGenerationService>>(),
            taskQueue.Object,
            progressStore.Object,
            Options.Create(new AIGenerationOptions
            {
                BatchSize = 2,
                MaxSyncCount = 20
            }));

        var request = new AIGenerateRequestDto
        {
            Subject = "Vue 3",
            Count = 3,
            Difficulty = "medium",
            Language = "zh-CN",
            ProviderName = "deepseek",
            QuestionBankId = 5,
            QuestionTypes = new List<QuestionType> { QuestionType.SingleChoice }
        };

        // Act
        await service.ExecuteTaskAsync(
            taskId,
            userId,
            request,
            (reportedTaskId, generatedCount, totalCount, status, questions) =>
            {
                snapshots.Add(new ProgressSnapshot(
                    reportedTaskId,
                    generatedCount,
                    totalCount,
                    status,
                    questions?.Select(question => question.QuestionText).ToList() ?? new List<string>()));
                return Task.CompletedTask;
            });

        // Assert
        snapshots.Select(snapshot => snapshot.GeneratedCount).Should().ContainInOrder(1, 2, 3);
        snapshots[0].Questions.Should().ContainSingle().Which.Should().Be("题目 1");
        snapshots[1].Questions.Should().ContainInOrder("题目 1", "题目 2");
        snapshots[2].Questions.Should().ContainInOrder("题目 1", "题目 2", "题目 3");
        snapshots.All(snapshot => snapshot.Status == "processing").Should().BeTrue();
    }

    private static AIQuestionGenerateResponse CreateAiResponse(params string[] questionTexts)
    {
        return new AIQuestionGenerateResponse
        {
            Success = true,
            Questions = questionTexts.Select(questionText => new GeneratedQuestion
            {
                QuestionTypeEnum = QuestionType.SingleChoice,
                QuestionText = questionText,
                Difficulty = "medium",
                Data = new ChoiceQuestionData
                {
                    Options = new List<string> { "A. 选项1", "B. 选项2" },
                    CorrectAnswers = new List<string> { "A" },
                    Difficulty = "medium",
                    Explanation = "解释"
                },
                Explanation = "解释"
            }).ToList()
        };
    }

    private sealed record ProgressSnapshot(
        string TaskId,
        int GeneratedCount,
        int TotalCount,
        string Status,
        List<string> Questions);
}
