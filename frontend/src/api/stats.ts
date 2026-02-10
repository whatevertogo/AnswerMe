import { request } from '@/utils/request'

/**
 * 首页统计数据响应
 * 对应后端 HomeStatsDto
 */
export interface HomeStatsResponse {
  /** 题库总数 */
  questionBanksCount: number
  /** 题目总数 */
  questionsCount: number
  /** 本月练习次数 */
  monthlyAttempts: number
  /** AI数据源数量 */
  dataSourcesCount: number
}

/**
 * 获取首页统计数据
 */
export function getHomeStats(): Promise<HomeStatsResponse> {
  return request.get('/stats/home') as Promise<HomeStatsResponse>
}
