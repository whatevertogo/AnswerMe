using System.Text.Json;
using AnswerMe.Application.AI;
using Microsoft.Extensions.Logging;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// Anthropic Provider实现（Claude Messages API）
/// </summary>
public class AnthropicProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<AnthropicProvider> _logger;

    public string ProviderName => "anthropic";

    public AnthropicProvider(IHttpClientFactory httpClientFactory, ILogger<AnthropicProvider> logger)
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
            var modelToUse = string.IsNullOrWhiteSpace(model) ? "claude-3-5-sonnet-latest" : model;
            var actualEndpoint = string.IsNullOrWhiteSpace(endpoint)
                ? "https://api.anthropic.com/v1/messages"
                : endpoint;

            var estimatedTokensPerQuestion = 300;
            var maxTokens = Math.Clamp(request.Count * estimatedTokensPerQuestion + 1000, 1000, 8192);

            _logger.LogInformation(
                "Anthropic配置: Model={Model}, QuestionCount={Count}, MaxTokens={MaxTokens}",
                modelToUse,
                request.Count,
                maxTokens);

            var requestBody = new
            {
                model = modelToUse,
                max_tokens = maxTokens,
                system = "你是一个专业的题目生成助手。请根据用户要求生成题目，返回JSON格式。",
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = prompt
                    }
                }
            };

            var httpRequest = BuildAnthropicRequest(actualEndpoint, apiKey, requestBody);
            var response = await HttpRetryHelper.SendWithRetryAsync(_httpClient, httpRequest, _logger, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("Anthropic API错误: {StatusCode}, {Body}", response.StatusCode, responseBody);
                return new AIQuestionGenerateResponse
                {
                    Success = false,
                    ErrorMessage = $"Anthropic API调用失败: {response.StatusCode}",
                    ErrorCode = ((int)response.StatusCode).ToString()
                };
            }

            var jsonDoc = JsonDocument.Parse(responseBody);
            var contentText = ExtractTextContent(jsonDoc.RootElement);
            if (string.IsNullOrWhiteSpace(contentText))
            {
                return new AIQuestionGenerateResponse
                {
                    Success = false,
                    ErrorMessage = "Anthropic返回空内容",
                    ErrorCode = "EMPTY_RESPONSE"
                };
            }

            if (!AIResponseParser.TryParseQuestions(contentText, out var questions, out var error))
            {
                return new AIQuestionGenerateResponse
                {
                    Success = false,
                    ErrorMessage = error ?? "AI响应解析失败",
                    ErrorCode = "PARSE_ERROR"
                };
            }

            var tokensUsed = TryGetUsageTokens(jsonDoc.RootElement);
            return new AIQuestionGenerateResponse
            {
                Success = true,
                Questions = questions,
                TokensUsed = tokensUsed
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Anthropic生成题目失败");
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
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            var actualEndpoint = string.IsNullOrWhiteSpace(endpoint)
                ? "https://api.anthropic.com/v1/messages"
                : endpoint;
            var modelToUse = string.IsNullOrWhiteSpace(model) ? "claude-3-5-sonnet-latest" : model;

            var requestBody = new
            {
                model = modelToUse,
                max_tokens = 5,
                messages = new[]
                {
                    new
                    {
                        role = "user",
                        content = "hi"
                    }
                }
            };

            var httpRequest = BuildAnthropicRequest(actualEndpoint, apiKey, requestBody);
            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    private static HttpRequestMessage BuildAnthropicRequest(string endpoint, string apiKey, object body)
    {
        var httpRequest = new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(endpoint),
            Content = new StringContent(
                JsonSerializer.Serialize(body),
                System.Text.Encoding.UTF8,
                "application/json")
        };

        httpRequest.Headers.TryAddWithoutValidation("x-api-key", apiKey);
        httpRequest.Headers.TryAddWithoutValidation("anthropic-version", "2023-06-01");
        return httpRequest;
    }

    private static string ExtractTextContent(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("content", out var contentNode) ||
            contentNode.ValueKind != JsonValueKind.Array)
        {
            return string.Empty;
        }

        var textChunks = new List<string>();
        foreach (var item in contentNode.EnumerateArray())
        {
            if (!item.TryGetProperty("type", out var typeNode) ||
                !string.Equals(typeNode.GetString(), "text", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (item.TryGetProperty("text", out var textNode))
            {
                var text = textNode.GetString();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    textChunks.Add(text);
                }
            }
        }

        return string.Join("\n", textChunks);
    }

    private static int? TryGetUsageTokens(JsonElement rootElement)
    {
        if (!rootElement.TryGetProperty("usage", out var usageNode))
        {
            return null;
        }

        var inputTokens = usageNode.TryGetProperty("input_tokens", out var inputNode)
            ? inputNode.GetInt32()
            : 0;
        var outputTokens = usageNode.TryGetProperty("output_tokens", out var outputNode)
            ? outputNode.GetInt32()
            : 0;

        var total = inputTokens + outputTokens;
        return total > 0 ? total : null;
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
}
