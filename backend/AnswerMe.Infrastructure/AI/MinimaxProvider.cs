using System.Text.Json;
using AnswerMe.Application.AI;
using Microsoft.Extensions.Logging;
using System.Net.Http.Json;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// Minimax AI Provider实现
/// </summary>
public class MinimaxProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<MinimaxProvider> _logger;
    private readonly string _providerName;
    private readonly string _defaultEndpoint;

    public string ProviderName => _providerName;

    public MinimaxProvider(
        IHttpClientFactory httpClientFactory,
        ILogger<MinimaxProvider> logger,
        string? providerName = null,
        string? defaultEndpoint = null)
    {
        _httpClient = httpClientFactory.CreateClient("AI");
        _logger = logger;
        _providerName = string.IsNullOrWhiteSpace(providerName) ? "minimax" : providerName;
        _defaultEndpoint = string.IsNullOrWhiteSpace(defaultEndpoint)
            ? "https://api.minimaxi.com/v1/text/chatcompletion_v2"
            : defaultEndpoint;
    }

    public async Task<AIQuestionGenerateResponse> GenerateQuestionsAsync(
        string apiKey,
        AIQuestionGenerateRequest request,
        string? model = null,
        string? endpoint = null,  // 忽略 endpoint 参数，Minimax 使用固定端点
        CancellationToken cancellationToken = default)
    {
        try
        {
            var prompt = BuildPrompt(request);

            // 使用配置的模型，如果为空则使用默认模型 M2-her
            var modelToUse = string.IsNullOrEmpty(model) ? "M2-her" : model;

            // ✅ 根据题目数量动态计算max_tokens
            var estimatedTokensPerQuestion = 250;
            var maxTokens = Math.Clamp(request.Count * estimatedTokensPerQuestion + 1000, 1000, 8000);

            _logger.LogInformation("Minimax配置: Model={Model}, QuestionCount={Count}, MaxTokens={MaxTokens}",
                modelToUse, request.Count, maxTokens);

            // ✅ 支持自定义端点（如代理或镜像）
            // 如果用户提供了自定义端点则使用，否则使用 Minimax 官方端点
            var actualEndpoint = string.IsNullOrEmpty(endpoint)
                ? _defaultEndpoint  // 默认官方端点
                : endpoint;  // 用户自定义端点

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
                max_completion_tokens = maxTokens
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

            // 使用重试机制发送请求（支持指数退避）
            var response = await HttpRetryHelper.SendWithRetryAsync(_httpClient, httpRequest, _logger, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Minimax API错误: {StatusCode}, {Body}", response.StatusCode, responseBody);

                return new AIQuestionGenerateResponse
                {
                    Success = false,
                    ErrorMessage = $"Minimax API调用失败: {response.StatusCode}",
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
                ? _defaultEndpoint
                : endpoint;
            var modelToUse = string.IsNullOrEmpty(model) ? "M2-her" : model;

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
                    max_completion_tokens = 5
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
