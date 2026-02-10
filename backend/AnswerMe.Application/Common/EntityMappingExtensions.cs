using AnswerMe.Application.DTOs;
using AnswerMe.Domain.Entities;
using AnswerMe.Domain.Interfaces;

namespace AnswerMe.Application.Common;

/// <summary>
/// 实体到 DTO 的映射扩展方法
/// 提供简洁、类型安全的映射方式
/// </summary>
public static class EntityMappingExtensions
{
    /// <summary>
    /// 将 QuestionBank 实体映射为 DTO (异步，包含题目计数)
    /// </summary>
    public static async Task<QuestionBankDto> ToDtoAsync(
        this QuestionBank entity,
        IQuestionRepository questionRepository,
        CancellationToken cancellationToken = default)
    {
        // 获取题目数量
        var questions = await questionRepository.GetByQuestionBankIdAsync(entity.Id, cancellationToken);

        // 解析 Tags
        var tags = ParseTags(entity.Tags);

        return new QuestionBankDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            Description = entity.Description,
            Tags = tags,
            DataSourceId = entity.DataSourceId,
            DataSourceName = entity.DataSource?.Name,
            QuestionCount = questions.Count,
            Version = entity.Version ?? Array.Empty<byte>(),
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 将 Question 实体映射为 DTO
    /// 只映射新字段，旧字段由 Data 属性提供
    /// </summary>
    public static QuestionDto ToDto(this Question entity)
    {
        return new QuestionDto
        {
            Id = entity.Id,
            QuestionBankId = entity.QuestionBankId,
            QuestionBankName = entity.QuestionBank?.Name ?? string.Empty,
            QuestionText = entity.QuestionText,
            QuestionTypeEnum = entity.QuestionTypeEnum,
            Data = entity.Data,
            Explanation = entity.Explanation,
            Difficulty = entity.Difficulty,
            OrderIndex = entity.OrderIndex,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 将 DataSource 实体映射为 DTO
    /// </summary>
    public static DataSourceDto ToDto(this DataSource entity)
    {
        var config = System.Text.Json.JsonSerializer.Deserialize<DataSourceConfig>(entity.Config);

        return new DataSourceDto
        {
            Id = entity.Id,
            UserId = entity.UserId,
            Name = entity.Name,
            Type = entity.Type,
            Endpoint = config?.Endpoint,
            Model = config?.Model,
            MaskedApiKey = MaskApiKey(config?.ApiKey ?? ""),
            IsDefault = entity.IsDefault,
            CreatedAt = entity.CreatedAt,
            UpdatedAt = entity.UpdatedAt
        };
    }

    /// <summary>
    /// 批量映射 Question 实体为 DTO
    /// </summary>
    public static List<QuestionDto> ToDtoList(this IEnumerable<Question> entities)
    {
        return entities.Select(ToDto).ToList();
    }

    /// <summary>
    /// 批量映射 QuestionBank 实体为 DTO (异步)
    /// 优化：使用批量统计避免 N+1 查询
    /// </summary>
    public static async Task<List<QuestionBankDto>> ToDtoListAsync(
        this IEnumerable<QuestionBank> entities,
        IQuestionRepository questionRepository,
        CancellationToken cancellationToken = default)
    {
        var entityList = entities.ToList();
        if (entityList.Count == 0)
        {
            return new List<QuestionBankDto>();
        }

        // 批量获取题目数量
        var bankIds = entityList.Select(e => e.Id).ToList();
        var countMap = await questionRepository.CountByQuestionBankIdsAsync(bankIds, cancellationToken);

        var result = new List<QuestionBankDto>();

        foreach (var entity in entityList)
        {
            var tags = ParseTags(entity.Tags);

            result.Add(new QuestionBankDto
            {
                Id = entity.Id,
                UserId = entity.UserId,
                Name = entity.Name,
                Description = entity.Description,
                Tags = tags,
                DataSourceId = entity.DataSourceId,
                DataSourceName = entity.DataSource?.Name,
                QuestionCount = countMap.GetValueOrDefault(entity.Id, 0),
                Version = entity.Version ?? Array.Empty<byte>(),
                CreatedAt = entity.CreatedAt,
                UpdatedAt = entity.UpdatedAt
            });
        }

        return result;
    }

    #region Private Helper Methods

    private static List<string> ParseTags(string? tagsJson)
    {
        if (string.IsNullOrEmpty(tagsJson))
        {
            return new List<string>();
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<List<string>>(tagsJson) ?? new List<string>();
        }
        catch
        {
            return new List<string>();
        }
    }

    private static string MaskApiKey(string apiKey)
    {
        if (string.IsNullOrEmpty(apiKey) || apiKey.Length <= 8)
        {
            return "****";
        }

        return $"{apiKey.Substring(0, 4)}...{apiKey.Substring(apiKey.Length - 4)}";
    }

    private class DataSourceConfig
    {
        public string? ApiKey { get; set; }
        public string? Endpoint { get; set; }
        public string? Model { get; set; }
    }

    #endregion
}
