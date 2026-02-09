using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using AnswerMe.API.DTOs;
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

        // 构建错误响应（使用Create方法，自动支持两种格式）
        var errorDetail = ErrorDetail.Create(message, statusCode, null, title);
        errorDetail.ExceptionType = context.Exception.GetType().Name;

        // 仅在开发环境暴露堆栈跟踪
        if (_environment.IsDevelopment())
        {
            errorDetail.StackTrace = context.Exception.StackTrace;
            errorDetail.InnerException = context.Exception.InnerException?.Message;
        }

        var errorResponse = new ErrorResponse
        {
            Error = errorDetail,
            // 同时设置扁平化属性以保持前端兼容性
            Message = message,
            StatusCode = statusCode
        };

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
