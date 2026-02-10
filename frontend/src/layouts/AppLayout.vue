<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import {
  HomeFilled,
  Notebook,
  Reading,
  Setting,
  User,
  SwitchButton,
  ArrowDown,
  Setting as SettingIcon
} from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAuthStore } from '@/stores/auth'
import SettingsDialog from '@/components/SettingsDialog.vue'

const router = useRouter()
const route = useRoute()
const authStore = useAuthStore()

const menuItems = [
  {
    path: '/home',
    icon: HomeFilled,
    title: '首页'
  },
  {
    path: '/question-banks',
    icon: Notebook,
    title: '题库管理'
  },
  {
    path: '/practice',
    icon: Reading,
    title: '开始练习'
  },
  {
    path: '/ai-config',
    icon: Setting,
    title: 'AI配置'
  }
]

const activeMenu = computed(() => route.path)

// 设置对话框
const settingsDialogVisible = ref(false)

const handleCommand = (command: string) => {
  if (command === 'settings') {
    settingsDialogVisible.value = true
  } else if (command === 'logout') {
    handleLogout()
  }
}

const handleLogout = async () => {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    authStore.logout()
    ElMessage.success('已退出登录')
    router.push('/login')
  } catch {
    // 用户取消
  }
}
</script>

<template>
  <div class="app-layout">
    <!-- 顶部导航栏 -->
    <header class="app-header">
      <div class="header-container">
        <!-- Logo -->
        <div class="logo-section">
          <div class="logo-icon">
            <el-icon :size="28"><Reading /></el-icon>
          </div>
          <h1 class="logo-text">AnswerMe</h1>
        </div>

        <!-- 导航菜单 -->
        <nav class="nav-menu">
          <router-link
            v-for="item in menuItems"
            :key="item.path"
            :to="item.path"
            :class="['nav-item', { active: activeMenu.startsWith(item.path) }]"
          >
            <el-icon><component :is="item.icon" /></el-icon>
            <span class="nav-text">{{ item.title }}</span>
          </router-link>
        </nav>

        <!-- 用户信息 -->
        <div class="user-section">
          <el-dropdown @command="handleCommand" trigger="click">
            <div class="user-dropdown">
              <el-avatar :size="36" class="user-avatar">
                <el-icon><User /></el-icon>
              </el-avatar>
              <span class="user-name">
                {{ authStore.userInfo?.username || authStore.userInfo?.email || '用户' }}
              </span>
              <el-icon class="dropdown-icon" :size="14"><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="settings">
                  <el-icon><SettingIcon /></el-icon>
                  设置
                </el-dropdown-item>
                <el-dropdown-item command="logout">
                  <el-icon><SwitchButton /></el-icon>
                  退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </div>
    </header>

    <!-- 内容区域 -->
    <main class="app-main">
      <div class="content-container">
        <router-view v-slot="{ Component, route }">
          <transition :name="(route.meta?.transition as string) || 'fade'" mode="out-in">
            <component :is="Component" :key="route.path" />
          </transition>
        </router-view>
      </div>
    </main>

    <!-- 设置对话框 -->
    <SettingsDialog v-model="settingsDialogVisible" />
  </div>
</template>

<style scoped>
.app-layout {
  @apply min-h-screen flex flex-col bg-bg;
}

/* 顶部导航栏 */
.app-header {
  @apply bg-bg border-b border-border sticky top-0 z-[100] shadow-xs;
}

.header-container {
  @apply max-w-[1400px] mx-auto px-8 h-16 flex items-center justify-between;
}

/* Logo */
.logo-section {
  @apply flex items-center gap-3 flex-shrink-0;
}

.logo-icon {
  @apply w-11 h-11 bg-primary rounded-md flex items-center justify-center text-white shadow-sm;
}

.logo-text {
  @apply text-xl font-bold text-text-primary m-0 whitespace-nowrap;
}

/* 导航菜单 */
.nav-menu {
  @apply flex items-center gap-2 flex-1 mx-12;
}

.nav-item {
  @apply flex items-center gap-2 px-4 py-2 rounded-md text-text-secondary
         no-underline text-sm font-medium
         transition-all duration-400 ease-smooth
         hover:bg-bg-secondary hover:text-text-primary;
}

.nav-item.active {
  @apply bg-primary text-white;
}

.nav-text {
  @apply whitespace-nowrap;
}

/* 用户信息 */
.user-section {
  @apply flex-shrink-0;
}

.user-dropdown {
  @apply flex items-center gap-2 px-3 py-1.5 rounded-md cursor-pointer
         transition-all duration-400 ease-smooth
         hover:bg-bg-secondary;
}

.user-avatar {
  @apply bg-primary flex-shrink-0;
}

.user-name {
  @apply text-sm font-medium text-text-primary max-w-[150px] overflow-hidden
         text-ellipsis whitespace-nowrap;
}

.dropdown-icon {
  @apply text-text-muted flex-shrink-0;
}

/* 内容区域 */
.app-main {
  @apply flex-1 overflow-y-auto;
}

.content-container {
  @apply max-w-[1400px] mx-auto p-8 min-h-[calc(100vh-64px)];
}

/* 过渡动画 */
.fade-enter-active,
.fade-leave-active {
  @apply transition-all duration-400 ease-smooth;
}

.fade-enter-from {
  @apply opacity-0 -translate-y-2.5;
}

.fade-leave-to {
  @apply opacity-0 translate-y-2.5;
}

/* 响应式 */
@media (max-width: 1024px) {
  .nav-menu {
    @apply mx-6;
  }

  .user-name {
    @apply hidden;
  }
}

@media (max-width: 768px) {
  .header-container {
    @apply px-4;
  }

  .nav-text {
    @apply hidden;
  }

  .content-container {
    @apply p-4;
  }
}

@media (max-width: 640px) {
  .logo-text {
    @apply hidden;
  }

  .nav-menu {
    @apply mx-3;
  }
}
</style>
