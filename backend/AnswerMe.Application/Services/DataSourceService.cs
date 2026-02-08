using System.Text.Json;
using AnswerMe.Application.DTOs;
using AnswerMe.Application.Interfaces;
using AnswerMe.Domain.Interfaces;
using Microsoft.AspNetCore.DataProtection;

namespace AnswerMe.Application.Services;

/// <summary>
/// 数据源服务实现
/// </summary>
public class DataSourceService : IDataSourceService
{
    private readonly IDataSourceRepository _dataSourceRepository;
    private readonly IDataProtector _dataProtector;

    public DataSourceService(
        IDataSourceRepository dataSourceRepository,
        IDataProtectionProvider dataProtectionProvider)
    {
        _dataSourceRepository = dataSourceRepository;
        _dataProtector = dataProtectionProvider.CreateProtector("DataSourceApiKeys");
    }

    public async Task<DataSourceDto> CreateAsync(int userId, CreateDataSourceDto dto, CancellationToken cancellationToken = default)
    {
        // 加密API密钥
        var encryptedApiKey = _dataProtector.Protect(dto.ApiKey);

        // 构建配置JSON - 使用DataSourceConfig类
        var config = new DataSourceConfig
        {
            ApiKey = encryptedApiKey,
            Endpoint = dto.Endpoint,
            Model = dto.Model
        };
        var configJson = JsonSerializer.Serialize(config);

        // 如果设置为默认，先取消其他默认
        if (dto.IsDefault)
        {
            await ClearDefaultAsync(userId, cancellationToken);
        }

        var dataSource = new Domain.Entities.DataSource
        {
            UserId = userId,
            Name = dto.Name,
            Type = dto.Type,
            Config = configJson,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dataSourceRepository.AddAsync(dataSource, cancellationToken);
        await _dataSourceRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(dataSource, dto.ApiKey);
    }

    public async Task<List<DataSourceDto>> GetUserDataSourcesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var dataSources = await _dataSourceRepository.GetByUserIdAsync(userId, cancellationToken);
        var result = new List<DataSourceDto>();

        foreach (var ds in dataSources)
        {
            var config = JsonSerializer.Deserialize<DataSourceConfig>(ds.Config);
            var maskedKey = MaskApiKey(config?.ApiKey ?? "");
            result.Add(MapToDto(ds, maskedKey));
        }

        return result;
    }

    public async Task<DataSourceDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetByIdAsync(id, cancellationToken);
        if (dataSource == null || dataSource.UserId != userId)
        {
            return null;
        }

        var config = JsonSerializer.Deserialize<DataSourceConfig>(dataSource.Config);
        var maskedKey = MaskApiKey(config?.ApiKey ?? "");

        return MapToDto(dataSource, maskedKey);
    }

    public async Task<DataSourceDto?> GetDefaultAsync(int userId, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetDefaultByUserIdAsync(userId, cancellationToken);
        if (dataSource == null)
        {
            return null;
        }

        var config = JsonSerializer.Deserialize<DataSourceConfig>(dataSource.Config);
        var maskedKey = MaskApiKey(config?.ApiKey ?? "");

        return MapToDto(dataSource, maskedKey);
    }

    public async Task<DataSourceDto?> UpdateAsync(int id, int userId, UpdateDataSourceDto dto, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetByIdAsync(id, cancellationToken);
        if (dataSource == null || dataSource.UserId != userId)
        {
            return null;
        }

        var config = JsonSerializer.Deserialize<DataSourceConfig>(dataSource.Config) ?? new DataSourceConfig();

        // 更新名称
        if (!string.IsNullOrEmpty(dto.Name))
        {
            dataSource.Name = dto.Name;
        }

        // 更新API Key（需要加密）
        if (!string.IsNullOrEmpty(dto.ApiKey))
        {
            config.ApiKey = _dataProtector.Protect(dto.ApiKey);
        }

        // 更新Endpoint
        if (dto.Endpoint != null)
        {
            config.Endpoint = dto.Endpoint;
        }

        // 更新Model
        if (dto.Model != null)
        {
            config.Model = dto.Model;
        }

        // 更新默认状态
        if (dto.IsDefault.HasValue && dto.IsDefault.Value)
        {
            await ClearDefaultAsync(userId, cancellationToken);
            dataSource.IsDefault = true;
        }
        else if (dto.IsDefault.HasValue)
        {
            dataSource.IsDefault = false;
        }

        // 保存更新后的配置
        dataSource.Config = JsonSerializer.Serialize(config);
        dataSource.UpdatedAt = DateTime.UtcNow;

        await _dataSourceRepository.SaveChangesAsync(cancellationToken);

        var maskedKey = MaskApiKey(config.ApiKey ?? "");
        return MapToDto(dataSource, maskedKey);
    }

    public async Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetByIdAsync(id, cancellationToken);
        if (dataSource == null || dataSource.UserId != userId)
        {
            return false;
        }

        await _dataSourceRepository.DeleteAsync(dataSource, cancellationToken);
        await _dataSourceRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetDefaultAsync(int id, int userId, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetByIdAsync(id, cancellationToken);
        if (dataSource == null || dataSource.UserId != userId)
        {
            return false;
        }

        await ClearDefaultAsync(userId, cancellationToken);
        dataSource.IsDefault = true;
        dataSource.UpdatedAt = DateTime.UtcNow;

        await _dataSourceRepository.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<string?> GetDecryptedApiKeyAsync(int dataSourceId, int userId, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetByIdAsync(dataSourceId, cancellationToken);
        if (dataSource == null || dataSource.UserId != userId)
        {
            return null;
        }

        var config = JsonSerializer.Deserialize<DataSourceConfig>(dataSource.Config);
        if (string.IsNullOrEmpty(config?.ApiKey))
        {
            return null;
        }

        try
        {
            return _dataProtector.Unprotect(config.ApiKey);
        }
        catch
        {
            return null;
        }
    }

    public async Task<bool> ValidateApiKeyAsync(int dataSourceId, int userId, CancellationToken cancellationToken = default)
    {
        var apiKey = await GetDecryptedApiKeyAsync(dataSourceId, userId, cancellationToken);
        return !string.IsNullOrEmpty(apiKey);
    }

    #region Private Methods

    private async Task ClearDefaultAsync(int userId, CancellationToken cancellationToken)
    {
        var dataSources = await _dataSourceRepository.GetByUserIdAsync(userId, cancellationToken);
        foreach (var ds in dataSources.Where(ds => ds.IsDefault))
        {
            ds.IsDefault = false;
            ds.UpdatedAt = DateTime.UtcNow;
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

    private static DataSourceDto MapToDto(Domain.Entities.DataSource dataSource, string maskedKey)
    {
        var config = JsonSerializer.Deserialize<DataSourceConfig>(dataSource.Config);

        return new DataSourceDto
        {
            Id = dataSource.Id,
            UserId = dataSource.UserId,
            Name = dataSource.Name,
            Type = dataSource.Type,
            Endpoint = config?.Endpoint,
            Model = config?.Model,
            MaskedApiKey = maskedKey,
            IsDefault = dataSource.IsDefault,
            CreatedAt = dataSource.CreatedAt,
            UpdatedAt = dataSource.UpdatedAt
        };
    }

    #endregion

    #region Config Model

    private class DataSourceConfig
    {
        public string? ApiKey { get; set; }
        public string? Endpoint { get; set; }
        public string? Model { get; set; }
    }

    #endregion
}
