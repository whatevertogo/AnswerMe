<script setup lang="ts">
import { computed, ref, watch, onMounted, onUnmounted, nextTick } from 'vue'
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

// 导航栏滚动状态
const isScrolled = ref(false)

// 监听滚动事件
const handleScroll = () => {
  isScrolled.value = window.scrollY > 10
}

// Intersection Observer for scroll reveal animation
const observer = ref<IntersectionObserver | null>(null)

const observeScrollReveal = () => {
  nextTick(() => {
    // 清除之前的 observer
    if (observer.value) {
      observer.value.disconnect()
    }

    // 查找所有需要动画的元素
    const elements = document.querySelectorAll('.scroll-reveal')

    // 创建 Intersection Observer
    observer.value = new IntersectionObserver(
      (entries) => {
        entries.forEach(entry => {
          if (entry.isIntersecting) {
            entry.target.classList.add('visible')
          }
        })
      },
      {
        root: null,
        rootMargin: '0px',
        threshold: 0.1
      }
    )

    // 开始观察所有元素
    elements.forEach(el => observer.value?.observe(el))
  })
}

onMounted(() => {
  window.addEventListener('scroll', handleScroll)
  // 初始化滚动显示动画
  observeScrollReveal()
})

// 监听路由变化，重新应用动画
watch(route, () => {
  nextTick(() => {
    observeScrollReveal()
  })
})

onUnmounted(() => {
  window.removeEventListener('scroll', handleScroll)
  if (observer.value) {
    observer.value.disconnect()
  }
})

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
    <header class="app-header" :class="{ scrolled: isScrolled }">
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
  @apply bg-bg border-b border-border sticky top-0 z-[100];
  transition: all var(--transition-normal);
  backdrop-filter: blur(0);
}

.app-header.scrolled {
  box-shadow: 0 2px 12px rgba(42, 37, 32, 0.08);
  background: var(--color-bg-glass);
}

.header-container {
  @apply max-w-[1400px] mx-auto px-8 h-16 flex items-center justify-between;
}

/* Logo */
.logo-section {
  @apply flex items-center gap-3 flex-shrink-0;
}

.logo-icon {
  @apply w-12 h-12 rounded-lg flex items-center justify-center text-white shadow-sm;
  background: var(--color-primary-gradient);
}

.logo-text {
  @apply text-2xl font-bold m-0 whitespace-nowrap;
  background: var(--color-primary-gradient);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  font-family: 'Noto Serif SC', 'Songti SC', serif;
}

/* 导航菜单 */
.nav-menu {
  @apply flex items-center gap-1 flex-1;
}

.nav-item {
  @apply flex items-center gap-2 px-4 py-2 rounded-md
         no-underline text-sm font-medium;
  transition: all var(--transition-fast);
  color: var(--color-text-secondary);
}

.nav-item:hover {
  background: var(--color-hover-light);
  color: var(--color-text-primary);
}

.nav-item.active {
  background: var(--color-primary-gradient);
  color: white;
  box-shadow: 0 2px 8px rgba(61, 40, 23, 0.20);
}

.nav-text {
  @apply font-medium whitespace-nowrap;
  font-family: 'Noto Serif SC', 'Songti SC', serif;
}

/* 用户信息 */
.user-section {
  @apply flex-shrink-0;
}

.user-dropdown {
  @apply flex items-center gap-2 px-3 py-1.5 rounded-md cursor-pointer;
  transition: all var(--transition-fast);
  @apply hover:bg-bg-secondary;
}

.user-dropdown:hover {
  background: var(--color-hover-light);
}

.user-avatar {
  @apply flex-shrink-0;
  background: var(--color-primary-gradient);
}

.user-name {
  @apply text-sm font-medium max-w-[120px] overflow-hidden text-ellipsis whitespace-nowrap;
  color: var(--color-text-primary);
}

.dropdown-icon {
  @apply flex-shrink-0;
  color: var(--color-text-muted);
}

/* 内容区域 */
.app-main {
  @apply flex-1 overflow-y-auto;
}

.content-container {
  @apply max-w-[1400px] mx-auto px-8 py-8 min-h-[calc(100vh-64px)];
}

/* 过渡动画 */
.fade-enter-active,
.fade-leave-active {
  @apply transition-all duration-300 ease-out;
}

.fade-enter-from {
  @apply opacity-0 -translate-y-2.5;
}

.fade-leave-to {
  @apply opacity-0 translate-y-2.5;
}

/* 响应式 */
@media (max-width: 1024px) {
  .nav-text {
    @apply hidden;
  }
}

@media (max-width: 768px) {
  .header-container {
    @apply px-4 h-14;
  }

  .logo-text {
    @apply text-xl;
  }

  .nav-menu {
    @apply gap-0;
  }

  .nav-item {
    @apply px-2;
  }

  .user-name {
    @apply max-w-[80px];
  }

  .content-container {
    @apply px-4 py-6 min-h-[calc(100vh-56px)];
  }
}
</style>
