using AnswerMe.Application.DTOs;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// 题目服务接口
/// </summary>
public interface IQuestionService
{
    /// <summary>
    /// 创建题目
    /// </summary>
    Task<QuestionDto> CreateAsync(int userId, CreateQuestionDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取题目列表(游标分页)
    /// </summary>
    Task<QuestionListDto> GetListAsync(int userId, QuestionListQueryDto query, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取题目
    /// </summary>
    Task<QuestionDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新题目
    /// </summary>
    Task<QuestionDto?> UpdateAsync(int id, int userId, UpdateQuestionDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除题目
    /// </summary>
    Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 搜索题目
    /// </summary>
    Task<List<QuestionDto>> SearchAsync(int questionBankId, int userId, string searchTerm, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量创建题目
    /// </summary>
    Task<List<QuestionDto>> CreateBatchAsync(int userId, List<CreateQuestionDto> dtos, CancellationToken cancellationToken = default);

    /// <summary>
    /// 批量删除题目
    /// </summary>
    Task<(int successCount, int notFoundCount)> DeleteBatchAsync(int userId, List<int> ids, CancellationToken cancellationToken = default);
}
