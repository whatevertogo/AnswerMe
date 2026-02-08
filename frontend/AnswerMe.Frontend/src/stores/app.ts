import { defineStore } from 'pinia'
import { ref } from 'vue'

export const useAppStore = defineStore('app', () => {
  const isLoading = ref<boolean>(false)
  const sidebarCollapsed = ref<boolean>(false)
  const theme = ref<'light' | 'dark'>('light')

  function setLoading(loading: boolean) {
    isLoading.value = loading
  }

  function toggleSidebar() {
    sidebarCollapsed.value = !sidebarCollapsed.value
  }

  function setTheme(newTheme: 'light' | 'dark') {
    theme.value = newTheme
  }

  return {
    isLoading,
    sidebarCollapsed,
    theme,
    setLoading,
    toggleSidebar,
    setTheme
  }
})
