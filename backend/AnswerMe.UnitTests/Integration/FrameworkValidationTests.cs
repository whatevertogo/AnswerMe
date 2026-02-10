using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;
using AnswerMe.Domain.Models;
using FluentAssertions;
using Xunit;
using Microsoft.EntityFrameworkCore;

namespace AnswerMe.UnitTests.Integration;

/// <summary>
/// 集成测试框架验证测试
/// 验证测试基础设施是否正常工作
/// </summary>
public class FrameworkValidationTests : TestBase
{
    [Fact]
    public async Task DatabaseFixture_ShouldCreateDatabaseSuccessfully()
    {
        // Act & Assert - 如果没有抛异常,则数据库创建成功
        DbContext.Database.Should().NotBeNull();
    }

    [Fact]
    public async Task TestBase_ShouldProvideAllRequiredServices()
    {
        // Assert
        UserRepository.Should().NotBeNull();
        QuestionBankRepository.Should().NotBeNull();
        QuestionRepository.Should().NotBeNull();
        QuestionService.Should().NotBeNull();
        QuestionBankService.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateTestUserAsync_ShouldCreateUserSuccessfully()
    {
        // Arrange
        var username = "test_user_123";

        // Act
        var user = await CreateTestUserAsync(username);

        // Assert
        user.Should().NotBeNull();
        user.Id.Should().BeGreaterThan(0);
        user.Username.Should().Be(username);
        user.Email.Should().Contain("@");
    }

    [Fact]
    public async Task CreateTestQuestionBankAsync_ShouldCreateBankSuccessfully()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var bankName = "Test Bank";

        // Act
        var bank = await CreateTestQuestionBankAsync(user.Id, bankName);

        // Assert
        bank.Should().NotBeNull();
        bank.Id.Should().BeGreaterThan(0);
        bank.Name.Should().Be(bankName);
        bank.UserId.Should().Be(user.Id);
    }

    [Fact]
    public async Task CreateTestQuestionAsync_ShouldCreateSingleChoiceQuestion()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var bank = await CreateTestQuestionBankAsync(user.Id);
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. Test 1", "B. Test 2" },
            CorrectAnswers = new List<string> { "A" },
            Explanation = "Test explanation"
        };

        // Act
        var question = await CreateTestQuestionAsync(bank.Id, QuestionType.SingleChoice, data);

        // Assert
        question.Should().NotBeNull();
        question.Id.Should().BeGreaterThan(0);
        question.QuestionTypeEnum.Should().Be(QuestionType.SingleChoice);
        question.QuestionBankId.Should().Be(bank.Id);

        var choiceData = question.Data as ChoiceQuestionData;
        choiceData.Should().NotBeNull();
        choiceData!.Options.Should().HaveCount(2);
        choiceData.CorrectAnswers.Should().ContainSingle("A");
    }

    [Fact]
    public async Task CreateTestQuestionAsync_ShouldCreateMultipleChoiceQuestion()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var bank = await CreateTestQuestionBankAsync(user.Id);
        var data = new ChoiceQuestionData
        {
            Options = new List<string> { "A. Test 1", "B. Test 2", "C. Test 3", "D. Test 4" },
            CorrectAnswers = new List<string> { "A", "C", "D" }, // 多选题: 3个答案
            Explanation = "Multiple choice explanation"
        };

        // Act
        var question = await CreateTestQuestionAsync(bank.Id, QuestionType.MultipleChoice, data);

        // Assert
        question.Should().NotBeNull();
        question.QuestionTypeEnum.Should().Be(QuestionType.MultipleChoice);

        var choiceData = question.Data as ChoiceQuestionData;
        choiceData.Should().NotBeNull();
        choiceData!.CorrectAnswers.Should().HaveCount(3);
        choiceData.CorrectAnswers.Should().BeEquivalentTo(new[] { "A", "C", "D" });
    }

    [Fact]
    public async Task ExecuteInTransactionAsync_ShouldRollbackChanges()
    {
        // Arrange
        var initialUserCount = await DbContext.Users.CountAsync();

        // Act - 在事务中创建用户,然后回滚
        await ExecuteInTransactionAsync(async () =>
        {
            var user = new User
            {
                Username = "transaction_test_user",
                Email = "transaction@test.com",
                PasswordHash = "hash",
                CreatedAt = DateTime.UtcNow
            };

            DbContext.Users.Add(user);
            await DbContext.SaveChangesAsync();
        });

        // Assert - 用户数量应该没有变化 (已回滚)
        var finalUserCount = await DbContext.Users.CountAsync();
        finalUserCount.Should().Be(initialUserCount);
    }

    [Fact]
    public async Task ClearDatabaseAsync_ShouldRemoveAllData()
    {
        // Arrange - 创建一些测试数据
        var user = await CreateTestUserAsync();
        await CreateTestQuestionBankAsync(user.Id);
        await CreateTestQuestionAsync(user.Id, QuestionType.TrueFalse);

        // Act
        await ClearDatabaseAsync();

        // Assert - 所有表应该为空
        (await DbContext.Users.CountAsync()).Should().Be(0);
        (await DbContext.QuestionBanks.CountAsync()).Should().Be(0);
        (await DbContext.Questions.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateDefaultQuestionData_ShouldSupportAllQuestionTypes()
    {
        // Arrange & Act
        var singleChoice = CreateDefaultQuestionData(QuestionType.SingleChoice);
        var multipleChoice = CreateDefaultQuestionData(QuestionType.MultipleChoice);
        var trueFalse = CreateDefaultQuestionData(QuestionType.TrueFalse);
        var fillBlank = CreateDefaultQuestionData(QuestionType.FillBlank);
        var shortAnswer = CreateDefaultQuestionData(QuestionType.ShortAnswer);

        // Assert
        singleChoice.Should().BeOfType<ChoiceQuestionData>();
        multipleChoice.Should().BeOfType<ChoiceQuestionData>();
        trueFalse.Should().BeOfType<BooleanQuestionData>();
        fillBlank.Should().BeOfType<FillBlankQuestionData>();
        shortAnswer.Should().BeOfType<ShortAnswerQuestionData>();
    }

    [Fact]
    public async Task DatabaseFixture_ShouldSupportMultipleQuestionTypes()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var bank = await CreateTestQuestionBankAsync(user.Id);

        // Act - 创建每种类型的题目
        var singleChoice = await CreateTestQuestionAsync(bank.Id, QuestionType.SingleChoice);
        var multipleChoice = await CreateTestQuestionAsync(bank.Id, QuestionType.MultipleChoice);
        var trueFalse = await CreateTestQuestionAsync(bank.Id, QuestionType.TrueFalse);
        var fillBlank = await CreateTestQuestionAsync(bank.Id, QuestionType.FillBlank);
        var shortAnswer = await CreateTestQuestionAsync(bank.Id, QuestionType.ShortAnswer);

        // Assert
        var questions = await DbContext.Questions
            .Where(q => q.QuestionBankId == bank.Id)
            .ToListAsync();

        questions.Should().HaveCount(5);
        questions.Select(q => q.QuestionTypeEnum).Should().BeEquivalentTo(new[]
        {
            QuestionType.SingleChoice,
            QuestionType.MultipleChoice,
            QuestionType.TrueFalse,
            QuestionType.FillBlank,
            QuestionType.ShortAnswer
        });
    }

    [Fact]
    public async Task DatabaseFixture_ShouldSupportQuestionDataJsonRoundTrip()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var bank = await CreateTestQuestionBankAsync(user.Id);

        var originalData = new ChoiceQuestionData
        {
            Options = new List<string> { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" },
            CorrectAnswers = new List<string> { "B", "C" },
            Explanation = "多选题解析",
            Difficulty = "hard"
        };

        // Act - 创建题目
        var question = await CreateTestQuestionAsync(bank.Id, QuestionType.MultipleChoice, originalData);

        // 重新从数据库读取
        var retrievedQuestion = await DbContext.Questions.FindAsync(question.Id);

        // Assert
        retrievedQuestion.Should().NotBeNull();
        retrievedQuestion!.QuestionTypeEnum.Should().Be(QuestionType.MultipleChoice);

        var retrievedData = retrievedQuestion.Data as ChoiceQuestionData;
        retrievedData.Should().NotBeNull();
        retrievedData!.Options.Should().BeEquivalentTo(originalData.Options);
        retrievedData.CorrectAnswers.Should().BeEquivalentTo(originalData.CorrectAnswers);
        retrievedData.Explanation.Should().Be(originalData.Explanation);
        retrievedData.Difficulty.Should().Be(originalData.Difficulty);
    }

    [Fact]
    public async Task Repository_ShouldBeAbleToQueryQuestions()
    {
        // Arrange
        var user = await CreateTestUserAsync();
        var bank = await CreateTestQuestionBankAsync(user.Id);
        await CreateTestQuestionAsync(bank.Id, QuestionType.SingleChoice);
        await CreateTestQuestionAsync(bank.Id, QuestionType.MultipleChoice);

        // Act
        var questions = await QuestionRepository.GetByQuestionBankIdAsync(bank.Id);

        // Assert
        questions.Should().HaveCount(2);
    }
}
