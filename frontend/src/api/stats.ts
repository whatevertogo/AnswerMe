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
 * 首页最近活动项
 */
export interface HomeRecentActivity {
  /** 活动类型：create_bank / generate_questions / complete_practice */
  type: string
  /** 活动文案 */
  title: string
  /** UTC 时间字符串 */
  occurredAt: string
}

/**
 * 获取首页统计数据
 */
export function getHomeStats(): Promise<HomeStatsResponse> {
  return request.get('/stats/home') as Promise<HomeStatsResponse>
}

/**
 * 获取首页最近活动
 */
export function getHomeRecentActivities(limit = 10): Promise<HomeRecentActivity[]> {
  return request.get('/stats/recent-activities', { params: { limit } }) as Promise<
    HomeRecentActivity[]
  >
}
