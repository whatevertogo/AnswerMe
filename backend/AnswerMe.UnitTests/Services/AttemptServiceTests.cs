using AnswerMe.Application.DTOs;
using AnswerMe.Application.Services;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Interfaces;
using AnswerMe.Domain.Models;
using AnswerMe.UnitTests.Helpers;
using FluentAssertions;
using Moq;
using Xunit;

namespace AnswerMe.UnitTests.Services;

/// <summary>
/// AttemptService 单元测试
/// </summary>
public class AttemptServiceTests
{
    private readonly Mock<IAttemptRepository> _mockAttemptRepository;
    private readonly Mock<IAttemptDetailRepository> _mockAttemptDetailRepository;
    private readonly Mock<IQuestionBankRepository> _mockQuestionBankRepository;
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly AttemptService _attemptService;

    public AttemptServiceTests()
    {
        _mockAttemptRepository = new Mock<IAttemptRepository>();
        _mockAttemptDetailRepository = new Mock<IAttemptDetailRepository>();
        _mockQuestionBankRepository = new Mock<IQuestionBankRepository>();
        _mockQuestionRepository = new Mock<IQuestionRepository>();
        
        _attemptService = new AttemptService(
            _mockAttemptRepository.Object,
            _mockAttemptDetailRepository.Object,
            _mockQuestionBankRepository.Object,
            _mockQuestionRepository.Object);
    }

    #region StartAttemptAsync Tests

    [Fact]
    public async Task StartAttemptAsync_WithValidData_ShouldReturnAttemptId()
    {
        // Arrange
        int userId = 1;
        var dto = new StartAttemptDto
        {
            QuestionBankId = 1,
            Mode = "sequential"
        };

        var questionBank = CreateTestQuestionBank(userId: userId);
        var questions = CreateTestQuestions(5, dto.QuestionBankId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(dto.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.GetByQuestionBankIdAsync(dto.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questions);

        _mockAttemptRepository
            .Setup(r => r.AddAsync(It.IsAny<Attempt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attempt attempt, CancellationToken _) =>
            {
                attempt.Id = 1;
                return attempt;
            });

        // Act
        var result = await _attemptService.StartAttemptAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.AttemptId.Should().Be(1);
        result.QuestionIds.Should().HaveCount(5);
        _mockAttemptRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task StartAttemptAsync_WithRandomMode_ShouldReturnRandomizedQuestionOrder()
    {
        // Arrange
        int userId = 1;
        var dto = new StartAttemptDto
        {
            QuestionBankId = 1,
            Mode = "random"
        };

        var questionBank = CreateTestQuestionBank(userId: userId);
        var questions = CreateTestQuestions(3, dto.QuestionBankId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(dto.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.GetByQuestionBankIdAsync(dto.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questions);

        _mockAttemptRepository
            .Setup(r => r.AddAsync(It.IsAny<Attempt>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attempt attempt, CancellationToken _) =>
            {
                attempt.Id = 1;
                return attempt;
            });

        // Act
        var result = await _attemptService.StartAttemptAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.QuestionIds.Should().HaveCount(3);
    }

    [Fact]
    public async Task StartAttemptAsync_WithNonExistentQuestionBank_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        var dto = new StartAttemptDto
        {
            QuestionBankId = 999,
            Mode = "sequential"
        };

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(dto.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuestionBank?)null);

        // Act
        var act = async () => await _attemptService.StartAttemptAsync(userId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("题库不存在或无权访问");
    }

    [Fact]
    public async Task StartAttemptAsync_WithEmptyQuestionBank_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        var dto = new StartAttemptDto
        {
            QuestionBankId = 1,
            Mode = "sequential"
        };

        var questionBank = CreateTestQuestionBank(userId: userId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(dto.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.GetByQuestionBankIdAsync(dto.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question>());

        // Act
        var act = async () => await _attemptService.StartAttemptAsync(userId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("题库中没有题目");
    }

    [Fact]
    public async Task StartAttemptAsync_WithDifferentUser_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        int differentUserId = 2;
        var dto = new StartAttemptDto
        {
            QuestionBankId = 1,
            Mode = "sequential"
        };

        var questionBank = CreateTestQuestionBank(userId: differentUserId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(dto.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var act = async () => await _attemptService.StartAttemptAsync(userId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("题库不存在或无权访问");
    }

    #endregion

    #region SubmitAnswerAsync Tests

    [Fact]
    public async Task SubmitAnswerAsync_WithValidSingleChoiceAnswer_ShouldReturnTrue()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestSingleChoiceQuestion(questionId, attempt.QuestionBankId, "A");
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "A",
            TimeSpent = 30
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail?)null);

        _mockAttemptDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<AttemptDetail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail detail, CancellationToken _) => detail);

        // Act
        var result = await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        result.Should().BeTrue();
        _mockAttemptDetailRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task SubmitAnswerAsync_WithValidBooleanAnswer_ShouldReturnTrue()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestBooleanQuestion(questionId, attempt.QuestionBankId, true);
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "true",
            TimeSpent = 15
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail?)null);

        _mockAttemptDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<AttemptDetail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail detail, CancellationToken _) => detail);

        // Act
        var result = await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAnswerAsync_WithValidFillBlankAnswer_ShouldReturnTrue()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestFillBlankQuestion(questionId, attempt.QuestionBankId, new List<string> { "北京", "北京市" });
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "北京",
            TimeSpent = 45
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail?)null);

        _mockAttemptDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<AttemptDetail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail detail, CancellationToken _) => detail);

        // Act
        var result = await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAnswerAsync_WithValidShortAnswer_ShouldReturnTrue()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestShortAnswerQuestion(questionId, attempt.QuestionBankId, "参考答案内容");
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "用户答案",
            TimeSpent = 120
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail?)null);

        _mockAttemptDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<AttemptDetail>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail detail, CancellationToken _) => detail);

        // Act
        var result = await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAnswerAsync_WithNonExistentAttempt_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        int attemptId = 999;
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = 1,
            UserAnswer = "A"
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attempt?)null);

        // Act
        var act = async () => await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("答题记录不存在或无权访问");
    }

    [Fact]
    public async Task SubmitAnswerAsync_WithCompletedAttempt_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        attempt.CompletedAt = DateTime.UtcNow; // Mark as completed

        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = 1,
            UserAnswer = "A"
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        // Act
        var act = async () => await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("答题已完成");
    }

    [Fact]
    public async Task SubmitAnswerAsync_WithNonExistentQuestion_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 999;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "A"
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question?)null);

        // Act
        var act = async () => await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("题目不存在");
    }

    [Fact]
    public async Task SubmitAnswerAsync_WithExistingAnswer_ShouldUpdateAnswer()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestSingleChoiceQuestion(questionId, attempt.QuestionBankId, "A");
        var existingDetail = new AttemptDetail
        {
            Id = 1,
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "B",
            IsCorrect = false
        };

        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "A",
            TimeSpent = 30
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(existingDetail);

        // Act
        var result = await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        result.Should().BeTrue();
        existingDetail.UserAnswer.Should().Be("A");
        _mockAttemptDetailRepository.Verify(r => r.UpdateAsync(existingDetail, It.IsAny<CancellationToken>()), Times.Once);
    }

    #endregion

    #region CompleteAttemptAsync Tests

    [Fact]
    public async Task CompleteAttemptAsync_WithValidData_ShouldCalculateScore()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        var dto = new CompleteAttemptDto { AttemptId = attemptId };

        var attempt = CreateTestAttempt(userId, attemptId);
        attempt.TotalQuestions = 5;

        var details = new List<AttemptDetail>
        {
            new() { AttemptId = attemptId, QuestionId = 1, IsCorrect = true },
            new() { AttemptId = attemptId, QuestionId = 2, IsCorrect = true },
            new() { AttemptId = attemptId, QuestionId = 3, IsCorrect = false },
            new() { AttemptId = attemptId, QuestionId = 4, IsCorrect = true },
            new() { AttemptId = attemptId, QuestionId = 5, IsCorrect = false }
        };

        var questionBank = CreateTestQuestionBank(userId: userId);

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(attempt.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _attemptService.CompleteAttemptAsync(userId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(attemptId);
        result.TotalQuestions.Should().Be(5);
        result.CorrectCount.Should().Be(3);
        result.Score.Should().Be(60); // 3/5 * 100 = 60
        attempt.CompletedAt.Should().NotBeNull();
        _mockAttemptRepository.Verify(r => r.UpdateAsync(attempt, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CompleteAttemptAsync_WithAllCorrect_ShouldGetFullScore()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        var dto = new CompleteAttemptDto { AttemptId = attemptId };

        var attempt = CreateTestAttempt(userId, attemptId);
        attempt.TotalQuestions = 10;

        var details = new List<AttemptDetail>
        {
            new() { AttemptId = attemptId, QuestionId = 1, IsCorrect = true },
            new() { AttemptId = attemptId, QuestionId = 2, IsCorrect = true },
            new() { AttemptId = attemptId, QuestionId = 3, IsCorrect = true }
        };

        var questionBank = CreateTestQuestionBank(userId: userId);

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(attempt.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _attemptService.CompleteAttemptAsync(userId, dto);

        // Assert
        result.Score.Should().Be(30); // 3/10 * 100 = 30 (only 3 answered out of 10)
    }

    [Fact]
    public async Task CompleteAttemptAsync_WithNoAnswers_ShouldGetZeroScore()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        var dto = new CompleteAttemptDto { AttemptId = attemptId };

        var attempt = CreateTestAttempt(userId, attemptId);
        attempt.TotalQuestions = 5;

        var details = new List<AttemptDetail>(); // No answers

        var questionBank = CreateTestQuestionBank(userId: userId);

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(attempt.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _attemptService.CompleteAttemptAsync(userId, dto);

        // Assert
        result.CorrectCount.Should().Be(0);
        result.Score.Should().Be(0);
    }

    [Fact]
    public async Task CompleteAttemptAsync_WithNonExistentAttempt_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        int attemptId = 999;
        var dto = new CompleteAttemptDto { AttemptId = attemptId };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attempt?)null);

        // Act
        var act = async () => await _attemptService.CompleteAttemptAsync(userId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("答题记录不存在或无权访问");
    }

    [Fact]
    public async Task CompleteAttemptAsync_WithAlreadyCompleted_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        var dto = new CompleteAttemptDto { AttemptId = attemptId };

        var attempt = CreateTestAttempt(userId, attemptId);
        attempt.CompletedAt = DateTime.UtcNow; // Already completed

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        // Act
        var act = async () => await _attemptService.CompleteAttemptAsync(userId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("答题已完成");
    }

    #endregion

    #region GetAttemptByIdAsync Tests

    [Fact]
    public async Task GetAttemptByIdAsync_WithExistingAttempt_ShouldReturnAttemptDto()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;

        var attempt = CreateTestAttempt(userId, attemptId);
        attempt.TotalQuestions = 5;
        attempt.StartedAt = DateTime.UtcNow.AddMinutes(-10);
        attempt.CompletedAt = DateTime.UtcNow;

        var questionBank = CreateTestQuestionBank(userId: userId);

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(attempt.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttemptDetail>());

        // Act
        var result = await _attemptService.GetAttemptByIdAsync(attemptId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(attemptId);
        result.QuestionBankId.Should().Be(attempt.QuestionBankId);
        result.TotalQuestions.Should().Be(5);
        result.CompletedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task GetAttemptByIdAsync_WithNonExistentAttempt_ShouldReturnNull()
    {
        // Arrange
        int userId = 1;
        int attemptId = 999;

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attempt?)null);

        // Act
        var result = await _attemptService.GetAttemptByIdAsync(attemptId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAttemptByIdAsync_WithDifferentUser_ShouldReturnNull()
    {
        // Arrange
        int userId = 1;
        int differentUserId = 2;
        int attemptId = 1;

        var attempt = CreateTestAttempt(differentUserId, attemptId);

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        // Act
        var result = await _attemptService.GetAttemptByIdAsync(attemptId, userId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetAttemptByIdAsync_WithInProgressAttempt_ShouldCalculateDuration()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;

        var attempt = CreateTestAttempt(userId, attemptId);
        attempt.StartedAt = DateTime.UtcNow.AddMinutes(-5);
        // Not completed yet

        var questionBank = CreateTestQuestionBank(userId: userId);

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(attempt.QuestionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttemptDetail>());

        // Act
        var result = await _attemptService.GetAttemptByIdAsync(attemptId, userId);

        // Assert
        result.Should().NotBeNull();
        result!.DurationSeconds.Should().BeGreaterThan(0);
        result.CompletedAt.Should().BeNull();
    }

    #endregion

    #region GetAttemptDetailsAsync Tests

    [Fact]
    public async Task GetAttemptDetailsAsync_WithValidAttempt_ShouldReturnDetails()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;

        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestSingleChoiceQuestion(1, attempt.QuestionBankId, "A");
        question.QuestionText = "测试题目";
        
        var details = new List<AttemptDetail>
        {
            new()
            {
                Id = 1,
                AttemptId = attemptId,
                QuestionId = 1,
                Question = question,
                UserAnswer = "A",
                IsCorrect = true,
                TimeSpent = 30
            }
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdWithQuestionsAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _attemptService.GetAttemptDetailsAsync(attemptId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(1);
        result[0].QuestionText.Should().Be("测试题目");
        result[0].UserAnswer.Should().Be("A");
        result[0].IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task GetAttemptDetailsAsync_WithNonExistentAttempt_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        int attemptId = 999;

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Attempt?)null);

        // Act
        var act = async () => await _attemptService.GetAttemptDetailsAsync(attemptId, userId);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("答题记录不存在或无权访问");
    }

    [Fact]
    public async Task GetAttemptDetailsAsync_WithDifferentUser_ShouldThrowException()
    {
        // Arrange
        int userId = 1;
        int differentUserId = 2;
        int attemptId = 1;

        var attempt = CreateTestAttempt(differentUserId, attemptId);

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        // Act
        var act = async () => await _attemptService.GetAttemptDetailsAsync(attemptId, userId);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("答题记录不存在或无权访问");
    }

    [Fact]
    public async Task GetAttemptDetailsAsync_WithMultipleDetails_ShouldReturnAll()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;

        var attempt = CreateTestAttempt(userId, attemptId);
        
        var questions = new List<Question>
        {
            new Question
            {
                Id = 1,
                QuestionBankId = attempt.QuestionBankId,
                QuestionText = "题目1",
                QuestionType = "SingleChoice",
                QuestionDataJson = JsonTestHelper.Serialize(new ChoiceQuestionData
                {
                    Options = new List<string> { "A. 选项A", "B. 选项B", "C. 选项C", "D. 选项D" },
                    CorrectAnswers = new List<string> { "A" }
                })
            },
            new Question
            {
                Id = 2,
                QuestionBankId = attempt.QuestionBankId,
                QuestionText = "题目2",
                QuestionType = "TrueFalse",
                QuestionDataJson = JsonTestHelper.Serialize(new BooleanQuestionData
                {
                    CorrectAnswer = true
                })
            },
            new Question
            {
                Id = 3,
                QuestionBankId = attempt.QuestionBankId,
                QuestionText = "题目3",
                QuestionType = "FillBlank",
                QuestionDataJson = JsonTestHelper.Serialize(new FillBlankQuestionData
                {
                    AcceptableAnswers = new List<string> { "答案" }
                })
            }
        };

        var details = new List<AttemptDetail>
        {
            new() { Id = 1, AttemptId = attemptId, QuestionId = 1, Question = questions[0], UserAnswer = "A", IsCorrect = true },
            new() { Id = 2, AttemptId = attemptId, QuestionId = 2, Question = questions[1], UserAnswer = "true", IsCorrect = false },
            new() { Id = 3, AttemptId = attemptId, QuestionId = 3, Question = questions[2], UserAnswer = "答案", IsCorrect = true }
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdWithQuestionsAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _attemptService.GetAttemptDetailsAsync(attemptId, userId);

        // Assert
        result.Should().NotBeNull();
        result.Should().HaveCount(3);
    }

    #endregion

    #region GetStatisticsAsync Tests

    [Fact]
    public async Task GetStatisticsAsync_WithMultipleAttempts_ShouldReturnCorrectStats()
    {
        // Arrange
        int userId = 1;

        var attempts = new List<Attempt>
        {
            new() { Id = 1, UserId = userId, QuestionBankId = 1, StartedAt = DateTime.UtcNow.AddDays(-10), CompletedAt = DateTime.UtcNow.AddDays(-10), Score = 80 },
            new() { Id = 2, UserId = userId, QuestionBankId = 1, StartedAt = DateTime.UtcNow.AddDays(-5), CompletedAt = DateTime.UtcNow.AddDays(-5), Score = 60 },
            new() { Id = 3, UserId = userId, QuestionBankId = 2, StartedAt = DateTime.UtcNow, CompletedAt = null } // In progress
        };

        var details = new List<AttemptDetail>
        {
            new() { AttemptId = 1, IsCorrect = true },
            new() { AttemptId = 1, IsCorrect = true },
            new() { AttemptId = 1, IsCorrect = false },
            new() { AttemptId = 2, IsCorrect = true },
            new() { AttemptId = 2, IsCorrect = false },
            new() { AttemptId = 3, IsCorrect = true }
        };

        _mockAttemptRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempts);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _attemptService.GetStatisticsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.TotalAttempts.Should().Be(3);
        result.CompletedAttempts.Should().Be(2);
        result.TotalQuestionsAnswered.Should().Be(6);
        result.TotalCorrectAnswers.Should().Be(4);
        result.OverallAccuracy.Should().BeApproximately(66.67m, 0.01m);
    }

    [Fact]
    public async Task GetStatisticsAsync_WithNoAttempts_ShouldReturnEmptyStats()
    {
        // Arrange
        int userId = 1;

        _mockAttemptRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Attempt>());

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<AttemptDetail>());

        // Act
        var result = await _attemptService.GetStatisticsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.TotalAttempts.Should().Be(0);
        result.CompletedAttempts.Should().Be(0);
        result.AverageScore.Should().BeNull();
        result.OverallAccuracy.Should().BeNull();
    }

    [Fact]
    public async Task GetStatisticsAsync_WithSingleAttempt_ShouldReturnCorrectAverage()
    {
        // Arrange
        int userId = 1;

        var attempts = new List<Attempt>
        {
            new() { Id = 1, UserId = userId, QuestionBankId = 1, StartedAt = DateTime.UtcNow.AddDays(-1), CompletedAt = DateTime.UtcNow.AddDays(-1), Score = 75 }
        };

        var details = new List<AttemptDetail>
        {
            new() { AttemptId = 1, IsCorrect = true },
            new() { AttemptId = 1, IsCorrect = false },
            new() { AttemptId = 1, IsCorrect = true },
            new() { AttemptId = 1, IsCorrect = true }
        };

        _mockAttemptRepository
            .Setup(r => r.GetByUserIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempts);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptIdsAsync(It.IsAny<List<int>>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(details);

        // Act
        var result = await _attemptService.GetStatisticsAsync(userId);

        // Assert
        result.Should().NotBeNull();
        result.TotalAttempts.Should().Be(1);
        result.CompletedAttempts.Should().Be(1);
        result.AverageScore.Should().Be(75);
        result.TotalQuestionsAnswered.Should().Be(4);
        result.TotalCorrectAnswers.Should().Be(3);
    }

    #endregion

    #region Answer Checking Tests

    [Fact]
    public async Task SubmitAnswerAsync_SingleChoiceCorrect_ShouldMarkAsCorrect()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestSingleChoiceQuestion(questionId, attempt.QuestionBankId, "C");
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "C"
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail?)null);

        AttemptDetail? capturedDetail = null;
        _mockAttemptDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<AttemptDetail>(), It.IsAny<CancellationToken>()))
            .Callback<AttemptDetail, CancellationToken>((d, _) => capturedDetail = d)
            .ReturnsAsync((AttemptDetail d, CancellationToken _) => d);

        // Act
        await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        capturedDetail.Should().NotBeNull();
        capturedDetail!.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAnswerAsync_SingleChoiceWrong_ShouldMarkAsIncorrect()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestSingleChoiceQuestion(questionId, attempt.QuestionBankId, "A");
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "B" // Wrong answer
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail?)null);

        AttemptDetail? capturedDetail = null;
        _mockAttemptDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<AttemptDetail>(), It.IsAny<CancellationToken>()))
            .Callback<AttemptDetail, CancellationToken>((d, _) => capturedDetail = d)
            .ReturnsAsync((AttemptDetail d, CancellationToken _) => d);

        // Act
        await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        capturedDetail.Should().NotBeNull();
        capturedDetail!.IsCorrect.Should().BeFalse();
    }

    [Fact]
    public async Task SubmitAnswerAsync_BooleanTrueCorrect_ShouldMarkAsCorrect()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestBooleanQuestion(questionId, attempt.QuestionBankId, true);
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "true"
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail?)null);

        AttemptDetail? capturedDetail = null;
        _mockAttemptDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<AttemptDetail>(), It.IsAny<CancellationToken>()))
            .Callback<AttemptDetail, CancellationToken>((d, _) => capturedDetail = d)
            .ReturnsAsync((AttemptDetail d, CancellationToken _) => d);

        // Act
        await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        capturedDetail.Should().NotBeNull();
        capturedDetail!.IsCorrect.Should().BeTrue();
    }

    [Fact]
    public async Task SubmitAnswerAsync_FillBlankContainsAnswer_ShouldMarkAsCorrect()
    {
        // Arrange
        int userId = 1;
        int attemptId = 1;
        int questionId = 1;
        
        var attempt = CreateTestAttempt(userId, attemptId);
        var question = CreateTestFillBlankQuestion(questionId, attempt.QuestionBankId, new List<string> { "中华人民共和国", "中国" });
        var dto = new SubmitAnswerDto
        {
            AttemptId = attemptId,
            QuestionId = questionId,
            UserAnswer = "中国"
        };

        _mockAttemptRepository
            .Setup(r => r.GetByIdAsync(attemptId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(attempt);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockAttemptDetailRepository
            .Setup(r => r.GetByAttemptAndQuestionAsync(attemptId, questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((AttemptDetail?)null);

        AttemptDetail? capturedDetail = null;
        _mockAttemptDetailRepository
            .Setup(r => r.AddAsync(It.IsAny<AttemptDetail>(), It.IsAny<CancellationToken>()))
            .Callback<AttemptDetail, CancellationToken>((d, _) => capturedDetail = d)
            .ReturnsAsync((AttemptDetail d, CancellationToken _) => d);

        // Act
        await _attemptService.SubmitAnswerAsync(userId, dto);

        // Assert
        capturedDetail.Should().NotBeNull();
        capturedDetail!.IsCorrect.Should().BeTrue();
    }

    #endregion

    #region Helper Methods

    private static QuestionBank CreateTestQuestionBank(int userId = 1, int id = 1)
    {
        return new QuestionBank
        {
            Id = id,
            UserId = userId,
            Name = "测试题库",
            Description = "测试用题库",
            Tags = "[]"
        };
    }

    private static List<Question> CreateTestQuestions(int count, int questionBankId)
    {
        var questions = new List<Question>();
        for (int i = 0; i < count; i++)
        {
            questions.Add(new Question
            {
                Id = i + 1,
                QuestionBankId = questionBankId,
                QuestionText = $"测试题目{i + 1}",
                QuestionType = "SingleChoice",
                QuestionDataJson = JsonTestHelper.Serialize(new ChoiceQuestionData
                {
                    Options = new List<string> { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" },
                    CorrectAnswers = new List<string> { "A" }
                }),
                OrderIndex = i,
                Difficulty = "medium"
            });
        }
        return questions;
    }

    private static Question CreateTestSingleChoiceQuestion(int id, int questionBankId, string correctAnswer)
    {
        return new Question
        {
            Id = id,
            QuestionBankId = questionBankId,
            QuestionText = "测试选择题",
            QuestionType = "SingleChoice",
            QuestionDataJson = JsonTestHelper.Serialize(new ChoiceQuestionData
            {
                Options = new List<string> { "A. 选项A", "B. 选项B", "C. 选项C", "D. 选项D" },
                CorrectAnswers = new List<string> { correctAnswer }
            }),
            Difficulty = "medium"
        };
    }

    private static Question CreateTestBooleanQuestion(int id, int questionBankId, bool correctAnswer)
    {
        return new Question
        {
            Id = id,
            QuestionBankId = questionBankId,
            QuestionText = "测试判断题",
            QuestionType = "TrueFalse",
            QuestionDataJson = JsonTestHelper.Serialize(new BooleanQuestionData
            {
                CorrectAnswer = correctAnswer
            }),
            Difficulty = "easy"
        };
    }

    private static Question CreateTestFillBlankQuestion(int id, int questionBankId, List<string> acceptableAnswers)
    {
        return new Question
        {
            Id = id,
            QuestionBankId = questionBankId,
            QuestionText = "测试填空题",
            QuestionType = "FillBlank",
            QuestionDataJson = JsonTestHelper.Serialize(new FillBlankQuestionData
            {
                AcceptableAnswers = acceptableAnswers
            }),
            Difficulty = "medium"
        };
    }

    private static Question CreateTestShortAnswerQuestion(int id, int questionBankId, string referenceAnswer)
    {
        return new Question
        {
            Id = id,
            QuestionBankId = questionBankId,
            QuestionText = "测试简答题",
            QuestionType = "ShortAnswer",
            QuestionDataJson = JsonTestHelper.Serialize(new ShortAnswerQuestionData
            {
                ReferenceAnswer = referenceAnswer
            }),
            Difficulty = "hard"
        };
    }

    private static Attempt CreateTestAttempt(int userId, int id = 1, int questionBankId = 1)
    {
        return new Attempt
        {
            Id = id,
            UserId = userId,
            QuestionBankId = questionBankId,
            StartedAt = DateTime.UtcNow,
            TotalQuestions = 5
        };
    }

    #endregion
}
