using System.Text.Json;
using AnswerMe.Application.AI;
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
    private readonly AIProviderFactory _aiProviderFactory;

    public DataSourceService(
        IDataSourceRepository dataSourceRepository,
        IDataProtectionProvider dataProtectionProvider,
        AIProviderFactory aiProviderFactory)
    {
        _dataSourceRepository = dataSourceRepository;
        _dataProtector = dataProtectionProvider.CreateProtector("DataSourceApiKeys");
        _aiProviderFactory = aiProviderFactory;
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
        var configJson = SerializeConfig(config);

        // 如果设置为默认，先取消其他默认
        if (dto.IsDefault)
        {
            await ClearDefaultAsync(userId, cancellationToken);
        }

        var dataSource = new Domain.Entities.DataSource
        {
            UserId = userId,
            Name = dto.Name,
            Type = dto.Type.Trim().ToLowerInvariant(),
            Config = configJson,
            IsDefault = dto.IsDefault,
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _dataSourceRepository.AddAsync(dataSource, cancellationToken);
        await _dataSourceRepository.SaveChangesAsync(cancellationToken);

        return MapToDto(dataSource, config, MaskApiKey(dto.ApiKey));
    }

    public async Task<List<DataSourceDto>> GetUserDataSourcesAsync(int userId, CancellationToken cancellationToken = default)
    {
        var dataSources = await _dataSourceRepository.GetByUserIdAsync(userId, cancellationToken);
        var result = new List<DataSourceDto>();

        foreach (var ds in dataSources)
        {
            var config = DeserializeConfig(ds.Config);
            var maskedKey = MaskApiKeyFromEncrypted(config.ApiKey ?? string.Empty);
            result.Add(MapToDto(ds, config, maskedKey));
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

        var config = DeserializeConfig(dataSource.Config);
        var maskedKey = MaskApiKeyFromEncrypted(config.ApiKey ?? string.Empty);

        return MapToDto(dataSource, config, maskedKey);
    }

    public async Task<DataSourceDto?> GetDefaultAsync(int userId, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetDefaultByUserIdAsync(userId, cancellationToken);
        if (dataSource == null)
        {
            return null;
        }

        var config = DeserializeConfig(dataSource.Config);
        var maskedKey = MaskApiKeyFromEncrypted(config.ApiKey ?? string.Empty);

        return MapToDto(dataSource, config, maskedKey);
    }

    public async Task<DataSourceDto?> UpdateAsync(int id, int userId, UpdateDataSourceDto dto, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetByIdAsync(id, cancellationToken);
        if (dataSource == null || dataSource.UserId != userId)
        {
            return null;
        }

        var config = DeserializeConfig(dataSource.Config);

        // 更新名称
        if (!string.IsNullOrEmpty(dto.Name))
        {
            dataSource.Name = dto.Name;
        }

        // 更新Provider类型
        if (!string.IsNullOrEmpty(dto.Type))
        {
            dataSource.Type = dto.Type.Trim().ToLowerInvariant();
        }

        // 更新API Key（需要加密）
        // 注意：编辑时前端会发送空字符串表示不修改密钥
        // 需要检查是否是真实的 API Key（不是掩码格式）
        if (!string.IsNullOrEmpty(dto.ApiKey) && !dto.ApiKey.Contains("...") && !dto.ApiKey.Contains("****"))
        {
            // 只有当输入的不是掩码且不为空时才更新密钥
            config.ApiKey = _dataProtector.Protect(dto.ApiKey);
        }
        // 如果 dto.ApiKey 是空字符串或掩码格式，则保留原密钥不变

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
        dataSource.Config = SerializeConfig(config);
        dataSource.UpdatedAt = DateTime.UtcNow;

        await _dataSourceRepository.SaveChangesAsync(cancellationToken);

        var maskedKey = MaskApiKeyFromEncrypted(config.ApiKey ?? string.Empty);
        return MapToDto(dataSource, config, maskedKey);
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
        var config = await GetDecryptedConfigAsync(dataSourceId, userId, cancellationToken);
        return config?.ApiKey;
    }

    public async Task<DataSourceConfigDto?> GetDecryptedConfigAsync(int dataSourceId, int userId, CancellationToken cancellationToken = default)
    {
        var dataSource = await _dataSourceRepository.GetByIdAsync(dataSourceId, cancellationToken);
        if (dataSource == null || dataSource.UserId != userId)
        {
            return null;
        }

        var config = DeserializeConfig(dataSource.Config);
        if (string.IsNullOrEmpty(config.ApiKey))
        {
            return null;
        }

        var decryptedApiKey = TryUnprotectApiKey(config.ApiKey);
        if (string.IsNullOrEmpty(decryptedApiKey))
        {
            return null;
        }

        return new DataSourceConfigDto
        {
            ApiKey = decryptedApiKey,
            Endpoint = config.Endpoint,
            Model = config.Model
        };
    }

    public async Task<bool> ValidateApiKeyAsync(int dataSourceId, int userId, CancellationToken cancellationToken = default)
    {
        var config = await GetDecryptedConfigAsync(dataSourceId, userId, cancellationToken);
        if (config == null || string.IsNullOrEmpty(config.ApiKey))
        {
            return false;
        }

        try
        {
            // 获取 DataSource 实体以确定 Provider 类型
            var dataSource = await _dataSourceRepository.GetByIdAsync(dataSourceId, cancellationToken);
            if (dataSource == null || dataSource.UserId != userId)
            {
                return false;
            }

            // 使用注入的 AIProviderFactory 发送真实测试请求
            var provider = _aiProviderFactory.GetProvider(dataSource.Type);

            if (provider == null)
            {
                return false;
            }

            // 发送真实的 API 验证请求
            var isValid = await provider.ValidateApiKeyAsync(
                config.ApiKey,
                config.Endpoint,
                config.Model,
                cancellationToken);
            return isValid;
        }
        catch
        {
            return false;
        }
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

    private static DataSourceDto MapToDto(
        Domain.Entities.DataSource dataSource,
        DataSourceConfig config,
        string maskedKey)
    {
        return new DataSourceDto
        {
            Id = dataSource.Id,
            UserId = dataSource.UserId,
            Name = dataSource.Name,
            Type = dataSource.Type,
            Endpoint = config.Endpoint,
            Model = config.Model,
            MaskedApiKey = maskedKey,
            IsDefault = dataSource.IsDefault,
            CreatedAt = dataSource.CreatedAt,
            UpdatedAt = dataSource.UpdatedAt
        };
    }

    private static DataSourceConfig DeserializeConfig(string configJson)
    {
        if (string.IsNullOrWhiteSpace(configJson))
        {
            return new DataSourceConfig();
        }

        try
        {
            return JsonSerializer.Deserialize<DataSourceConfig>(configJson) ?? new DataSourceConfig();
        }
        catch
        {
            return new DataSourceConfig();
        }
    }

    private static string SerializeConfig(DataSourceConfig config)
    {
        return JsonSerializer.Serialize(config);
    }

    private string? TryUnprotectApiKey(string encrypted)
    {
        if (string.IsNullOrEmpty(encrypted))
        {
            return null;
        }

        try
        {
            return _dataProtector.Unprotect(encrypted);
        }
        catch
        {
            return null;
        }
    }

    private string MaskApiKeyFromEncrypted(string encrypted)
    {
        var decrypted = TryUnprotectApiKey(encrypted);
        return string.IsNullOrEmpty(decrypted) ? "****" : MaskApiKey(decrypted);
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
