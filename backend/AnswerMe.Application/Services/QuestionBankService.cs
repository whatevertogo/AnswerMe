using System.Text.Json;
using AnswerMe.Application.Common;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Interfaces;

namespace AnswerMe.Application.Services;

/// <summary>
/// 题库服务实现
/// </summary>
public class QuestionBankService : IQuestionBankService
{
    private readonly IQuestionBankRepository _questionBankRepository;
    private readonly IQuestionRepository _questionRepository;

    public QuestionBankService(
        IQuestionBankRepository questionBankRepository,
        IQuestionRepository questionRepository)
    {
        _questionBankRepository = questionBankRepository;
        _questionRepository = questionRepository;
    }

    public async Task<QuestionBankDto> CreateAsync(int userId, CreateQuestionBankDto dto, CancellationToken cancellationToken = default)
    {
        // 检查名称是否重复
        if (await _questionBankRepository.ExistsByNameAsync(userId, dto.Name, cancellationToken))
        {
            throw new InvalidOperationException($"题库名称 '{dto.Name}' 已存在");
        }

        var questionBank = new Domain.Entities.QuestionBank
        {
            UserId = userId,
            Name = dto.Name,
            Description = dto.Description,
            Tags = JsonSerializer.Serialize(dto.Tags),
            DataSourceId = dto.DataSourceId,
            Version = new byte[8], // 初始化版本号
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _questionBankRepository.AddAsync(questionBank, cancellationToken);
        await _questionBankRepository.SaveChangesAsync(cancellationToken);

        return await questionBank.ToDtoAsync(_questionRepository, cancellationToken);
    }

    public async Task<QuestionBankListDto> GetListAsync(int userId, QuestionBankListQueryDto query, CancellationToken cancellationToken = default)
    {
        var questionBanks = await _questionBankRepository.GetPagedAsync(
            userId,
            query.PageSize,
            query.LastId,
            cancellationToken);

        var hasMore = questionBanks.Count > query.PageSize;
        var pageItems = hasMore ? questionBanks.Take(query.PageSize).ToList() : questionBanks;
        var result = await pageItems.ToDtoListAsync(_questionRepository, cancellationToken);

        // 判断是否有更多数据
        int? nextCursor = hasMore ? pageItems.LastOrDefault()?.Id : (int?)null;

        return new QuestionBankListDto
        {
            Data = result,
            HasMore = hasMore,
            NextCursor = nextCursor
        };
    }

    public async Task<QuestionBankDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var questionBank = await _questionBankRepository.GetByIdAsync(id, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            return null;
        }

        return await questionBank.ToDtoAsync(_questionRepository, cancellationToken);
    }

    public async Task<QuestionBankDto?> UpdateAsync(int id, int userId, UpdateQuestionBankDto dto, CancellationToken cancellationToken = default)
    {
        var questionBank = await _questionBankRepository.GetByIdAsync(id, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            return null;
        }

        // 乐观锁检查 - 强制要求版本号
        if (dto.Version == null || !questionBank.Version.SequenceEqual(dto.Version))
        {
            throw new InvalidOperationException("题库已被其他用户修改，请刷新后重试");
        }

        // 检查名称重复
        if (!string.IsNullOrEmpty(dto.Name) && dto.Name != questionBank.Name)
        {
            if (await _questionBankRepository.ExistsByNameAsync(userId, dto.Name, cancellationToken))
            {
                throw new InvalidOperationException($"题库名称 '{dto.Name}' 已存在");
            }
            questionBank.Name = dto.Name;
        }

        if (dto.Description != null)
        {
            questionBank.Description = dto.Description;
        }

        if (dto.Tags != null)
        {
            questionBank.Tags = JsonSerializer.Serialize(dto.Tags);
        }

        if (dto.DataSourceId.HasValue)
        {
            questionBank.DataSourceId = dto.DataSourceId.Value;
        }

        // 更新版本号 - 确保数组长度正确
        IncrementVersion(questionBank);

        questionBank.UpdatedAt = DateTime.UtcNow;

        await _questionBankRepository.SaveChangesAsync(cancellationToken);

        return await questionBank.ToDtoAsync(_questionRepository, cancellationToken);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var questionBank = await _questionBankRepository.GetByIdAsync(id, cancellationToken);
        if (questionBank == null || questionBank.UserId != userId)
        {
            return false;
        }

        await _questionBankRepository.DeleteAsync(questionBank, cancellationToken);
        await _questionBankRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<List<QuestionBankDto>> SearchAsync(int userId, string searchTerm, CancellationToken cancellationToken = default)
    {
        var questionBanks = await _questionBankRepository.SearchAsync(userId, searchTerm, cancellationToken);
        return await questionBanks.ToDtoListAsync(_questionRepository, cancellationToken);
    }

    private static void IncrementVersion(Domain.Entities.QuestionBank questionBank)
    {
        if (questionBank.Version == null || questionBank.Version.Length < 8)
        {
            questionBank.Version = new byte[8];
        }

        var currentVersion = BitConverter.ToInt64(questionBank.Version);
        BitConverter.GetBytes(currentVersion + 1).CopyTo(questionBank.Version, 0);
    }
}
