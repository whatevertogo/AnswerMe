using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Serilog;

namespace AnswerMe.API.Filters;

/// <summary>
/// 全局异常处理过滤器
/// 统一处理所有未捕获的异常,返回标准化的错误响应
/// </summary>
public class GlobalExceptionFilter : IExceptionFilter
{
    private readonly Serilog.ILogger _logger;
    private readonly IWebHostEnvironment _environment;

    public GlobalExceptionFilter(IWebHostEnvironment environment)
    {
        _environment = environment;
        _logger = Serilog.Log.ForContext<GlobalExceptionFilter>();
    }

    public void OnException(ExceptionContext context)
    {
        // 记录异常详情(Serilog自动包含堆栈跟踪)
        _logger.Error(context.Exception,
            "未处理的异常: {ExceptionType} - {Message} | 路径: {Path} | 方法: {Method}",
            context.Exception.GetType().Name,
            context.Exception.Message,
            context.HttpContext.Request.Path,
            context.HttpContext.Request.Method);

        // 根据异常类型确定HTTP状态码和错误消息
        var (statusCode, title, message) = GetErrorInfo(context.Exception);

        // 构建错误响应
        var errorResponse = new ErrorResponse
        {
            Error = new ErrorDetail
            {
                Title = title,
                Message = message,
                StatusCode = statusCode
            }
        };

        // 仅在开发环境暴露堆栈跟踪
        if (_environment.IsDevelopment())
        {
            errorResponse.Error.StackTrace = context.Exception.StackTrace;
            errorResponse.Error.InnerException = context.Exception.InnerException?.Message;
            errorResponse.Error.ExceptionType = context.Exception.GetType().Name;
        }

        context.Result = new ObjectResult(errorResponse)
        {
            StatusCode = statusCode
        };

        context.ExceptionHandled = true;
    }

    /// <summary>
    /// 根据异常类型获取错误信息
    /// </summary>
    private (int statusCode, string title, string message) GetErrorInfo(Exception exception)
    {
        // 特殊处理:速率限制异常
        if (exception is InvalidOperationException ex && ex.Message.Contains("速率"))
        {
            return (
                StatusCodes.Status429TooManyRequests,
                "Too Many Requests",
                "请求过于频繁,请稍后重试");
        }

        // 根据异常类型确定HTTP状态码和错误消息
        return exception switch
        {
            // 400 Bad Request - 客户端请求错误
            ArgumentException _ => (
                StatusCodes.Status400BadRequest,
                "Invalid Argument",
                exception.Message),

            // 401 Unauthorized - 未认证
            UnauthorizedAccessException _ => (
                StatusCodes.Status401Unauthorized,
                "Unauthorized",
                "邮箱或密码错误"),

            // 404 Not Found - 资源不存在
            KeyNotFoundException _ => (
                StatusCodes.Status404NotFound,
                "Resource Not Found",
                exception.Message),

            // 422 Unprocessable Entity - 请求格式正确但语义错误
            InvalidOperationException _ => (
                StatusCodes.Status422UnprocessableEntity,
                "Invalid Operation",
                exception.Message),

            // 500 Internal Server Error - 服务器错误
            _ => (
                StatusCodes.Status500InternalServerError,
                "Internal Server Error",
                _environment.IsDevelopment()
                    ? exception.Message
                    : "服务器内部错误,请稍后重试")
        };
    }
}

/// <summary>
/// 标准错误响应格式
/// </summary>
public class ErrorResponse
{
    public ErrorDetail Error { get; set; } = null!;
}

/// <summary>
/// 错误详情
/// </summary>
public class ErrorDetail
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }
}
