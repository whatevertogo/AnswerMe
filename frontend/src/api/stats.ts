import request from '@/utils/request'

/**
 * 首页统计数据响应
 */
export interface HomeStatsResponse {
  questionBanksCount: number
  questionsCount: number
  monthlyAttempts: number
  dataSourcesCount: number
}

/**
 * 获取首页统计数据
 */
export function getHomeStats(): Promise<HomeStatsResponse> {
  return request.get<HomeStatsResponse>('/stats/home')
}
