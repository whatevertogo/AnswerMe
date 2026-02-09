using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;

namespace AnswerMe.Application.Authorization;

/// <summary>
/// 资源授权服务接口
/// </summary>
public interface IResourceAuthorizationService
{
    /// <summary>
    /// 验证用户是否有权访问题库，如无权则抛出异常
    /// </summary>
    Task<QuestionBank> RequireQuestionBankAccessAsync(int questionBankId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证用户是否有权访问题库，如无权则返回 null
    /// </summary>
    Task<QuestionBank?> GetQuestionBankIfAccessibleAsync(int questionBankId, int userId, CancellationToken cancellationToken = default);
}

/// <summary>
/// 资源授权服务实现
/// </summary>
public class ResourceAuthorizationService : IResourceAuthorizationService
{
    private readonly IQuestionBankRepository _questionBankRepository;

    public ResourceAuthorizationService(IQuestionBankRepository questionBankRepository)
    {
        _questionBankRepository = questionBankRepository;
    }

    public async Task<QuestionBank> RequireQuestionBankAccessAsync(int questionBankId, int userId, CancellationToken cancellationToken = default)
    {
        var questionBank = await _questionBankRepository.GetByIdAsync(questionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            throw new InvalidOperationException("题库不存在或无权访问");
        }
        return questionBank;
    }

    public async Task<QuestionBank?> GetQuestionBankIfAccessibleAsync(int questionBankId, int userId, CancellationToken cancellationToken = default)
    {
        var questionBank = await _questionBankRepository.GetByIdAsync(questionBankId, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            return null;
        }
        return questionBank;
    }
}
