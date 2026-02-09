/**
 * User Store - 用户状态管理
 * 
 * 此文件提供对用户状态的访问。
 * 推荐使用 @/stores/auth 获取完整的认证功能。
 */

// 重新导出 auth store 以支持 @/stores/user 引用
export { useAuthStore, type UserInfo } from './auth'
