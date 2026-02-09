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

    public CustomApiProvider(HttpClient httpClient, ILogger<CustomApiProvider> logger)
    {
        _httpClient = httpClient;
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
                // 如果端点包含智谱的 API 路径但没有 /chat/completions，自动添加
                if (ep.Contains("api/coding/paas/v4") || ep.Contains("api/paas/v4"))
                {
                    if (!ep.EndsWith("/chat/completions"))
                    {
                        // 移除末尾的斜杠（如果有）然后添加完整路径
                        var normalized = ep.TrimEnd('/');
                        _logger.LogInformation("检测到智谱API端点，自动补全路径: {From} -> {To}", ep, normalized + "/chat/completions");
                        return normalized + "/chat/completions";
                    }
                }
                return ep;
            }

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
                    model = "gpt-3.5-turbo", // 默认模型，用户可以在 DataSource 配置中覆盖
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
                    max_tokens = 4000
                }), System.Text.Encoding.UTF8, "application/json")
            };

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
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
            var questions = ParseQuestionsFromResponse(content ?? "");

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

    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        // 自定义 API 的验证需要端点信息，但 ValidateApiKeyAsync 接口没有提供端点参数
        // 所以我们暂时返回 true，实际验证会在生成题目时进行
        // 更好的方案是修改接口以支持端点参数
        await Task.CompletedTask;
        return !string.IsNullOrEmpty(apiKey);
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

    private List<GeneratedQuestion> ParseQuestionsFromResponse(string content)
    {
        try
        {
            // 尝试提取 JSON（可能包含在代码块中）
            var jsonMatch = System.Text.RegularExpressions.Regex.Match(content, @"\[\s*\{.*\}\s*\]", System.Text.RegularExpressions.RegexOptions.Singleline);
            if (jsonMatch.Success)
            {
                content = jsonMatch.Value;
            }

            var questions = JsonSerializer.Deserialize<List<GeneratedQuestion>>(content);
            return questions ?? new List<GeneratedQuestion>();
        }
        catch (JsonException)
        {
            // 如果解析失败，返回空列表
            return new List<GeneratedQuestion>();
        }
    }
}
