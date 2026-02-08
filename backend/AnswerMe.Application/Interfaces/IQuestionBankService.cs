using AnswerMe.Application.DTOs;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// 题库服务接口
/// </summary>
public interface IQuestionBankService
{
    /// <summary>
    /// 创建题库
    /// </summary>
    Task<QuestionBankDto> CreateAsync(int userId, CreateQuestionBankDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取题库列表（游标分页）
    /// </summary>
    Task<QuestionBankListDto> GetListAsync(int userId, QuestionBankListQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取题库
    /// </summary>
    Task<QuestionBankDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新题库
    /// </summary>
    Task<QuestionBankDto?> UpdateAsync(int id, int userId, UpdateQuestionBankDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除题库
    /// </summary>
    Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索题库
    /// </summary>
    Task<List<QuestionBankDto>> SearchAsync(int userId, string searchTerm, CancellationToken cancellationToken = default);
}
