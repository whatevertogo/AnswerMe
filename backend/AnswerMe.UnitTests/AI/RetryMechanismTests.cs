using System.Net;
using AnswerMe.Application.AI;
using AnswerMe.UnitTests.Helpers;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.Protected;
using Xunit;

namespace AnswerMe.UnitTests.AI;

/// <summary>
/// HTTP 重试机制的单元测试
/// 测试覆盖率目标: 100%
/// </summary>
public class RetryMechanismTests
{
    private readonly Mock<HttpMessageHandler> _mockHandler;
    private readonly Mock<ILogger<RetryTestHelper>> _mockLogger;
    private readonly HttpClient _httpClient;
    private readonly ILogger<RetryTestHelper> _logger;

    public RetryMechanismTests()
    {
        _mockHandler = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        _mockLogger = new Mock<ILogger<RetryTestHelper>>();
        _httpClient = new HttpClient(_mockHandler.Object);
        _logger = _mockLogger.Object;
    }

    #region 429 TooManyRequests - Should Trigger Retry

    [Fact]
    public async Task SendWithRetryAsync_When429TooManyRequests_ShouldRetry3Times()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");

        // 设置 Mock: 前 3 次返回 429,第 4 次返回 200
        var callCount = 0;
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                if (callCount <= 3)
                {
                    return new HttpResponseMessage(HttpStatusCode.TooManyRequests)
                    {
                        Content = new StringContent("{\"error\": \"rate_limit_exceeded\"}")
                    };
                }
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                };
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var response = await helper.SendWithRetryAsync(
            request,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(10), // 加快测试
            CancellationToken.None);

        // Assert
        callCount.Should().Be(4, "应该发起 4 次请求(1 次初始 + 3 次重试)");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Fact]
    public async Task SendWithRetryAsync_When429_ShouldUseExponentialBackoff()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var delays = new List<TimeSpan>();

        // 使用 Mock 机制测量延迟时间
        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                delays.Add(TimeSpan.FromMilliseconds(100)); // 占位符
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var startTime = DateTime.UtcNow;
        try
        {
            await helper.SendWithRetryAsync(
                request,
                maxRetries: 2,
                baseDelay: TimeSpan.FromMilliseconds(100),
                CancellationToken.None);
        }
        catch (HttpRequestException)
        {
            // 预期会在重试用尽后抛出异常
        }
        var elapsed = DateTime.UtcNow - startTime;

        // Assert
        // 指数退避: 100ms + 200ms = 300ms (至少)
        elapsed.Should().BeGreaterThanOrEqualTo(TimeSpan.FromMilliseconds(290));
        elapsed.Should().BeLessThan(TimeSpan.FromMilliseconds(500)); // 不应该太长
    }

    [Fact]
    public async Task SendWithRetryAsync_When429_ShouldLogWarning()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        try
        {
            await helper.SendWithRetryAsync(
                request,
                maxRetries: 1,
                baseDelay: TimeSpan.FromMilliseconds(10),
                CancellationToken.None);
        }
        catch { }

        // Assert
        _mockLogger.Verify(
            x => x.Log(
                LogLevel.Warning,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("请求失败") && v.ToString()!.Contains("重试")),
                It.IsAny<Exception>(),
                It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
            Times.AtLeastOnce,
            "应该记录重试警告日志");
    }

    #endregion

    #region 503 ServiceUnavailable - Should Trigger Retry

    [Fact]
    public async Task SendWithRetryAsync_When503ServiceUnavailable_ShouldRetry()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.ServiceUnavailable)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                };
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var response = await helper.SendWithRetryAsync(
            request,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        callCount.Should().Be(2);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region 504 GatewayTimeout - Should Trigger Retry

    [Fact]
    public async Task SendWithRetryAsync_When504GatewayTimeout_ShouldRetry()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(HttpStatusCode.GatewayTimeout)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                };
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var response = await helper.SendWithRetryAsync(
            request,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        callCount.Should().Be(2);
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region 200 OK - Should NOT Retry

    [Fact]
    public async Task SendWithRetryAsync_When200OK_ShouldReturnImmediately()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                };
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var response = await helper.SendWithRetryAsync(
            request,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        callCount.Should().Be(1, "成功响应不应该触发重试");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    #endregion

    #region 400 Bad Request - Should NOT Retry

    [Fact]
    public async Task SendWithRetryAsync_When400BadRequest_ShouldNotRetry()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.BadRequest)
                {
                    Content = new StringContent("{\"error\": \"invalid_request\"}")
                };
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var response = await helper.SendWithRetryAsync(
            request,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        callCount.Should().Be(1, "客户端错误不应该触发重试");
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    #endregion

    #region 500 Internal Server Error - Should NOT Retry

    [Fact]
    public async Task SendWithRetryAsync_When500InternalServerError_ShouldNotRetry()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.InternalServerError)
                {
                    Content = new StringContent("{\"error\": \"internal_error\"}")
                };
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var response = await helper.SendWithRetryAsync(
            request,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        callCount.Should().Be(1, "服务器内部错误不应该触发重试");
        response.StatusCode.Should().Be(HttpStatusCode.InternalServerError);
    }

    #endregion

    #region Max Retries Exceeded - Should Throw Exception

    [Fact]
    public async Task SendWithRetryAsync_WhenRetriesExceeded_ShouldThrowException()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage(HttpStatusCode.TooManyRequests));

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var act = async () => await helper.SendWithRetryAsync(
            request,
            maxRetries: 2,
            baseDelay: TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        await act.Should().ThrowExactlyAsync<HttpRequestException>()
            .WithMessage("*重试次数已用尽*");
    }

    [Fact]
    public async Task SendWithRetryAsync_WhenRetriesExceeded_ShouldMakeCorrectNumberOfCalls()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        try
        {
            await helper.SendWithRetryAsync(
                request,
                maxRetries: 3,
                baseDelay: TimeSpan.FromMilliseconds(10),
                CancellationToken.None);
        }
        catch { }

        // Assert
        // 初始请求 + 3 次重试 = 4 次调用
        callCount.Should().Be(4);
    }

    #endregion

    #region CancellationToken - Should Cancel Retry

    [Fact]
    public async Task SendWithRetryAsync_WhenCanceled_ShouldStopRetrying()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var cts = new CancellationTokenSource();
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                // 在第二次调用时取消
                if (callCount == 2)
                {
                    cts.Cancel();
                }
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var act = async () => await helper.SendWithRetryAsync(
            request,
            maxRetries: 5,
            baseDelay: TimeSpan.FromMilliseconds(10),
            cts.Token);

        // Assert
        await act.Should().ThrowAsync<OperationCanceledException>();
        callCount.Should().BeLessThanOrEqualTo(2);
    }

    #endregion

    #region Mixed Status Codes - Should Retry Only Appropriate Codes

    [Theory]
    [InlineData(HttpStatusCode.TooManyRequests)]      // 429 - 应该重试
    [InlineData(HttpStatusCode.ServiceUnavailable)]  // 503 - 应该重试
    [InlineData(HttpStatusCode.GatewayTimeout)]      // 504 - 应该重试
    public async Task SendWithRetryAsync_ShouldRetrySpecificStatusCodes(HttpStatusCode statusCode)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return callCount == 1
                    ? new HttpResponseMessage(statusCode)
                    : new HttpResponseMessage(HttpStatusCode.OK)
                {
                    Content = new StringContent("{\"result\": \"success\"}")
                };
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var response = await helper.SendWithRetryAsync(
            request,
            maxRetries: 2,
            baseDelay: TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        callCount.Should().Be(2, $"{statusCode} 应该触发重试");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
    }

    [Theory]
    [InlineData(HttpStatusCode.BadRequest)]           // 400 - 不应该重试
    [InlineData(HttpStatusCode.Unauthorized)]         // 401 - 不应该重试
    [InlineData(HttpStatusCode.Forbidden)]            // 403 - 不应该重试
    [InlineData(HttpStatusCode.NotFound)]             // 404 - 不应该重试
    [InlineData(HttpStatusCode.InternalServerError)] // 500 - 不应该重试
    [InlineData(HttpStatusCode.BadGateway)]           // 502 - 不应该重试
    public async Task SendWithRetryAsync_ShouldNotRetryOtherStatusCodes(HttpStatusCode statusCode)
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(statusCode);
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        var response = await helper.SendWithRetryAsync(
            request,
            maxRetries: 3,
            baseDelay: TimeSpan.FromMilliseconds(10),
            CancellationToken.None);

        // Assert
        callCount.Should().Be(1, $"{statusCode} 不应该触发重试");
        response.StatusCode.Should().Be(statusCode);
    }

    #endregion

    #region Zero MaxRetries - Should Not Retry

    [Fact]
    public async Task SendWithRetryAsync_WhenMaxRetriesIsZero_ShouldNotRetry()
    {
        // Arrange
        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.test.com/generate");
        var callCount = 0;

        _mockHandler
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(() =>
            {
                callCount++;
                return new HttpResponseMessage(HttpStatusCode.TooManyRequests);
            });

        // Act
        var helper = new RetryTestHelper(_httpClient, _logger);
        try
        {
            await helper.SendWithRetryAsync(
                request,
                maxRetries: 0,
                baseDelay: TimeSpan.FromMilliseconds(10),
                CancellationToken.None);
        }
        catch { }

        // Assert
        callCount.Should().Be(1, "maxRetries=0 时不应该重试");
    }

    #endregion
}

#region Test Helper Class

/// <summary>
/// 测试辅助类 - 实现重试逻辑
/// 在实际的 AI Provider 中应该使用相同的实现
/// </summary>
public class RetryTestHelper
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<RetryTestHelper> _logger;

    public RetryTestHelper(HttpClient httpClient, ILogger<RetryTestHelper> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// 带重试机制的 HTTP 请求发送
    /// 这是 AI Provider 应该使用的实现
    /// </summary>
    public async Task<HttpResponseMessage> SendWithRetryAsync(
        HttpRequestMessage request,
        int maxRetries,
        TimeSpan baseDelay,
        CancellationToken cancellationToken)
    {
        for (int attempt = 0; attempt <= maxRetries; attempt++)
        {
            // 克隆请求以支持重试
            var requestClone = CloneHttpRequestMessage(request);

            var response = await _httpClient.SendAsync(requestClone, cancellationToken);

            // 检查可重试的状态码
            if (response.StatusCode == HttpStatusCode.TooManyRequests ||   // 429
                response.StatusCode == HttpStatusCode.ServiceUnavailable || // 503
                response.StatusCode == HttpStatusCode.GatewayTimeout)       // 504
            {
                if (attempt < maxRetries)
                {
                    var delay = baseDelay * Math.Pow(2, attempt);  // 指数退避
                    _logger.LogWarning("请求失败 {StatusCode}, {Delay:F1}ms 后重试...",
                        response.StatusCode, delay.TotalMilliseconds);

                    try
                    {
                        await Task.Delay(delay, cancellationToken);
                    }
                    catch (OperationCanceledException)
                    {
                        response.Dispose();
                        throw;
                    }

                    continue;
                }
            }

            return response;
        }

        throw new HttpRequestException("重试次数已用尽");
    }

    private static HttpRequestMessage CloneHttpRequestMessage(HttpRequestMessage request)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Content = request.Content != null
                ? new StringContent(
                    request.Content.ReadAsStringAsync().GetAwaiter().GetResult(),
                    System.Text.Encoding.UTF8,
                    request.Content?.Headers.ContentType?.MediaType)
                : null
        };

        foreach (var (key, value) in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(key, value);
        }

        return clone;
    }
}

#endregion
