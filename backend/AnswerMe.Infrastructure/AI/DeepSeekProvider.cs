using System.Text.Json;
using AnswerMe.Application.AI;
using Microsoft.Extensions.Logging;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// DeepSeek Provider实现
/// API文档: https://api-docs.deepseek.com/
///
/// 支持的模型:
/// - deepseek-chat: DeepSeek-V3.2 (非思考模式), 128K context, 最大 8K 输出
/// - deepseek-reasoner: DeepSeek-V3.2 (思考模式), 最大 64K 输出
/// </summary>
public class DeepSeekProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<DeepSeekProvider> _logger;

    public string ProviderName => "DeepSeek";

    public DeepSeekProvider(IHttpClientFactory httpClientFactory, ILogger<DeepSeekProvider> logger)
    {
        _httpClient = httpClientFactory.CreateClient("AI");
        _logger = logger;
    }

    public async Task<AIQuestionGenerateResponse> GenerateQuestionsAsync(
        string apiKey,
        AIQuestionGenerateRequest request,
        string? model = null,
        string? endpoint = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildPrompt(request);

            // 使用配置的模型，如果为空则使用默认模型 deepseek-chat
            var modelToUse = string.IsNullOrEmpty(model) ? "deepseek-chat" : model;

            // 根据题目数量动态计算max_tokens
            // DeepSeek deepseek-chat 模型最大支持 8K 输出
            var estimatedTokensPerQuestion = 250;
            var maxTokens = Math.Clamp(request.Count * estimatedTokensPerQuestion + 1000, 1000, 8192);

            _logger.LogInformation("DeepSeek配置: Model={Model}, QuestionCount={Count}, MaxTokens={MaxTokens}",
                modelToUse, request.Count, maxTokens);

            // DeepSeek API 端点
            var actualEndpoint = string.IsNullOrEmpty(endpoint)
                ? "https://api.deepseek.com/chat/completions"
                : endpoint;

            var requestBody = new
            {
                model = modelToUse,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "你是一个专业的题目生成助手。请根据用户要求生成题目，返回JSON格式。"
                    },
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                },
                temperature = 0.7,
                max_tokens = maxTokens
            };

            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(actualEndpoint),
                Headers =
                {
                    { "Authorization", $"Bearer {apiKey}" }
                },
                Content = new StringContent(JsonSerializer.Serialize(requestBody), System.Text.Encoding.UTF8, "application/json")
            };

            // 使用重试机制发送请求
            var response = await HttpRetryHelper.SendWithRetryAsync(_httpClient, httpRequest, _logger, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("DeepSeek API错误: {StatusCode}, {Body}", response.StatusCode, responseBody);

                return new AIQuestionGenerateResponse
                {
                    Success = false,
                    ErrorMessage = $"DeepSeek API调用失败: {response.StatusCode}",
                    ErrorCode = ((int)response.StatusCode).ToString()
                };
            }

            var jsonDoc = JsonDocument.Parse(responseBody);
            var content = jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString()!;

            return ParseResponse(content);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "生成题目失败");
            return new AIQuestionGenerateResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "EXCEPTION"
            };
        }
    }

    public async Task<bool> ValidateApiKeyAsync(
        string apiKey,
        string? endpoint = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        try
        {
            var actualEndpoint = string.IsNullOrEmpty(endpoint)
                ? "https://api.deepseek.com/chat/completions"
                : endpoint;
            var modelToUse = string.IsNullOrEmpty(model) ? "deepseek-chat" : model;

            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(actualEndpoint),
                Headers =
                {
                    { "Authorization", $"Bearer {apiKey}" }
                },
                Content = new StringContent(JsonSerializer.Serialize(new
                {
                    model = modelToUse,
                    messages = new[]
                    {
                        new { role = "user", content = "hi" }
                    },
                    max_tokens = 5
                }), System.Text.Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private string BuildPrompt(AIQuestionGenerateRequest request)
    {
        var customPrompt = !string.IsNullOrEmpty(request.CustomPrompt)
            ? $"额外要求：{request.CustomPrompt}\n"
            : "";

        var questionTypesStr = string.Join("、", request.QuestionTypes);

        return $@"请生成{request.Count}道关于""{request.Subject}""的{request.Difficulty}难度题目，题型包括：{questionTypesStr}。

{customPrompt}要求：
1. 返回JSON格式
2. 包含questionType, questionText, options（数组）, correctAnswer, explanation, difficulty
3. 题目要符合{request.Language}语言习惯
4. 选项要合理，干扰项要有迷惑性
5. 解析要详细准确

格式示例：
{{
  ""questions"": [
    {{
      ""questionType"": ""选择题"",
      ""questionText"": ""以下哪个是JavaScript的基本数据类型？"",
      ""options"": [""字符串"", ""数字"", ""布尔"", ""对象""],
      ""correctAnswer"": ""字符串"",
      ""explanation"": ""JavaScript的基本数据类型包括字符串、数字、布尔、Symbol、BigInt、null。对象是引用类型，不是基本数据类型。"",
      ""difficulty"": ""easy""
    }}
  ]
}}";
    }

    private AIQuestionGenerateResponse ParseResponse(string content)
    {
        if (!AIResponseParser.TryParseQuestions(content, out var questions, out var error))
        {
            return new AIQuestionGenerateResponse
            {
                Success = false,
                ErrorMessage = error ?? "解析响应失败",
                ErrorCode = "PARSE_ERROR"
            };
        }

        return new AIQuestionGenerateResponse
        {
            Success = true,
            Questions = questions,
            TokensUsed = content.Length
        };
    }
}
