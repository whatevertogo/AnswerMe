namespace AnswerMe.API.DTOs;

/// <summary>
/// 标准错误响应格式（支持前端兼容性）
/// </summary>
public class ErrorResponse
{
    /// <summary>
    /// 嵌套错误详情（标准格式）
    /// </summary>
    public ErrorDetail? Error { get; set; }

    /// <summary>
    /// 扁平化错误消息（前端兼容性）
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// HTTP状态码（前端兼容性）
    /// </summary>
    public int? StatusCode { get; set; }

    /// <summary>
    /// 创建标准错误响应
    /// </summary>
    public static ErrorResponse Create(string message, int statusCode, string? errorCode = null, string? title = null)
    {
        var errorDetail = ErrorDetail.Create(message, statusCode, errorCode, title);

        return new ErrorResponse
        {
            Error = errorDetail,
            // 同时设置扁平化属性以保持前端兼容性
            Message = message,
            StatusCode = statusCode
        };
    }
}

/// <summary>
/// 错误详情
/// </summary>
public class ErrorDetail
{
    public string Title { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
    public int StatusCode { get; set; }
    public string? ErrorCode { get; set; }
    public string? ExceptionType { get; set; }
    public string? StackTrace { get; set; }
    public string? InnerException { get; set; }

    /// <summary>
    /// 创建错误详情
    /// </summary>
    public static ErrorDetail Create(string message, int statusCode, string? errorCode = null, string? title = null)
    {
        return new ErrorDetail
        {
            Message = message,
            StatusCode = statusCode,
            ErrorCode = errorCode,
            Title = title ?? GetDefaultTitle(statusCode)
        };
    }

    /// <summary>
    /// 根据状态码获取默认标题
    /// </summary>
    private static string GetDefaultTitle(int statusCode)
    {
        return statusCode switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            422 => "Unprocessable Entity",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            _ => "Error"
        };
    }
}
