namespace AnswerMe.Application.AI;

/// <summary>
/// Prompt模板
/// </summary>
public static class PromptTemplates
{
    /// <summary>
    /// 题目生成模板
    /// </summary>
    public const string QuestionGeneration = @"你是一个专业的题目生成助手。

任务：生成{{count}}道关于""{{subject}}""的{{difficulty}}难度题目。

{% if customPrompt %}
额外要求：{{customPrompt}}
{% endif %}

题型：{{questionTypes}}
语言：{{language}}

要求：
1. 返回JSON格式
2. 包含questionType, questionText, options（数组）, correctAnswer, explanation, difficulty
3. 题目要符合{{language}}语言习惯
4. 选项要合理，干扰项要有迷惑性
5. 解析要详细准确

格式示例：
{
  ""questions"": [
    {
      ""questionType"": ""选择题"",
      ""questionText"": ""题目内容"",
      ""options"": [""选项A"", ""选项B"", ""选项C"", ""选项D""],
      ""correctAnswer"": ""选项A"",
      ""explanation"": ""详细解析"",
      ""difficulty"": ""easy""
    }
  ]
}

请严格按照上述JSON格式返回，不要包含任何其他文字说明。";

    /// <summary>
    /// 系统提示词
    /// </summary>
    public const string SystemPrompt = @"你是一个专业的教育题目生成助手。你的任务是：
1. 根据用户提供的主题生成高质量的题目
2. 题目要准确、清晰、符合教育标准
3. 必须返回有效的JSON格式
4. 确保选项中有且只有一个正确答案
5. 提供详细准确的解析

请始终返回纯JSON格式，不要包含任何markdown代码块标记或其他说明文字。";

    /// <summary>
    /// 获取题目生成的完整Prompt
    /// </summary>
    public static string GetQuestionGenerationPrompt(
        string subject,
        int count,
        string difficulty,
        List<string> questionTypes,
        string language,
        string? customPrompt = null)
    {
        var questionTypesStr = string.Join("、", questionTypes);
        var customPromptPart = !string.IsNullOrEmpty(customPrompt)
            ? $"\n额外要求：{customPrompt}\n"
            : "";

        return $@"{SystemPrompt}

请生成{count}道关于""{subject}""的{difficulty}难度题目，题型包括：{questionTypesStr}。{customPromptPart}要求：
1. 返回JSON格式
2. 包含questionType, questionText, options（数组）, correctAnswer, explanation, difficulty
3. 题目要符合{language}语言习惯
4. 选项要合理，干扰项要有迷惑性
5. 解析要详细准确

格式示例：
{{
  ""questions"": [
    {{
      ""questionType"": ""选择题"",
      ""questionText"": ""题目内容"",
      ""options"": [""选项A"", ""选项B"", ""选项C"", ""选项D""],
      ""correctAnswer"": ""选项A"",
      ""explanation"": ""详细解析"",
      ""difficulty"": ""easy""
    }}
  ]
}}

请严格按照上述JSON格式返回，不要包含任何其他文字说明。";
    }
}
