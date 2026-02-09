using System.Text.Json;
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

        return await MapToDtoAsync(questionBank, cancellationToken);
    }

    public async Task<QuestionBankListDto> GetListAsync(int userId, QuestionBankListQueryDto query, CancellationToken cancellationToken = default)
    {
        var questionBanks = await _questionBankRepository.GetPagedAsync(
            userId,
            query.PageSize,
            query.LastId,
            cancellationToken);

        var result = new List<QuestionBankDto>();
        foreach (var qb in questionBanks)
        {
            result.Add(await MapToDtoAsync(qb, cancellationToken));
        }

        // 判断是否有更多数据
        var hasMore = questionBanks.Count == query.PageSize;
        int? nextCursor = hasMore ? questionBanks.LastOrDefault()?.Id : (int?)null;

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

        return await MapToDtoAsync(questionBank, cancellationToken);
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
        if (questionBank.Version == null || questionBank.Version.Length < 8)
        {
            questionBank.Version = new byte[8];
        }

        var version = BitConverter.ToInt64(questionBank.Version);
        BitConverter.GetBytes(version + 1).CopyTo(questionBank.Version, 0);

        questionBank.UpdatedAt = DateTime.UtcNow;

        await _questionBankRepository.SaveChangesAsync(cancellationToken);

        return await MapToDtoAsync(questionBank, cancellationToken);
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
        var result = new List<QuestionBankDto>();

        foreach (var qb in questionBanks)
        {
            result.Add(await MapToDtoAsync(qb, cancellationToken));
        }

        return result;
    }

    private async Task<QuestionBankDto> MapToDtoAsync(Domain.Entities.QuestionBank questionBank, CancellationToken cancellationToken)
    {
        // 获取题目数量
        var questions = await _questionRepository.GetByQuestionBankIdAsync(questionBank.Id, cancellationToken);

        // 解析Tags
        List<string> tags = new();
        if (!string.IsNullOrEmpty(questionBank.Tags))
        {
            try
            {
                tags = JsonSerializer.Deserialize<List<string>>(questionBank.Tags) ?? new();
            }
            catch
            {
                tags = new();
            }
        }

        return new QuestionBankDto
        {
            Id = questionBank.Id,
            UserId = questionBank.UserId,
            Name = questionBank.Name,
            Description = questionBank.Description,
            Tags = tags,
            DataSourceId = questionBank.DataSourceId,
            DataSourceName = questionBank.DataSource?.Name,
            QuestionCount = questions.Count,
            Version = questionBank.Version,
            CreatedAt = questionBank.CreatedAt,
            UpdatedAt = questionBank.UpdatedAt
        };
    }
}
