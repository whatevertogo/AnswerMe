<script setup lang="ts">
import { ref, markRaw, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import {
  Notebook,
  Setting,
  TrendCharts,
  CollectionTag,
  CirclePlus,
  Cpu,
  EditPen,
  Sugar,
  ArrowRight
} from '@element-plus/icons-vue'
import { getHomeStats, type HomeStatsResponse } from '@/api/stats'
import { ElMessage } from 'element-plus'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const authStore = useAuthStore()

const username = computed(() => authStore.userInfo?.username || authStore.userInfo?.email || '用户')

interface StatCard {
  icon: any
  title: string
  value: string
  color: string
  path: string
}

const stats = ref<StatCard[]>([
  {
    icon: markRaw(Notebook),
    title: '题库总数',
    value: '0',
    color: 'var(--color-primary)',
    path: '/question-banks'
  },
  {
    icon: markRaw(CollectionTag),
    title: '题目总数',
    value: '0',
    color: 'var(--color-success)',
    path: '/question-banks'
  },
  {
    icon: markRaw(TrendCharts),
    title: '本月练习',
    value: '0',
    color: 'var(--color-warning)',
    path: '/practice'
  },
  {
    icon: markRaw(Setting),
    title: 'AI配置',
    value: '0',
    color: 'var(--color-danger)',
    path: '/ai-config'
  }
])

const isLoading = ref(true)

async function loadStats() {
  try {
    isLoading.value = true
    const data: HomeStatsResponse = await getHomeStats()
    stats.value[0] && (stats.value[0].value = data.questionBanksCount.toString())
    stats.value[1] && (stats.value[1].value = data.questionsCount.toString())
    stats.value[2] && (stats.value[2].value = data.monthlyAttempts.toString())
    stats.value[3] && (stats.value[3].value = data.dataSourcesCount.toString())
  } catch (error) {
    console.error('加载统计数据失败:', error)
    ElMessage.error('加载统计数据失败')
  } finally {
    isLoading.value = false
  }
}

onMounted(() => {
  loadStats()
})

const quickActions = ref([
  {
    title: '创建题库',
    description: '快速创建一个新的题库',
    icon: markRaw(CirclePlus),
    action: () => router.push('/question-banks?action=create')
  },
  {
    title: 'AI生成题目',
    description: '使用AI自动生成题目',
    icon: markRaw(Cpu),
    action: () => router.push('/question-banks')
  },
  {
    title: '开始练习',
    description: '选择题库开始练习',
    icon: markRaw(EditPen),
    action: () => router.push('/practice')
  },
  {
    title: '配置AI',
    description: '管理AI数据源',
    icon: markRaw(Setting),
    action: () => router.push('/ai-config')
  }
])

const recentActivities = ref([
  {
    title: '创建了"JavaScript基础"题库',
    time: '2小时前',
    type: 'create'
  },
  {
    title: '生成了20道题目',
    time: '5小时前',
    type: 'generate'
  },
  {
    title: '完成了"Vue 3入门"练习',
    time: '昨天',
    type: 'practice'
  }
])
</script>

<template>
  <div class="home-view">
    <!-- 欢迎横幅 -->
    <div class="welcome-banner">
      <div class="banner-content">
        <div class="banner-greeting">
          <el-icon :size="32" class="wave-icon"><Sugar /></el-icon>
          <div class="greeting-text">
            <h1>欢迎回来，{{ username }}</h1>
            <p>今天想学习什么？</p>
          </div>
        </div>
        <div class="banner-decoration">
          <div class="decoration-circle"></div>
          <div class="decoration-circle"></div>
        </div>
      </div>
    </div>

    <!-- 统计概览 - 横向排列大卡片 -->
    <div class="stats-section">
      <div
        v-for="stat in stats"
        :key="stat.title"
        class="stat-card-large"
        @click="router.push(stat.path)"
      >
        <div class="stat-icon" :style="{ background: stat.color }">
          <el-icon :size="28" color="#FFFFFF">
            <component :is="stat.icon" />
          </el-icon>
        </div>
        <div class="stat-info">
          <div class="stat-value">{{ stat.value }}</div>
          <div class="stat-title">{{ stat.title }}</div>
        </div>
      </div>
    </div>

    <!-- 快捷操作 - 垂直列表 -->
    <div class="actions-section">
      <h2 class="section-title">快捷操作</h2>
      <div class="action-list">
        <div
          v-for="action in quickActions"
          :key="action.title"
          class="action-item"
          @click="action.action"
        >
          <div class="action-icon">
            <el-icon :size="24">
              <component :is="action.icon" />
            </el-icon>
          </div>
          <div class="action-content">
            <div class="action-title">{{ action.title }}</div>
            <div class="action-description">{{ action.description }}</div>
          </div>
          <el-icon class="action-arrow" :size="16">
            <ArrowRight />
          </el-icon>
        </div>
      </div>
    </div>

    <!-- 最近活动 - 简化展示 -->
    <div class="activity-section" v-if="recentActivities.length > 0">
      <h2 class="section-title">最近活动</h2>
      <div class="activity-list">
        <div
          v-for="(activity, index) in recentActivities"
          :key="index"
          class="activity-item"
        >
          <div class="activity-dot"></div>
          <div class="activity-content">
            <div class="activity-title">{{ activity.title }}</div>
            <div class="activity-time">{{ activity.time }}</div>
          </div>
        </div>
      </div>
    </div>
  </div>
</template>

<style scoped>
.home-view {
  @apply max-w-[1400px] mx-auto;
}

/* 欢迎横幅 */
.welcome-banner {
  @apply bg-bg-secondary rounded-xl p-10 mb-8 shadow-sm;
}

.banner-content {
  @apply flex justify-between items-center;
}

.banner-greeting {
  @apply flex items-center gap-4;
}

.wave-icon {
  @apply text-primary;
  animation: wave 1.5s ease-in-out infinite;
}

@keyframes wave {
  0%, 100% { transform: rotate(0deg); }
  25% { transform: rotate(20deg); }
  75% { transform: rotate(-20deg); }
}

.greeting-text h1 {
  @apply text-[1.75rem] font-bold text-text-primary m-0 mb-1;
}

.greeting-text p {
  @apply text-base text-text-secondary m-0;
}

.banner-decoration {
  @apply flex gap-4;
}

.decoration-circle {
  @apply w-[60px] h-[60px] rounded-full;
  background: linear-gradient(135deg, var(--color-primary) 0%, var(--color-secondary) 100%);
  @apply opacity-10;
}

/* 统计概览 */
.stats-section {
  @apply grid grid-cols-4 gap-6 mb-8;
}

.stat-card-large {
  @apply bg-bg rounded-lg p-6 shadow-sm
         transition-all duration-400 ease-smooth
         flex items-center gap-4 cursor-pointer
         border border-border
         hover:-translate-y-1 hover:shadow-md hover:border-primary;
}

.stat-icon {
  @apply w-14 h-14 rounded-md
         flex items-center justify-center
         flex-shrink-0;
}

.stat-info {
  @apply flex-1;
}

.stat-value {
  @apply text-[1.75rem] font-bold text-text-primary mb-1;
}

.stat-title {
  @apply text-sm text-text-secondary font-medium;
}

/* 区块 */
.section-title {
  @apply text-[1.125rem] font-semibold text-text-primary m-0 mb-4;
}

/* 快捷操作 */
.actions-section {
  @apply mb-8;
}

.action-list {
  @apply flex flex-col gap-3;
}

.action-item {
  @apply bg-bg rounded-md px-5 py-4
         flex items-center gap-4 cursor-pointer
         transition-all duration-400 ease-smooth
         border border-border shadow-xs
         hover:translate-x-1 hover:shadow-sm hover:border-primary;
}

.action-icon {
  @apply w-11 h-11 rounded-md bg-bg-secondary
         flex items-center justify-center text-primary
         flex-shrink-0;
}

.action-content {
  @apply flex-1;
}

.action-title {
  @apply text-base font-semibold text-text-primary mb-1;
}

.action-description {
  @apply text-[0.8125rem] text-text-secondary;
}

.action-arrow {
  @apply text-text-muted;
}

/* 最近活动 */
.activity-section {
  @apply mb-8;
}

.activity-list {
  @apply bg-bg rounded-lg p-6
         border border-border shadow-xs;
}

.activity-item {
  @apply flex items-start gap-4 py-4 relative;
}

.activity-item:not(:last-child)::after {
  content: '';
  @apply absolute left-[7px] top-10 bottom-0 w-[2px] bg-bg-secondary;
}

.activity-dot {
  @apply w-4 h-4 rounded-full bg-primary flex-shrink-0 mt-1;
}

.activity-content {
  @apply flex-1;
}

.activity-title {
  @apply text-[0.9375rem] text-text-primary mb-1;
}

.activity-time {
  @apply text-[0.8125rem] text-text-muted;
}

/* 响应式 */
@media (max-width: 1024px) {
  .stats-section {
    @apply grid-cols-2;
  }
}

@media (max-width: 640px) {
  .welcome-banner {
    @apply p-6;
  }

  .banner-content {
    @apply flex-col items-start gap-4;
  }

  .banner-decoration {
    @apply hidden;
  }

  .greeting-text h1 {
    @apply text-[1.25rem];
  }

  .stats-section {
    @apply grid-cols-1 gap-4;
  }

  .action-item {
    @apply px-4 py-3.5;
  }
}
</style>
