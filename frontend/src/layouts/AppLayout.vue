<script setup lang="ts">
import { ref, computed } from 'vue'
import { useRouter, useRoute } from 'vue-router'
import {
  HomeFilled,
  Notebook,
  Setting,
  User,
  SwitchButton,
  ArrowDown
} from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useUserStore } from '@/stores/user'

const router = useRouter()
const route = useRoute()
const userStore = useUserStore()

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
    path: '/data-sources',
    icon: Setting,
    title: 'AI配置'
  }
]

const activeMenu = computed(() => route.path)

const handleLogout = async () => {
  try {
    await ElMessageBox.confirm('确定要退出登录吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })

    userStore.logout()
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
          <div class="logo-icon">🎓</div>
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
          <el-dropdown @command="handleLogout" trigger="click">
            <div class="user-dropdown">
              <el-avatar :size="36" class="user-avatar">
                <el-icon><User /></el-icon>
              </el-avatar>
              <span class="user-name">
                {{ userStore.userInfo?.username || userStore.userInfo?.email || '用户' }}
              </span>
              <el-icon class="dropdown-icon" :size="14"><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu>
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
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </div>
    </main>
  </div>
</template>

<style scoped>
.app-layout {
  min-height: 100vh;
  display: flex;
  flex-direction: column;
  background: #FDF6E3;
}

.dark .app-layout {
  background: #002B36;
}

/* 顶部导航栏 */
.app-header {
  background: #FFFFFF;
  border-bottom: 1px solid #E8E4CE;
  position: sticky;
  top: 0;
  z-index: 100;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.dark .app-header {
  background: #073642;
  border-bottom-color: #586E75;
}

.header-container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 0 2rem;
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: space-between;
}

/* Logo */
.logo-section {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  flex-shrink: 0;
}

.logo-icon {
  font-size: 1.75rem;
  line-height: 1;
}

.logo-text {
  font-size: 1.25rem;
  font-weight: 700;
  color: #073642;
  margin: 0;
  white-space: nowrap;
}

.dark .logo-text {
  color: #839496;
}

/* 导航菜单 */
.nav-menu {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  flex: 1;
  margin: 0 3rem;
}

.nav-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.5rem 1rem;
  border-radius: 6px;
  color: #586E75;
  text-decoration: none;
  font-size: 0.875rem;
  font-weight: 500;
  transition: all 0.15s;
}

.dark .nav-item {
  color: #839496;
}

.nav-item:hover {
  background: #EEE8D5;
  color: #073642;
}

.dark .nav-item:hover {
  background: #073642;
  color: #FDF6E3;
}

.nav-item.active {
  background: #268BD2;
  color: #FFFFFF;
}

.dark .nav-item.active {
  background: #268BD2;
  color: #FFFFFF;
}

.nav-text {
  white-space: nowrap;
}

/* 用户信息 */
.user-section {
  flex-shrink: 0;
}

.user-dropdown {
  display: flex;
  align-items: center;
  gap: 0.5rem;
  padding: 0.375rem 0.75rem;
  border-radius: 0.5rem;
  cursor: pointer;
  transition: background 0.15s;
}

.user-dropdown:hover {
  background: #EEE8D5;
}

.dark .user-dropdown:hover {
  background: #073642;
}

.user-avatar {
  background: linear-gradient(135deg, #268BD2 0%, #2AA198 100%);
  flex-shrink: 0;
}

.user-name {
  font-size: 0.875rem;
  font-weight: 500;
  color: #073642;
  max-width: 150px;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dark .user-name {
  color: #839496;
}

.dropdown-icon {
  color: #9ca3af;
  flex-shrink: 0;
}

/* 内容区域 */
.app-main {
  flex: 1;
  overflow-y: auto;
}

.content-container {
  max-width: 1400px;
  margin: 0 auto;
  padding: 2rem;
  min-height: calc(100vh - 64px);
}

/* 过渡动画 */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.2s, transform 0.2s;
}

.fade-enter-from {
  opacity: 0;
  transform: translateY(-10px);
}

.fade-leave-to {
  opacity: 0;
  transform: translateY(10px);
}

/* 响应式 */
@media (max-width: 1024px) {
  .nav-menu {
    margin: 0 1.5rem;
  }

  .user-name {
    display: none;
  }
}

@media (max-width: 768px) {
  .header-container {
    padding: 0 1rem;
  }

  .nav-text {
    display: none;
  }

  .content-container {
    padding: 1rem;
  }
}

@media (max-width: 640px) {
  .logo-text {
    display: none;
  }

  .nav-menu {
    margin: 0 0.75rem;
  }
}
</style>
