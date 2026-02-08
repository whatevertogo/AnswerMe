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
    <el-card class="welcome-card" v-if="stats[0].value === '0'">
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
  margin-bottom: 24px;
}

.stat-card {
  cursor: pointer;
  transition: all 0.3s;
}

.stat-card:hover {
  transform: translateY(-4px);
}

.stat-content {
  display: flex;
  align-items: center;
  gap: 16px;
}

.stat-icon {
  width: 56px;
  height: 56px;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.stat-info {
  flex: 1;
}

.stat-value {
  font-size: 28px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 4px;
}

.stat-title {
  font-size: 14px;
  color: #909399;
}

/* 区块 */
.section {
  margin-bottom: 24px;
}

.section-title {
  font-size: 18px;
  font-weight: 600;
  color: #303133;
  margin: 0 0 16px;
}

/* 快捷操作 */
.action-card {
  cursor: pointer;
  text-align: center;
  transition: all 0.3s;
  padding: 24px;
}

.action-card:hover {
  transform: translateY(-4px);
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.15);
}

.action-icon {
  font-size: 48px;
  margin-bottom: 12px;
  width: 80px;
  height: 80px;
  line-height: 80px;
  border-radius: 50%;
  margin: 0 auto 12px;
}

.action-title {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
  margin-bottom: 8px;
}

.action-description {
  font-size: 13px;
  color: #909399;
  line-height: 1.5;
}

/* 活动卡片 */
.activity-card {
  background: #fff;
}

.activity-item {
  display: flex;
  align-items: center;
  gap: 8px;
}

.activity-text {
  font-size: 14px;
  color: #606266;
}

/* 欢迎卡片 */
.welcome-card {
  background: linear-gradient(135deg, #667eea 0%, #764ba2 100%);
  border: none;
}

.welcome-card :deep(.el-card__body) {
  padding: 48px;
}

.welcome-content {
  text-align: center;
}

.welcome-icon {
  font-size: 64px;
  margin-bottom: 16px;
}

.welcome-content h2 {
  margin: 0 0 12px;
  font-size: 28px;
  color: #fff;
}

.welcome-content p {
  margin: 0 0 24px;
  font-size: 16px;
  color: rgba(255, 255, 255, 0.9);
}
</style>
