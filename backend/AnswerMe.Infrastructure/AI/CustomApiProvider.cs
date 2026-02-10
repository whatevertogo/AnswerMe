using System.Text.Json;
using AnswerMe.Application.AI;
using AnswerMe.Application.DTOs;
using Microsoft.Extensions.Logging;

namespace AnswerMe.Infrastructure.AI;

/// <summary>
/// 自定义 API Provider (OpenAI 兼容格式)
/// </summary>
public class CustomApiProvider : IAIProvider
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<CustomApiProvider> _logger;

    public string ProviderName => "custom_api";

    public CustomApiProvider(IHttpClientFactory httpClientFactory, ILogger<CustomApiProvider> logger)
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

            // 使用用户配置的端点，如果为空则使用默认端点
            var actualEndpoint = string.IsNullOrEmpty(endpoint)
                ? "https://api.openai.com/v1/chat/completions"
                : NormalizeEndpoint(endpoint);

            string NormalizeEndpoint(string ep)
            {
                // 智谱 API 需要完整路径（OpenAI 协议兼容）
                if ((ep.Contains("api/coding/paas/v4") || ep.Contains("api/paas/v4")) &&
                    !ep.EndsWith("/chat/completions"))
                {
                    var normalized = ep.TrimEnd('/');
                    return normalized + "/chat/completions";
                }
                return ep;
            }

            // ✅ 使用配置的模型（如果为空则使用默认模型）
            var modelToUse = string.IsNullOrEmpty(model) ? "gpt-5.2" : model;

            // ✅ 根据题目数量动态计算max_tokens
            // 每道题平均需要约250个tokens（题目+选项+答案+解析）
            // 加上prompt本身的tokens，留一些余量
            var estimatedTokensPerQuestion = 300;
            var maxTokens = Math.Clamp(request.Count * estimatedTokensPerQuestion + 1000, 1000, 8000);

            _logger.LogInformation("AI配置: Endpoint={Endpoint}, Model={Model}, QuestionCount={Count}, MaxTokens={MaxTokens}",
                actualEndpoint.Replace(apiKey.Substring(0, Math.Min(10, apiKey.Length)), "***"),
                modelToUse,
                request.Count,
                maxTokens);

            _logger.LogInformation("发送请求: Endpoint={Endpoint}, MaxTokens={MaxTokens}", actualEndpoint, maxTokens);

            var requestBody = new
            {
                model = modelToUse,
                messages = new[]
                {
                    new
                    {
                        role = "system",
                        content = "你是一个专业的题目生成助手。请根据用户要求和主题生成题目(这很重要),返回JSON格式,返回json格式很重要。"
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

            var requestBodyJson = JsonSerializer.Serialize(requestBody);
            _logger.LogDebug("请求体: {RequestBody}", requestBodyJson.Substring(0, Math.Min(500, requestBodyJson.Length)) + "...");

            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(actualEndpoint),
                Headers =
                {
                    { "Authorization", $"Bearer {apiKey}" }
                },
                Content = new StringContent(requestBodyJson, System.Text.Encoding.UTF8, "application/json")
            };

            // 使用重试机制发送请求（支持指数退避）
            var response = await HttpRetryHelper.SendWithRetryAsync(_httpClient, httpRequest, _logger, cancellationToken);
            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogError("自定义API错误: {StatusCode}, {Body}", response.StatusCode, responseBody);

                return new AIQuestionGenerateResponse
                {
                    Success = false,
                    ErrorMessage = $"自定义API调用失败: {response.StatusCode}",
                    ErrorCode = ((int)response.StatusCode).ToString()
                };
            }

            var jsonDoc = JsonDocument.Parse(responseBody);
            var choices = jsonDoc.RootElement.GetProperty("choices");

            if (choices.GetArrayLength() == 0)
            {
                return new AIQuestionGenerateResponse
                {
                    Success = false,
                    ErrorMessage = "API返回空响应",
                    ErrorCode = "EMPTY_RESPONSE"
                };
            }

            var content = choices[0].GetProperty("message").GetProperty("content").GetString();
            var contentText = content ?? string.Empty;

            if (!AIResponseParser.TryParseQuestions(contentText, out var questions, out var error))
            {
                _logger.LogWarning("自定义API响应解析失败: {Error}", error);
                return new AIQuestionGenerateResponse
                {
                    Success = false,
                    ErrorMessage = error ?? "AI响应解析失败",
                    ErrorCode = "PARSE_ERROR"
                };
            }

            return new AIQuestionGenerateResponse
            {
                Success = true,
                Questions = questions,
                TokensUsed = jsonDoc.RootElement.TryGetProperty("usage", out var usage)
                    ? (int?)usage.GetProperty("total_tokens").GetInt64()
                    : null
            };
        }
        catch (HttpRequestException ex)
        {
            _logger.LogError(ex, "自定义API网络请求失败");
            return new AIQuestionGenerateResponse
            {
                Success = false,
                ErrorMessage = "网络请求失败，请检查端点地址",
                ErrorCode = "NETWORK_ERROR"
            };
        }
        catch (JsonException ex)
        {
            _logger.LogError(ex, "解析自定义API响应失败");
            return new AIQuestionGenerateResponse
            {
                Success = false,
                ErrorMessage = "API响应格式错误",
                ErrorCode = "PARSE_ERROR"
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "自定义API生成题目失败");
            return new AIQuestionGenerateResponse
            {
                Success = false,
                ErrorMessage = ex.Message,
                ErrorCode = "UNKNOWN_ERROR"
            };
        }
    }

    public async Task<bool> ValidateApiKeyAsync(
        string apiKey,
        string? endpoint = null,
        string? model = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(apiKey))
        {
            return false;
        }

        if (string.IsNullOrEmpty(endpoint))
        {
            // 无端点时无法验证，自定义API需要端点
            return true;
        }

        try
        {
            var actualEndpoint = NormalizeEndpoint(endpoint);
            var modelToUse = string.IsNullOrEmpty(model) ? "gpt-3.5-turbo" : model;

            var requestBody = new
            {
                model = modelToUse,
                messages = new[]
                {
                    new { role = "user", content = "hi" }
                },
                max_tokens = 5
            };

            var httpRequest = new HttpRequestMessage
            {
                Method = HttpMethod.Post,
                RequestUri = new Uri(actualEndpoint),
                Headers =
                {
                    { "Authorization", $"Bearer {apiKey}" }
                },
                Content = new StringContent(
                    JsonSerializer.Serialize(requestBody),
                    System.Text.Encoding.UTF8,
                    "application/json")
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }

        string NormalizeEndpoint(string ep)
        {
            if ((ep.Contains("api/coding/paas/v4") || ep.Contains("api/paas/v4")) &&
                !ep.EndsWith("/chat/completions"))
            {
                var normalized = ep.TrimEnd('/');
                return normalized + "/chat/completions";
            }
            return ep;
        }
    }

    private string BuildPrompt(AIQuestionGenerateRequest request)
    {
        var prompt = $"请生成{request.Count}道关于\"{request.Subject}\"的题目。\n\n";
        prompt += $"难度等级：{request.Difficulty}\n";
        prompt += $"题目类型：{string.Join("、", request.QuestionTypes)}\n";

        if (!string.IsNullOrEmpty(request.CustomPrompt))
        {
            prompt += $"特殊要求：{request.CustomPrompt}\n";
        }

        prompt += "\n请以JSON数组格式返回，每道题包含以下字段：\n";
        prompt += "- questionType: 题型\n";
        prompt += "- questionText: 题目内容\n";
        prompt += "- options: 选项（数组）\n";
        prompt += "- correctAnswer: 正确答案\n";
        prompt += "- explanation: 解析（可选）\n";
        prompt += "- difficulty: 难度\n\n";
        prompt += "只返回JSON数组，不要有其他内容。";

        return prompt;
    }

    // 解析逻辑统一由 AIResponseParser 处理
}
