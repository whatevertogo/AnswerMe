using AnswerMe.Application.DTOs;

namespace AnswerMe.Application.Interfaces;

/// <summary>
/// 统计服务接口
/// </summary>
public interface IStatsService
{
    /// <summary>
    /// 获取首页统计数据
    /// </summary>
    Task<HomeStatsDto> GetHomeStatsAsync(int userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// 获取首页最近活动
    /// </summary>
    Task<List<HomeRecentActivityDto>> GetHomeRecentActivitiesAsync(
        int userId,
        int limit = 10,
        CancellationToken cancellationToken = default);
}
