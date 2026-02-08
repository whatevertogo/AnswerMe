using AnswerMe.Application.DTOs;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// 数据源服务接口
/// </summary>
public interface IDataSourceService
{
    /// <summary>
    /// 创建数据源
    /// </summary>
    Task<DataSourceDto> CreateAsync(int userId, CreateDataSourceDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的数据源列表
    /// </summary>
    Task<List<DataSourceDto>> GetUserDataSourcesAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 根据ID获取数据源
    /// </summary>
    Task<DataSourceDto?> GetByIdAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取用户的默认数据源
    /// </summary>
    Task<DataSourceDto?> GetDefaultAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 更新数据源
    /// </summary>
    Task<DataSourceDto?> UpdateAsync(int id, int userId, UpdateDataSourceDto dto, CancellationToken cancellationToken = default);

    /// <summary>
    /// 删除数据源
    /// </summary>
    Task<bool> DeleteAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 设置默认数据源
    /// </summary>
    Task<bool> SetDefaultAsync(int id, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 解密并获取完整的API密钥（内部使用）
    /// </summary>
    Task<string?> GetDecryptedApiKeyAsync(int dataSourceId, int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 验证API密钥是否有效
    /// </summary>
    Task<bool> ValidateApiKeyAsync(int dataSourceId, int userId, CancellationToken cancellationToken = default);
}
