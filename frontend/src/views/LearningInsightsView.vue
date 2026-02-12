<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { DataAnalysis, Clock, CircleCheck, Warning, TrendCharts } from '@element-plus/icons-vue'
import { useWrongQuestionStore } from '@/stores/wrongQuestion'
import type { LearningStatsResponse } from '@/api/wrongQuestion'

const router = useRouter()
const wrongQuestionStore = useWrongQuestionStore()

const loading = ref(false)
const stats = ref<LearningStatsResponse | null>(null)

// 统计数据
const totalAttempts = computed(() => stats.value?.totalAttempts ?? 0)
const accuracyRate = computed(() => stats.value?.accuracyRate ?? 0)
const averageTime = computed(() => stats.value?.averageTimePerQuestion ?? 0)
const totalTime = computed(() => stats.value?.totalTimeSpent ?? 0)

// 格式化时间
function formatDuration(seconds: number): string {
  if (seconds < 60) return `${seconds}秒`
  if (seconds < 3600) return `${Math.floor(seconds / 60)}分${seconds % 60}秒`
  const hours = Math.floor(seconds / 3600)
  const minutes = Math.floor((seconds % 3600) / 60)
  return `${hours}小时${minutes}分`
}

// 题库统计
const bankStats = computed(() => stats.value?.bankStats ?? [])

// 趋势数据（简化显示）
const weeklyTrend = computed(() => stats.value?.weeklyTrend ?? [])

onMounted(async () => {
  await loadStats()
})

async function loadStats() {
  loading.value = true
  try {
    stats.value = await wrongQuestionStore.fetchStats()
  } catch (e) {
    ElMessage.error('加载学习统计失败')
  } finally {
    loading.value = false
  }
}

function goToWrongQuestions() {
  router.push('/wrong-questions')
}

function goToPractice(bankId?: number) {
  if (bankId) {
    router.push(`/practice?bankId=${bankId}`)
  } else {
    router.push('/practice')
  }
}

function formatWeekLabel(weekStart: string): string {
  const date = new Date(weekStart)
  return `${date.getMonth() + 1}/${date.getDate()}`
}
</script>

<template>
  <div v-loading="loading" class="learning-insights-container">
    <!-- 顶部统计卡片 -->
    <div class="stats-cards">
      <div class="stat-card">
        <div class="stat-icon">
          <el-icon :size="32"><DataAnalysis /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ totalAttempts }}</div>
          <div class="stat-label">答题次数</div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon success">
          <el-icon :size="32"><CircleCheck /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ accuracyRate }}%</div>
          <div class="stat-label">正确率</div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon warning">
          <el-icon :size="32"><Clock /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ averageTime }}秒</div>
          <div class="stat-label">平均用时</div>
        </div>
      </div>

      <div class="stat-card">
        <div class="stat-icon info">
          <el-icon :size="32"><TrendCharts /></el-icon>
        </div>
        <div class="stat-content">
          <div class="stat-value">{{ formatDuration(totalTime) }}</div>
          <div class="stat-label">总学习时长</div>
        </div>
      </div>
    </div>

    <!-- 题库掌握情况 -->
    <div class="section">
      <div class="section-header">
        <h3>题库掌握情况</h3>
      </div>

      <div v-if="bankStats.length > 0" class="bank-stats">
        <div
          v-for="bank in bankStats"
          :key="bank.questionBankId"
          class="bank-stat-card"
          @click="goToPractice(bank.questionBankId)"
        >
          <div class="bank-stat-header">
            <span class="bank-name">{{ bank.questionBankName }}</span>
            <span class="attempt-count">{{ bank.attemptCount }}次答题</span>
          </div>

          <div class="progress-bar">
            <div
              class="progress-fill"
              :style="{ width: `${bank.accuracyRate}%` }"
              :class="{
                high: bank.accuracyRate >= 80,
                medium: bank.accuracyRate >= 60 && bank.accuracyRate < 80,
                low: bank.accuracyRate < 60
              }"
            />
          </div>

          <div class="bank-stat-footer">
            <span>{{ bank.correctCount }}/{{ bank.totalQuestions }}</span>
            <span class="accuracy">{{ bank.accuracyRate }}%</span>
          </div>
        </div>
      </div>

      <el-empty v-else description="暂无答题记录">
        <el-button type="primary" @click="goToPractice()"> 开始答题 </el-button>
      </el-empty>
    </div>

    <!-- 正确率趋势 -->
    <div v-if="weeklyTrend.length > 0" class="section">
      <div class="section-header">
        <h3>正确率趋势 (近12周)</h3>
      </div>

      <div class="trend-chart">
        <div v-for="week in weeklyTrend" :key="week.weekStart" class="trend-bar">
          <div
            class="bar"
            :style="{ height: `${week.accuracyRate}%` }"
            :title="`${week.accuracyRate}% (${week.correctCount}/${week.questionCount})`"
          />
          <div class="bar-label">{{ formatWeekLabel(week.weekStart) }}</div>
        </div>
      </div>
    </div>

    <!-- 错题提醒 -->
    <div class="section">
      <div class="section-header">
        <h3>错题提醒</h3>
        <el-button type="primary" size="small" @click="goToWrongQuestions"> 查看错题本 </el-button>
      </div>

      <div class="wrong-reminder">
        <el-icon :size="40" color="var(--color-warning)"><Warning /></el-icon>
        <div class="reminder-content">
          <p>
            你当前共有 <strong>{{ stats?.wrongCount ?? 0 }}</strong> 道错题
          </p>
          <p class="reminder-hint">定期复习错题是提升成绩的关键！</p>
        </div>
        <el-button type="warning" @click="goToWrongQuestions"> 复习错题 </el-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.learning-insights-container {
  @apply p-6;
}

.stats-cards {
  @apply grid grid-cols-2 md:grid-cols-4 gap-4 mb-6;
}

.stat-card {
  @apply flex items-center gap-4 p-4 bg-bg rounded-lg border border-border;
}

.stat-icon {
  @apply flex items-center justify-center w-14 h-14 rounded-full;
  background-color: color-mix(in srgb, var(--color-primary) 10%, transparent);
  color: var(--color-primary);
}

.stat-icon.success {
  background-color: color-mix(in srgb, var(--color-success) 10%, transparent);
  color: var(--color-success);
}

.stat-icon.warning {
  background-color: color-mix(in srgb, var(--color-warning) 10%, transparent);
  color: var(--color-warning);
}

.stat-icon.info {
  background-color: color-mix(in srgb, var(--color-info) 10%, transparent);
  color: var(--color-info);
}

.stat-content {
  @apply flex-1;
}

.stat-value {
  @apply text-2xl font-bold text-text-primary;
}

.stat-label {
  @apply text-sm text-text-secondary;
}

.section {
  @apply mb-6;
}

.section-header {
  @apply flex justify-between items-center mb-4;
}

.section-header h3 {
  @apply text-lg font-semibold text-text-primary m-0;
}

.bank-stats {
  @apply grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-4;
}

.bank-stat-card {
  @apply p-4 bg-bg rounded-lg border border-border cursor-pointer transition-all hover:shadow-md;
}

.bank-stat-header {
  @apply flex justify-between items-center mb-3;
}

.bank-name {
  @apply font-medium text-text-primary;
}

.attempt-count {
  @apply text-xs text-text-muted;
}

.progress-bar {
  @apply h-2 bg-bg-secondary rounded-full overflow-hidden mb-2;
}

.progress-fill {
  @apply h-full rounded-full transition-all;
}

.progress-fill.high {
  @apply bg-success;
}

.progress-fill.medium {
  @apply bg-warning;
}

.progress-fill.low {
  @apply bg-danger;
}

.bank-stat-footer {
  @apply flex justify-between text-sm text-text-secondary;
}

.accuracy {
  @apply font-medium text-text-primary;
}

.trend-chart {
  @apply flex items-end gap-2 h-40 p-4 bg-bg rounded-lg border border-border;
}

.trend-bar {
  @apply flex-1 flex flex-col items-center;
}

.bar {
  @apply w-full bg-primary rounded-t transition-all;
  min-height: 4px;
}

.bar-label {
  @apply text-xs text-text-muted mt-2;
}

.wrong-reminder {
  @apply flex items-center gap-4 p-4 rounded-lg;
  background-color: color-mix(in srgb, var(--color-warning) 10%, transparent);
  border: 1px solid color-mix(in srgb, var(--color-warning) 20%, transparent);
}

.reminder-content {
  @apply flex-1;
}

.reminder-content p {
  @apply m-0;
}

.reminder-content strong {
  @apply text-warning;
}

.reminder-hint {
  @apply text-sm text-text-secondary mt-1;
}
</style>
