using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Enums;

namespace AnswerMe.UnitTests.Helpers;

/// <summary>
/// 测试数据生成器
/// </summary>
public static class TestDataGenerator
{
    private static readonly Random _random = new(42); // 固定种子以保证可重复性

    #region QuestionType Data

    public static readonly QuestionType[] AllQuestionTypes = Enum.GetValues<QuestionType>();

    public static QuestionType GetRandomQuestionType()
    {
        return AllQuestionTypes[_random.Next(AllQuestionTypes.Length)];
    }

    public static IEnumerable<QuestionType> GetAllQuestionTypes()
    {
        return AllQuestionTypes;
    }

    #endregion

    #region String Data

    public static string RandomString(int length = 10)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789abcdefghijklmnopqrstuvwxyz中文字符测试";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public static string RandomChineseText(int length = 20)
    {
        const string chineseChars = "这是一段用于测试的中文文本内容包含各种常用汉字";
        return new string(Enumerable.Repeat(chineseChars, length)
            .Select(s => s[_random.Next(s.Length)]).ToArray());
    }

    public static List<string> RandomStringList(int count, int minLength = 5, int maxLength = 20)
    {
        return Enumerable.Range(0, count)
            .Select(_ => RandomString(_random.Next(minLength, maxLength)))
            .ToList();
    }

    #endregion

    #region Question Data

    public static string RandomQuestionText()
    {
        var templates = new[]
        {
            "以下哪项是{0}的主要特征?",
            "关于{0},下列说法正确的是?",
            "{0}的主要用途包括?",
            "在使用{0}时应该注意什么?",
            "{0}的最佳实践是什么?"
        };

        var subject = RandomString(5);
        var template = templates[_random.Next(templates.Length)];

        return string.Format(template, subject);
    }

    public static List<string> RandomOptions(int count = 4)
    {
        const string optionPrefix = "ABCD";
        return Enumerable.Range(0, count)
            .Select(i => $"{optionPrefix[i]}. {RandomString(_random.Next(10, 30))}")
            .ToList();
    }

    public static string RandomExplanation()
    {
        var starters = new[]
        {
            "正确答案是",
            "这是因为",
            "根据原理",
            "通过分析可知"
        };

        var starter = starters[_random.Next(starters.Length)];
        return $"{starter}{RandomChineseText(_random.Next(20, 50))}。";
    }

    #endregion

    #region Number Data

    public static int RandomInt(int min = 1, int max = 100)
    {
        return _random.Next(min, max);
    }

    public static bool RandomBoolean()
    {
        return _random.Next(2) == 0;
    }

    #endregion

    #region DateTime Data

    public static DateTime RandomDateInRange(DateTime start, DateTime end)
    {
        var range = end - start;
        var randTimeSpan = TimeSpan.FromDays(_random.NextDouble() * range.TotalDays);
        return start + randTimeSpan;
    }

    public static DateTime RandomRecentDate(int daysBack = 30)
    {
        var end = DateTime.UtcNow;
        var start = end.AddDays(-daysBack);
        return RandomDateInRange(start, end);
    }

    #endregion

    #region Difficulty Levels

    public static readonly string[] DifficultyLevels = { "easy", "medium", "hard" };

    public static string RandomDifficulty()
    {
        return DifficultyLevels[_random.Next(DifficultyLevels.Length)];
    }

    #endregion

    #region Performance Test Data Generation

    /// <summary>
    /// 生成指定数量的旧格式题目（性能测试用）
    /// </summary>
    public static List<Question> GenerateOldFormatQuestions(int count)
    {
        var questions = new List<Question>();
        // 提高选择题占比，匹配迁移断言（>=60%）
        var types = new[] { "choice", "choice", "choice", "multiple", "multiple", "true-false", "fill", "essay" };
        var difficulties = new[] { "easy", "medium", "hard" };

        for (int i = 0; i < count; i++)
        {
            var type = types[_random.Next(types.Length)];
            var difficulty = difficulties[_random.Next(difficulties.Length)];

            var question = new Question
            {
                Id = i + 1,
                QuestionBankId = 1,
                QuestionText = $"性能测试题目 {i + 1}",
                QuestionType = type,
                Options = type is "choice" or "single" or "multiple"
                    ? "[\"A. 选项1\", \"B. 选项2\", \"C. 选项3\", \"D. 选项4\"]"
                    : null,
                CorrectAnswer = GetRandomCorrectAnswer(type),
                Explanation = $"测试解析 {i + 1}",
                Difficulty = difficulty,
                OrderIndex = i,
                CreatedAt = DateTime.UtcNow.AddDays(-_random.Next(365)),
                UpdatedAt = DateTime.UtcNow
            };

            questions.Add(question);
        }

        return questions;
    }

    private static string GetRandomCorrectAnswer(string type)
    {
        return type switch
        {
            "choice" or "single" => "A",
            "multiple" => "A,B",
            "true-false" => "true",
            "fill" => "答案",
            "essay" => "参考答案",
            _ => "A"
        };
    }

    /// <summary>
    /// 生成指定数量的新格式题目（性能测试用）
    /// </summary>
    public static List<Question> GenerateNewFormatQuestions(int count)
    {
        var questions = new List<Question>();
        var types = new[] { QuestionType.SingleChoice, QuestionType.MultipleChoice, QuestionType.TrueFalse, QuestionType.FillBlank, QuestionType.ShortAnswer };
        var difficulties = new[] { "easy", "medium", "hard" };

        for (int i = 0; i < count; i++)
        {
            var type = types[i % types.Length];
            var difficulty = difficulties[i % difficulties.Length];

            questions.Add(new Question
            {
                Id = i + 1,
                QuestionBankId = 1,
                QuestionText = $"性能测试题目 {i + 1}",
                QuestionType = type.ToString(),
                QuestionTypeEnum = type,
                QuestionDataJson = GenerateQuestionDataJson(type, difficulty),
                Difficulty = difficulty,
                OrderIndex = i,
                CreatedAt = DateTime.UtcNow.AddDays(-1),
                UpdatedAt = DateTime.UtcNow
            });
        }

        return questions;
    }

    private static string GenerateQuestionDataJson(QuestionType type, string difficulty)
    {
        return type switch
        {
            QuestionType.SingleChoice or QuestionType.MultipleChoice => System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "choice",
                options = new[] { "A. 选项1", "B. 选项2", "C. 选项3", "D. 选项4" },
                correctAnswers = new[] { "A" },
                explanation = "测试解析",
                difficulty
            }),
            QuestionType.TrueFalse => System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "boolean",
                correctAnswer = true,
                explanation = "测试解析",
                difficulty
            }),
            QuestionType.FillBlank => System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "fillBlank",
                correctAnswer = "答案",
                explanation = "测试解析",
                difficulty
            }),
            _ => System.Text.Json.JsonSerializer.Serialize(new
            {
                type = "shortAnswer",
                correctAnswer = "参考答案",
                explanation = "测试解析",
                difficulty
            })
        };
    }

    /// <summary>
    /// 生成混合格式的题目（性能测试用）
    /// </summary>
    /// <param name="count">总题目数量</param>
    /// <param name="oldFormatRatio">旧格式题目比例（0-1）</param>
    public static List<Question> GenerateMixedFormatQuestions(int count, double oldFormatRatio)
    {
        var questions = new List<Question>();
        var oldCount = (int)(count * oldFormatRatio);
        var newCount = count - oldCount;

        questions.AddRange(GenerateOldFormatQuestions(oldCount));
        questions.AddRange(GenerateNewFormatQuestions(newCount));

        // 打乱顺序以模拟真实环境
        return questions.OrderBy(_ => _random.Next()).ToList();
    }

    #endregion
}
