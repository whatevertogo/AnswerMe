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
}
