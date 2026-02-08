<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import {
  Notebook,
  Setting,
  TrendCharts,
  CollectionTag
} from '@element-plus/icons-vue'

const router = useRouter()

const stats = ref([
  {
    icon: Notebook,
    title: '题库总数',
    value: '12',
    color: '#409eff',
    path: '/question-banks'
  },
  {
    icon: CollectionTag,
    title: '题目总数',
    value: '256',
    color: '#67c23a',
    path: '/question-banks'
  },
  {
    icon: TrendCharts,
    title: '本月练习',
    value: '45',
    color: '#e6a23c',
    path: '#'
  },
  {
    icon: Setting,
    title: 'AI配置',
    value: '3',
    color: '#f56c6c',
    path: '/data-sources'
  }
])

const quickActions = ref([
  {
    title: '创建题库',
    description: '快速创建一个新的题库',
    icon: '➕',
    color: '#409eff',
    action: () => router.push('/question-banks?action=create')
  },
  {
    title: 'AI生成题目',
    description: '使用AI自动生成题目',
    icon: '🤖',
    color: '#67c23a',
    action: () => router.push('/question-banks')
  },
  {
    title: '开始练习',
    description: '选择题库开始练习',
    icon: '📝',
    color: '#e6a23c',
    action: () => router.push('/question-banks')
  },
  {
    title: '配置AI',
    description: '管理AI数据源',
    icon: '⚙️',
    color: '#909399',
    action: () => router.push('/data-sources')
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
    <!-- 统计卡片 -->
    <el-row :gutter="20" class="stats-row">
      <el-col
        v-for="stat in stats"
        :key="stat.title"
        :xs="24"
        :sm="12"
        :md="6"
      >
        <el-card class="stat-card" shadow="hover" @click="router.push(stat.path)">
          <div class="stat-content">
            <div class="stat-icon" :style="{ background: stat.color }">
              <el-icon :size="24" color="#fff">
                <component :is="stat.icon" />
              </el-icon>
            </div>
            <div class="stat-info">
              <div class="stat-value">{{ stat.value }}</div>
              <div class="stat-title">{{ stat.title }}</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <!-- 快捷操作 -->
    <div class="section">
      <h2 class="section-title">快捷操作</h2>
      <el-row :gutter="20">
        <el-col
          v-for="action in quickActions"
          :key="action.title"
          :xs="24"
          :sm="12"
          :md="6"
        >
          <el-card class="action-card" shadow="hover" @click="action.action">
            <div class="action-icon" :style="{ background: action.color }">
              {{ action.icon }}
            </div>
            <div class="action-title">{{ action.title }}</div>
            <div class="action-description">{{ action.description }}</div>
          </el-card>
        </el-col>
      </el-row>
    </div>

    <!-- 最近活动 -->
    <div class="section">
      <h2 class="section-title">最近活动</h2>
      <el-card class="activity-card">
        <el-timeline>
          <el-timeline-item
            v-for="(activity, index) in recentActivities"
            :key="index"
            :timestamp="activity.time"
            placement="top"
          >
            <div class="activity-item">
              <el-tag
                :type="activity.type === 'create' ? 'success' : activity.type === 'generate' ? 'warning' : 'info'"
                size="small"
              >
                {{ activity.type === 'create' ? '创建' : activity.type === 'generate' ? '生成' : '练习' }}
              </el-tag>
              <span class="activity-text">{{ activity.title }}</span>
            </div>
          </el-timeline-item>
        </el-timeline>
      </el-card>
    </div>

    <!-- 欢迎卡片 -->
    <el-card class="welcome-card" v-if="stats[0]?.value === '0'">
      <div class="welcome-content">
        <div class="welcome-icon">👋</div>
        <h2>欢迎使用 AnswerMe</h2>
        <p>开始创建你的第一个题库吧！</p>
        <el-button type="primary" size="large" @click="router.push('/question-banks?action=create')">
          创建题库
        </el-button>
      </div>
    </el-card>
  </div>
</template>

<style scoped>
.home-view {
  max-width: 1400px;
  margin: 0 auto;
}

/* 统计卡片 */
.stats-row {
  margin-bottom: 1.5rem;
}

.stat-card {
  cursor: pointer;
  transition: all 0.2s;
}

.stat-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 1.75rem;
  font-weight: 700;
  color: #073642;
  margin-bottom: 0.25rem;
}

.stat-title {
  font-size: 0.875rem;
  color: #586E75;
  font-weight: 500;
}

/* 区块 */
.section {
  margin-bottom: 1.5rem;
}

.section-title {
  font-size: 1.125rem;
  font-weight: 700;
  color: #073642;
  margin: 0 0 1rem;
}

/* 快捷操作 */
.action-card {
  cursor: pointer;
  text-align: center;
  transition: all 0.2s;
  padding: 1.5rem;
  height: 100%;
}

.action-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.action-icon {
  font-size: 3rem;
  margin-bottom: 0.75rem;
  width: 80px;
  height: 80px;
  line-height: 80px;
  border-radius: 50%;
  margin: 0 auto 0.75rem;
}

.action-title {
  font-size: 1rem;
  font-weight: 600;
  color: #073642;
  margin-bottom: 0.5rem;
}

.action-description {
  font-size: 0.8125rem;
  color: #586E75;
  line-height: 1.5;
}

/* 活动卡片 */
.activity-card {
  background: #FFFFFF;
}

.activity-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.activity-text {
  font-size: 0.875rem;
  color: #657B83;
}

/* 欢迎卡片 */
.welcome-card {
  background: linear-gradient(135deg, #268BD2 0%, #2AA198 100%);
  border: none;
}

.welcome-card :deep(.el-card__body) {
  padding: 3rem;
}

.welcome-content {
  text-align: center;
}

.welcome-icon {
  font-size: 4rem;
  margin-bottom: 1rem;
}

.welcome-content h2 {
  margin: 0 0 0.75rem;
  font-size: 1.75rem;
  color: #FFFFFF;
  font-weight: 700;
}

.welcome-content p {
  margin: 0 0 1.5rem;
  font-size: 1rem;
  color: rgba(255, 255, 255, 0.95);
}
</style>
