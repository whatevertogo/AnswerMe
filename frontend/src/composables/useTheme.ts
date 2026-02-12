import { computed, watch, readonly } from 'vue'
import { useStorage, usePreferredDark, useDark } from '@vueuse/core'

export type ThemeMode = 'system' | 'light' | 'dark'

const THEME_MODE_KEY = 'theme-mode'

/**
 * 主题管理 composable
 * 支持 System/Light/Dark 三种模式
 */
export function useTheme() {
  // 持久化主题模式设置
  const themeMode = useStorage<ThemeMode>(THEME_MODE_KEY, 'system')

  // 获取系统主题偏好
  const prefersDark = usePreferredDark()

  // 计算当前是否应该使用深色模式
  const isDark = computed(() => {
    if (themeMode.value === 'system') {
      return prefersDark.value
    }
    return themeMode.value === 'dark'
  })

  // 使用 useDark 控制深色 class
  const dark = useDark({
    selector: 'html',
    attribute: 'class',
    valueDark: 'dark',
    valueLight: ''
  })

  // 监听 isDark 变化，同步到 useDark
  watch(
    isDark,
    value => {
      dark.value = value
    },
    { immediate: true }
  )

  /**
   * 设置主题模式
   */
  function setTheme(mode: ThemeMode) {
    themeMode.value = mode
  }

  /**
   * 切换主题（在 light 和 dark 之间切换）
   */
  function toggleTheme() {
    if (themeMode.value === 'system') {
      // 如果当前是系统模式，根据系统偏好切换
      setTheme(prefersDark.value ? 'light' : 'dark')
    } else if (themeMode.value === 'dark') {
      setTheme('light')
    } else {
      setTheme('dark')
    }
  }

  return {
    themeMode: readonly(themeMode),
    isDark: readonly(isDark),
    setTheme,
    toggleTheme
  }
}
