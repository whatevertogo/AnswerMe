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
/// QuestionService 单元测试
/// </summary>
public class QuestionServiceTests
{
    private readonly Mock<IQuestionRepository> _mockQuestionRepository;
    private readonly Mock<IQuestionBankRepository> _mockQuestionBankRepository;
    private readonly QuestionService _questionService;
    private readonly int _testUserId = 1;

    public QuestionServiceTests()
    {
        _mockQuestionRepository = new Mock<IQuestionRepository>();
        _mockQuestionBankRepository = new Mock<IQuestionBankRepository>();
        _questionService = new QuestionService(
            _mockQuestionRepository.Object,
            _mockQuestionBankRepository.Object);
    }

    #region Helper Methods

    private QuestionBank CreateTestQuestionBank(int id = 1, int? userId = null)
    {
        return new QuestionBank
        {
            Id = id,
            UserId = userId ?? _testUserId,
            Name = $"测试题库 {id}",
            Description = "测试题库描述",
            Tags = "[]",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private Question CreateTestQuestion(int id = 1, int questionBankId = 1)
    {
        return new Question
        {
            Id = id,
            QuestionBankId = questionBankId,
            QuestionText = "测试题目内容",
            QuestionType = QuestionType.SingleChoice.ToString(),
            Explanation = "测试解析",
            Difficulty = "medium",
            OrderIndex = 0,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };
    }

    private CreateQuestionDto CreateChoiceQuestionDto(
        int questionBankId,
        ChoiceQuestionData? data = null)
    {
        return new CreateQuestionDto
        {
            QuestionBankId = questionBankId,
            QuestionText = "选择题题目内容",
            QuestionTypeEnum = QuestionType.SingleChoice,
            Data = data ?? new ChoiceQuestionData
            {
                Options = new List<string> { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" },
                CorrectAnswers = new List<string> { "A" },
                Explanation = "正确答案是A",
                Difficulty = "medium"
            },
            Difficulty = "medium",
            OrderIndex = 0
        };
    }

    private CreateQuestionDto CreateBooleanQuestionDto(int questionBankId, bool correctAnswer = true)
    {
        return new CreateQuestionDto
        {
            QuestionBankId = questionBankId,
            QuestionText = "判断题题目内容",
            QuestionTypeEnum = QuestionType.TrueFalse,
            Data = new BooleanQuestionData
            {
                CorrectAnswer = correctAnswer,
                Explanation = correctAnswer ? "正确" : "错误",
                Difficulty = "easy"
            },
            Difficulty = "easy",
            OrderIndex = 0
        };
    }

    private CreateQuestionDto CreateFillBlankQuestionDto(int questionBankId)
    {
        return new CreateQuestionDto
        {
            QuestionBankId = questionBankId,
            QuestionText = "填空题题目内容",
            QuestionTypeEnum = QuestionType.FillBlank,
            Data = new FillBlankQuestionData
            {
                AcceptableAnswers = new List<string> { "答案1", "答案2" },
                Explanation = "这是填空题解析",
                Difficulty = "medium"
            },
            Difficulty = "medium",
            OrderIndex = 0
        };
    }

    private CreateQuestionDto CreateShortAnswerQuestionDto(int questionBankId)
    {
        return new CreateQuestionDto
        {
            QuestionBankId = questionBankId,
            QuestionText = "简答题题目内容",
            QuestionTypeEnum = QuestionType.ShortAnswer,
            Data = new ShortAnswerQuestionData
            {
                ReferenceAnswer = "这是参考答案",
                Explanation = "这是简答题解析",
                Difficulty = "hard"
            },
            Difficulty = "hard",
            OrderIndex = 0
        };
    }

    #endregion

    #region CreateAsync Tests

    [Fact]
    public async Task CreateAsync_WithValidChoiceQuestionData_ShouldCreateQuestion()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var dto = CreateChoiceQuestionDto(questionBankId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question q, CancellationToken _) =>
            {
                q.Id = 1;
                return q;
            });

        // Act
        var result = await _questionService.CreateAsync(_testUserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.Id.Should().Be(1);
        result.QuestionText.Should().Be(dto.QuestionText);
        result.QuestionTypeEnum.Should().Be(QuestionType.SingleChoice);
        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<ChoiceQuestionData>();
        result.Difficulty.Should().Be("medium");

        _mockQuestionRepository.Verify(r => r.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockQuestionRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateAsync_WithValidBooleanQuestionData_ShouldCreateQuestion()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var dto = CreateBooleanQuestionDto(questionBankId, true);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question q, CancellationToken _) =>
            {
                q.Id = 1;
                return q;
            });

        // Act
        var result = await _questionService.CreateAsync(_testUserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.QuestionTypeEnum.Should().Be(QuestionType.TrueFalse);
        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<BooleanQuestionData>();
        ((BooleanQuestionData)result.Data!).CorrectAnswer.Should().BeTrue();
    }

    [Fact]
    public async Task CreateAsync_WithValidFillBlankQuestionData_ShouldCreateQuestion()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var dto = CreateFillBlankQuestionDto(questionBankId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question q, CancellationToken _) =>
            {
                q.Id = 1;
                return q;
            });

        // Act
        var result = await _questionService.CreateAsync(_testUserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.QuestionTypeEnum.Should().Be(QuestionType.FillBlank);
        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<FillBlankQuestionData>();
        ((FillBlankQuestionData)result.Data!).AcceptableAnswers.Should().HaveCount(2);
    }

    [Fact]
    public async Task CreateAsync_WithValidShortAnswerQuestionData_ShouldCreateQuestion()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var dto = CreateShortAnswerQuestionDto(questionBankId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.AddAsync(It.IsAny<Question>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question q, CancellationToken _) =>
            {
                q.Id = 1;
                return q;
            });

        // Act
        var result = await _questionService.CreateAsync(_testUserId, dto);

        // Assert
        result.Should().NotBeNull();
        result.QuestionTypeEnum.Should().Be(QuestionType.ShortAnswer);
        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<ShortAnswerQuestionData>();
        ((ShortAnswerQuestionData)result.Data!).ReferenceAnswer.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateAsync_WithNonExistentQuestionBank_ShouldThrowException()
    {
        // Arrange
        var questionBankId = 999;
        var dto = CreateChoiceQuestionDto(questionBankId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuestionBank?)null);

        // Act
        var act = async () => await _questionService.CreateAsync(_testUserId, dto);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("题库不存在或无权访问");
    }

    [Fact]
    public async Task CreateAsync_WithQuestionBankOwnedByOtherUser_ShouldThrowException()
    {
        // Arrange
        var questionBankId = 1;
        var otherUserId = 999;
        var questionBank = CreateTestQuestionBank(questionBankId, otherUserId);
        var dto = CreateChoiceQuestionDto(questionBankId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var act = async () => await _questionService.CreateAsync(_testUserId, dto);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    #endregion

    #region GetByIdAsync Tests

    [Fact]
    public async Task GetByIdAsync_WithExistingQuestion_ShouldReturnQuestion()
    {
        // Arrange
        var questionId = 1;
        var questionBankId = 1;
        var question = CreateTestQuestion(questionId, questionBankId);
        var questionBank = CreateTestQuestionBank(questionBankId);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.GetByIdAsync(questionId, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result!.Id.Should().Be(questionId);
        result.QuestionText.Should().Be(question.QuestionText);
    }

    [Fact]
    public async Task GetByIdAsync_WithNonExistentQuestion_ShouldReturnNull()
    {
        // Arrange
        var questionId = 999;

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question?)null);

        // Act
        var result = await _questionService.GetByIdAsync(questionId, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithQuestionBankOwnedByOtherUser_ShouldReturnNull()
    {
        // Arrange
        var questionId = 1;
        var questionBankId = 1;
        var question = CreateTestQuestion(questionId, questionBankId);
        var otherUserId = 999;
        var questionBank = CreateTestQuestionBank(questionBankId, otherUserId);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.GetByIdAsync(questionId, _testUserId);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task GetByIdAsync_WithQuestion_ShouldCorrectlyParseData()
    {
        // Arrange
        var questionId = 1;
        var questionBankId = 1;
        var question = CreateTestQuestion(questionId, questionBankId);
        question.QuestionDataJson = "{\"type\":\"choice\",\"options\":[\"A. 选项1\",\"B. 选项2\"],\"correctAnswers\":[\"A\"],\"explanation\":\"解析\",\"difficulty\":\"easy\"}";
        var questionBank = CreateTestQuestionBank(questionBankId);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.GetByIdAsync(questionId, _testUserId);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<ChoiceQuestionData>();
    }

    #endregion

    #region UpdateAsync Tests

    [Fact]
    public async Task UpdateAsync_WithValidData_ShouldUpdateQuestion()
    {
        // Arrange
        var questionId = 1;
        var questionBankId = 1;
        var question = CreateTestQuestion(questionId, questionBankId);
        var questionBank = CreateTestQuestionBank(questionBankId);

        var updateDto = new UpdateQuestionDto
        {
            QuestionText = "更新后的题目",
            Data = new ChoiceQuestionData
            {
                Options = new List<string> { "A. 新选项1", "B. 新选项2" },
                CorrectAnswers = new List<string> { "B" },
                Explanation = "更新后的解析",
                Difficulty = "hard"
            }
        };

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.UpdateAsync(questionId, _testUserId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.QuestionText.Should().Be("更新后的题目");
        result.Data.Should().NotBeNull();
        result.Data.Should().BeOfType<ChoiceQuestionData>();

        _mockQuestionRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_WithNonExistentQuestion_ShouldReturnNull()
    {
        // Arrange
        var questionId = 999;
        var updateDto = new UpdateQuestionDto { QuestionText = "更新后的题目" };

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question?)null);

        // Act
        var result = await _questionService.UpdateAsync(questionId, _testUserId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithQuestionBankOwnedByOtherUser_ShouldReturnNull()
    {
        // Arrange
        var questionId = 1;
        var questionBankId = 1;
        var question = CreateTestQuestion(questionId, questionBankId);
        var otherUserId = 999;
        var questionBank = CreateTestQuestionBank(questionBankId, otherUserId);

        var updateDto = new UpdateQuestionDto { QuestionText = "更新后的题目" };

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.UpdateAsync(questionId, _testUserId, updateDto);

        // Assert
        result.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_WithPartialUpdate_ShouldOnlyUpdateSpecifiedFields()
    {
        // Arrange
        var questionId = 1;
        var questionBankId = 1;
        var originalText = "原始题目";
        var question = CreateTestQuestion(questionId, questionBankId);
        question.QuestionText = originalText;
        question.Difficulty = "easy";
        var questionBank = CreateTestQuestionBank(questionBankId);

        var updateDto = new UpdateQuestionDto
        {
            Difficulty = "hard"
            // QuestionText 不更新
        };

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.UpdateAsync(questionId, _testUserId, updateDto);

        // Assert
        result.Should().NotBeNull();
        result!.QuestionText.Should().Be(originalText);
        result.Difficulty.Should().Be("hard");
    }

    #endregion

    #region DeleteAsync Tests

    [Fact]
    public async Task DeleteAsync_WithExistingQuestion_ShouldReturnTrue()
    {
        // Arrange
        var questionId = 1;
        var questionBankId = 1;
        var question = CreateTestQuestion(questionId, questionBankId);
        var questionBank = CreateTestQuestionBank(questionBankId);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.DeleteAsync(questionId, _testUserId);

        // Assert
        result.Should().BeTrue();
        _mockQuestionRepository.Verify(r => r.DeleteAsync(questionId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task DeleteAsync_WithNonExistentQuestion_ShouldReturnFalse()
    {
        // Arrange
        var questionId = 999;

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((Question?)null);

        // Act
        var result = await _questionService.DeleteAsync(questionId, _testUserId);

        // Assert
        result.Should().BeFalse();
        _mockQuestionRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task DeleteAsync_WithQuestionBankOwnedByOtherUser_ShouldReturnFalse()
    {
        // Arrange
        var questionId = 1;
        var questionBankId = 1;
        var question = CreateTestQuestion(questionId, questionBankId);
        var otherUserId = 999;
        var questionBank = CreateTestQuestionBank(questionBankId, otherUserId);

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(questionId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(question);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.DeleteAsync(questionId, _testUserId);

        // Assert
        result.Should().BeFalse();
        _mockQuestionRepository.Verify(r => r.DeleteAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    #endregion

    #region GetListAsync Tests

    [Fact]
    public async Task GetListAsync_WithValidQuestionBank_ShouldReturnQuestions()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var questions = new List<Question>
        {
            CreateTestQuestion(1, questionBankId),
            CreateTestQuestion(2, questionBankId),
            CreateTestQuestion(3, questionBankId)
        };

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.GetPagedFilteredAsync(
                questionBankId,
                It.IsAny<int>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(questions);

        _mockQuestionRepository
            .Setup(r => r.CountFilteredAsync(
                questionBankId,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var query = new QuestionListQueryDto
        {
            QuestionBankId = questionBankId,
            PageSize = 20
        };

        // Act
        var result = await _questionService.GetListAsync(_testUserId, query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(3);
        result.TotalCount.Should().Be(3);
        result.HasMore.Should().BeFalse();
    }

    [Fact]
    public async Task GetListAsync_WithEmptyQuestionBank_ShouldReturnEmptyList()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.GetPagedFilteredAsync(
                questionBankId,
                It.IsAny<int>(),
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question>());

        _mockQuestionRepository
            .Setup(r => r.CountFilteredAsync(
                questionBankId,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        var query = new QuestionListQueryDto
        {
            QuestionBankId = questionBankId,
            PageSize = 20
        };

        // Act
        var result = await _questionService.GetListAsync(_testUserId, query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().BeEmpty();
        result.TotalCount.Should().Be(0);
    }

    [Fact]
    public async Task GetListAsync_WithPagination_ShouldReturnPaginatedResults()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var questions = new List<Question>
        {
            CreateTestQuestion(1, questionBankId),
            CreateTestQuestion(2, questionBankId)
        };

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // 第一次请求获取 PageSize + 1 = 3 条（检测是否有更多）
        _mockQuestionRepository
            .Setup(r => r.GetPagedFilteredAsync(
                questionBankId,
                3,
                null,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<Question>
            {
                CreateTestQuestion(1, questionBankId),
                CreateTestQuestion(2, questionBankId),
                CreateTestQuestion(3, questionBankId)
            });

        _mockQuestionRepository
            .Setup(r => r.CountFilteredAsync(
                questionBankId,
                null,
                null,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);

        var query = new QuestionListQueryDto
        {
            QuestionBankId = questionBankId,
            PageSize = 2
        };

        // Act
        var result = await _questionService.GetListAsync(_testUserId, query);

        // Assert
        result.Should().NotBeNull();
        result.Data.Should().HaveCount(2); // 只返回 PageSize 条
        result.HasMore.Should().BeTrue();
        result.NextCursor.Should().NotBeNull();
        result.TotalCount.Should().Be(3);
    }

    [Fact]
    public async Task GetListAsync_WithNonExistentQuestionBank_ShouldThrowException()
    {
        // Arrange
        var questionBankId = 999;

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuestionBank?)null);

        var query = new QuestionListQueryDto
        {
            QuestionBankId = questionBankId,
            PageSize = 20
        };

        // Act
        var act = async () => await _questionService.GetListAsync(_testUserId, query);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    [Fact]
    public async Task GetListAsync_WithQuestionBankOwnedByOtherUser_ShouldThrowException()
    {
        // Arrange
        var questionBankId = 1;
        var otherUserId = 999;
        var questionBank = CreateTestQuestionBank(questionBankId, otherUserId);

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        var query = new QuestionListQueryDto
        {
            QuestionBankId = questionBankId,
            PageSize = 20
        };

        // Act
        var act = async () => await _questionService.GetListAsync(_testUserId, query);

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    #endregion

    #region CreateBatchAsync Tests

    [Fact]
    public async Task CreateBatchAsync_WithValidData_ShouldCreateAllQuestions()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var dtos = new List<CreateQuestionDto>
        {
            CreateChoiceQuestionDto(questionBankId),
            CreateBooleanQuestionDto(questionBankId),
            CreateFillBlankQuestionDto(questionBankId)
        };

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.AddRangeAsync(It.IsAny<List<Question>>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _questionService.CreateBatchAsync(_testUserId, dtos);

        // Assert
        result.Should().HaveCount(3);
        _mockQuestionRepository.Verify(r => r.AddRangeAsync(It.IsAny<List<Question>>(), It.IsAny<CancellationToken>()), Times.Once);
        _mockQuestionRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task CreateBatchAsync_WithEmptyList_ShouldThrowException()
    {
        // Arrange
        var dtos = new List<CreateQuestionDto>();

        // Act
        var act = async () => await _questionService.CreateBatchAsync(_testUserId, dtos);

        // Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(act);
        exception.Message.Should().Contain("创建题目列表不能为空");
    }

    #endregion

    #region DeleteBatchAsync Tests

    [Fact]
    public async Task DeleteBatchAsync_WithValidIds_ShouldReturnCorrectCounts()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var questions = new List<Question>
        {
            CreateTestQuestion(1, questionBankId),
            CreateTestQuestion(2, questionBankId),
            CreateTestQuestion(3, questionBankId)
        };

        _mockQuestionRepository
            .Setup(r => r.GetByIdAsync(It.IsAny<int>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((int id, CancellationToken _) => questions.FirstOrDefault(q => q.Id == id));

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        // Act
        var result = await _questionService.DeleteBatchAsync(_testUserId, new List<int> { 1, 2, 3 });

        // Assert
        result.successCount.Should().Be(3);
        result.notFoundCount.Should().Be(0);
    }

    [Fact]
    public async Task DeleteBatchAsync_WithEmptyList_ShouldReturnZeroCounts()
    {
        // Act
        var result = await _questionService.DeleteBatchAsync(_testUserId, new List<int>());

        // Assert
        result.successCount.Should().Be(0);
        result.notFoundCount.Should().Be(0);
    }

    #endregion

    #region SearchAsync Tests

    [Fact]
    public async Task SearchAsync_WithValidSearchTerm_ShouldReturnMatchingQuestions()
    {
        // Arrange
        var questionBankId = 1;
        var questionBank = CreateTestQuestionBank(questionBankId);
        var searchTerm = "测试";
        var questions = new List<Question>
        {
            CreateTestQuestion(1, questionBankId)
        };

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questionBank);

        _mockQuestionRepository
            .Setup(r => r.SearchAsync(questionBankId, searchTerm, It.IsAny<CancellationToken>()))
            .ReturnsAsync(questions);

        // Act
        var result = await _questionService.SearchAsync(questionBankId, _testUserId, searchTerm);

        // Assert
        result.Should().HaveCount(1);
    }

    [Fact]
    public async Task SearchAsync_WithNonExistentQuestionBank_ShouldThrowException()
    {
        // Arrange
        var questionBankId = 999;

        _mockQuestionBankRepository
            .Setup(r => r.GetByIdAsync(questionBankId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((QuestionBank?)null);

        // Act
        var act = async () => await _questionService.SearchAsync(questionBankId, _testUserId, "测试");

        // Assert
        await Assert.ThrowsAsync<InvalidOperationException>(act);
    }

    #endregion
}
