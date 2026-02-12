<script setup lang="ts">
import { ref, computed, onUnmounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { Delete, MagicStick, DocumentCopy, ArrowLeft } from '@element-plus/icons-vue'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useAIGenerationStore } from '@/stores/aiGeneration'
import { useDataSourceStore } from '@/stores/dataSource'
import type { DataSource } from '@/api/dataSource'
import type { AIGenerateRequest } from '@/api/aiGeneration'
import type { GeneratedQuestion } from '@/api/aiGeneration'
import { getQuestionBanks } from '@/api/questionBank'
import type { QuestionBank } from '@/types'
import {
  isChoiceQuestionData,
  isBooleanQuestionData,
  isFillBlankQuestionData,
  isShortAnswerQuestionData,
  DifficultyLabels,
  DifficultyColors,
  getQuestionTypeLabel
} from '@/types/question'
import { formatQuestionForCopy } from '@/composables/useQuestionDisplay'

const router = useRouter()
const aiGenerationStore = useAIGenerationStore()
const dataSourceStore = useDataSourceStore()

// 表单数据
const formData = ref<AIGenerateRequest>({
  subject: '',
  count: 10,
  difficulty: 'medium',
  questionTypes: ['SingleChoice'],
  language: 'zh-CN',
  customPrompt: '',
  questionBankId: undefined,
  providerName: undefined
})

// 题型选项
const questionTypeOptions = [
  { label: '单选题', value: 'SingleChoice' },
  { label: '多选题', value: 'MultipleChoice' },
  { label: '判断题', value: 'TrueFalse' },
  { label: '填空题', value: 'FillBlank' },
  { label: '简答题', value: 'ShortAnswer' }
]

// 难度选项
const difficultyOptions = [
  { label: '简单', value: 'easy' },
  { label: '中等', value: 'medium' },
  { label: '困难', value: 'hard' }
]

// 语言选项
const languageOptions = [
  { label: '中文', value: 'zh-CN' },
  { label: 'English', value: 'en-US' }
]

// 数据源列表
const dataSources = ref<DataSource[]>([])
const selectedDataSource = ref<DataSource | null>(null)

// 题库列表
const questionBanks = ref<QuestionBank[]>([])
const selectedQuestionBank = ref<QuestionBank | null>(null)
const loadingQuestionBanks = ref(false)

// 高级选项展开状态
const showAdvancedOptions = ref(false)

// 计算属性
const isFormValid = computed(() => {
  return (
    formData.value.subject.trim().length > 0 &&
    formData.value.count > 0 &&
    formData.value.count <= 100 &&
    formData.value.questionTypes.length > 0 &&
    selectedDataSource.value !== null
  )
})

const willUseAsync = computed(
  () => formData.value.count >= 10 || formData.value.providerName === 'custom_api'
)

const canGenerate = computed(() => isFormValid.value && !aiGenerationStore.loading)

// 加载数据源
async function loadDataSources() {
  try {
    await dataSourceStore.fetchDataSources()
    dataSources.value = dataSourceStore.dataSources

    // 自动选择默认数据源
    const defaultDataSource = dataSources.value.find(ds => ds.isDefault)
    if (defaultDataSource) {
      selectedDataSource.value = defaultDataSource
      formData.value.providerName = defaultDataSource.type
    } else if (dataSources.value.length > 0) {
      const firstDs = dataSources.value[0]
      if (firstDs) {
        selectedDataSource.value = firstDs
        formData.value.providerName = firstDs.type
      }
    }
  } catch (error) {
    console.error('加载数据源失败:', error)
    ElMessage.error('加载数据源失败')
  }
}

// 加载题库列表
async function loadQuestionBanks() {
  loadingQuestionBanks.value = true
  try {
    const response = await getQuestionBanks({ pageSize: 100 })
    questionBanks.value = response.data || []
  } catch (error) {
    console.error('加载题库列表失败:', error)
  } finally {
    loadingQuestionBanks.value = false
  }
}

// 数据源选择变更
function handleDataSourceChange() {
  if (selectedDataSource.value) {
    formData.value.providerName = selectedDataSource.value.type
  }
}

// 题库选择变更
function handleQuestionBankChange() {
  if (selectedQuestionBank.value) {
    formData.value.questionBankId = selectedQuestionBank.value.id
  } else {
    formData.value.questionBankId = undefined
  }
}

// 生成题目
async function handleGenerate() {
  if (!isFormValid.value) {
    ElMessage.warning('请填写完整信息')
    return
  }

  await aiGenerationStore.generateQuestions(formData.value)

  if (aiGenerationStore.error) {
    ElMessage.error(aiGenerationStore.error)
  } else if (aiGenerationStore.isCompleted) {
    ElMessage.success(`成功生成 ${aiGenerationStore.generatedQuestions.length} 道题目`)
  }
}

// 取消生成
function handleCancel() {
  ElMessageBox.confirm('确定要取消生成吗？', '提示', {
    confirmButtonText: '确定',
    cancelButtonText: '继续生成',
    type: 'warning'
  })
    .then(() => {
      aiGenerationStore.cancelGeneration()
      ElMessage.info('已取消生成')
    })
    .catch(() => {
      // 用户点击取消
    })
}

// 重置表单
function handleReset() {
  aiGenerationStore.reset()
  formData.value = {
    subject: '',
    count: 10,
    difficulty: 'medium',
    questionTypes: ['SingleChoice'],
    language: 'zh-CN',
    customPrompt: '',
    questionBankId: undefined,
    providerName: selectedDataSource.value?.type
  }
}

// 复制题目文本
function copyQuestionText(question: GeneratedQuestion) {
  const text = formatQuestionForCopy(question)
  navigator.clipboard
    .writeText(text)
    .then(() => {
      ElMessage.success('已复制到剪贴板')
    })
    .catch(() => {
      ElMessage.error('复制失败')
    })
}

// 返回
function goBack() {
  router.back()
}

// 初始化
loadDataSources()
loadQuestionBanks()

// 清理轮询定时器
onUnmounted(() => {
  aiGenerationStore.stopProgressPolling()
})

watch(
  () => aiGenerationStore.taskStatus,
  status => {
    if (status === 'failed' && aiGenerationStore.progress?.errorMessage) {
      ElMessage.error(aiGenerationStore.progress.errorMessage)
    }
  }
)

// 辅助函数
function progressStatusType(status: string) {
  const typeMap: Record<string, 'success' | 'warning' | 'info' | 'danger'> = {
    pending: 'info',
    processing: 'warning',
    completed: 'success',
    partial_success: 'warning'
  }
  return typeMap[status] || 'danger'
}

function progressStatus(status: string) {
  return status === 'completed' ? 'success' : status === 'failed' ? 'exception' : undefined
}

function taskStatusText(status: string) {
  const textMap: Record<string, string> = {
    pending: '等待中',
    processing: '生成中',
    completed: '已完成',
    failed: '失败',
    partial_success: '部分成功'
  }
  return textMap[status] || status
}
</script>

<template>
  <div class="generate-container">
    <!-- 顶部导航 -->
    <header class="generate-header scroll-reveal">
      <div class="header-left">
        <el-button :icon="ArrowLeft" @click="goBack">返回</el-button>
        <h1 class="page-title">
          <el-icon><MagicStick /></el-icon>
          AI 生成题目
        </h1>
      </div>
    </header>

    <div class="generate-content">
      <!-- 左侧：生成表单 -->
      <div class="form-section">
        <el-card shadow="never">
          <template #header>
            <span class="card-title">生成配置</span>
          </template>

          <el-form :model="formData" label-width="100px" label-position="left">
            <!-- 主题 -->
            <el-form-item label="生成主题" required>
              <el-input
                v-model="formData.subject"
                placeholder="例如：Vue 3 Composition API"
                :disabled="aiGenerationStore.loading || aiGenerationStore.generating"
                clearable
              />
            </el-form-item>

            <!-- 数量和难度 -->
            <el-form-item label="题目数量" required>
              <el-input-number
                v-model="formData.count"
                :min="1"
                :max="100"
                :disabled="aiGenerationStore.loading || aiGenerationStore.generating"
              />
              <span class="form-hint">
                {{ willUseAsync ? '≥ 10题将使用异步生成' : '< 10题使用同步生成' }}
              </span>
            </el-form-item>

            <el-form-item label="难度等级">
              <el-select
                v-model="formData.difficulty"
                :disabled="aiGenerationStore.loading || aiGenerationStore.generating"
              >
                <el-option
                  v-for="item in difficultyOptions"
                  :key="item.value"
                  :label="item.label"
                  :value="item.value"
                />
              </el-select>
            </el-form-item>

            <!-- 题型 -->
            <el-form-item label="题型" required>
              <el-checkbox-group
                v-model="formData.questionTypes"
                :disabled="aiGenerationStore.loading || aiGenerationStore.generating"
              >
                <el-checkbox
                  v-for="type in questionTypeOptions"
                  :key="type.value"
                  :value="type.value"
                >
                  {{ type.label }}
                </el-checkbox>
              </el-checkbox-group>
            </el-form-item>

            <!-- 数据源选择 -->
            <el-form-item label="AI 数据源" required>
              <el-select
                v-model="selectedDataSource"
                :disabled="aiGenerationStore.loading || aiGenerationStore.generating"
                placeholder="选择数据源"
                value-key="id"
                @change="handleDataSourceChange"
              >
                <el-option v-for="ds in dataSources" :key="ds.id" :label="ds.name" :value="ds">
                  <span>{{ ds.name }}</span>
                  <el-tag v-if="ds.isDefault" size="small" type="success" style="margin-left: 8px">
                    默认
                  </el-tag>
                </el-option>
              </el-select>
              <div v-if="dataSources.length === 0" class="form-hint error">
                暂无数据源，请先
                <router-link to="/ai-config">配置数据源</router-link>
              </div>
            </el-form-item>

            <!-- 题库选择（可选） -->
            <el-form-item label="添加到题库">
              <el-select
                v-model="selectedQuestionBank"
                :disabled="aiGenerationStore.loading || aiGenerationStore.generating"
                placeholder="可选：选择目标题库"
                clearable
                filterable
                value-key="id"
                :loading="loadingQuestionBanks"
                @change="handleQuestionBankChange"
              >
                <el-option v-for="qb in questionBanks" :key="qb.id" :label="qb.name" :value="qb">
                  <span>{{ qb.name }}</span>
                  <span style="color: var(--color-text-muted); font-size: 12px; margin-left: 8px">
                    {{ qb.questionCount }} 题
                  </span>
                </el-option>
              </el-select>
            </el-form-item>

            <!-- 高级选项 -->
            <el-form-item>
              <el-button type="primary" text @click="showAdvancedOptions = !showAdvancedOptions">
                {{ showAdvancedOptions ? '收起' : '展开' }}高级选项
              </el-button>
            </el-form-item>

            <template v-if="showAdvancedOptions">
              <el-form-item label="语言">
                <el-select
                  v-model="formData.language"
                  :disabled="aiGenerationStore.loading || aiGenerationStore.generating"
                >
                  <el-option
                    v-for="lang in languageOptions"
                    :key="lang.value"
                    :label="lang.label"
                    :value="lang.value"
                  />
                </el-select>
              </el-form-item>

              <el-form-item label="自定义提示">
                <el-input
                  v-model="formData.customPrompt"
                  type="textarea"
                  :rows="3"
                  placeholder="可选：自定义生成要求"
                  :disabled="aiGenerationStore.loading || aiGenerationStore.generating"
                />
              </el-form-item>
            </template>
          </el-form>

          <!-- 操作按钮 -->
          <div class="action-buttons">
            <el-button
              v-if="!aiGenerationStore.generating"
              type="primary"
              :icon="MagicStick"
              :disabled="!canGenerate"
              :loading="aiGenerationStore.loading"
              @click="handleGenerate"
            >
              {{ willUseAsync ? '开始生成（异步）' : '立即生成' }}
            </el-button>
            <el-button v-else type="danger" :icon="Delete" @click="handleCancel">
              取消生成
            </el-button>
            <el-button :disabled="aiGenerationStore.generating" @click="handleReset">
              重置
            </el-button>
          </div>
        </el-card>

        <!-- 生成进度卡片 -->
        <el-card
          v-if="aiGenerationStore.generating || aiGenerationStore.progress"
          shadow="never"
          class="progress-card"
        >
          <template #header>
            <span class="card-title">生成进度</span>
          </template>

          <div class="progress-content">
            <div class="progress-info">
              <span class="progress-label">
                {{ aiGenerationStore.isAsyncTask ? '异步任务' : '同步任务' }}
              </span>
              <el-tag :type="progressStatusType(aiGenerationStore.taskStatus)">
                {{ taskStatusText(aiGenerationStore.taskStatus) }}
              </el-tag>
            </div>

            <el-progress
              :percentage="aiGenerationStore.progressPercentage"
              :status="progressStatus(aiGenerationStore.taskStatus)"
              :stroke-width="8"
            />

            <div v-if="aiGenerationStore.progress" class="progress-details">
              <span
                >{{ aiGenerationStore.progress.generatedCount }} /
                {{ aiGenerationStore.progress.totalCount }} 题</span
              >
            </div>

            <div
              v-if="
                aiGenerationStore.taskStatus === 'failed' &&
                aiGenerationStore.progress?.errorMessage
              "
              class="progress-error"
            >
              {{ aiGenerationStore.progress.errorMessage }}
            </div>

            <div v-if="aiGenerationStore.currentTaskId" class="task-id">
              <span class="label">任务ID:</span>
              <code>{{ aiGenerationStore.currentTaskId }}</code>
            </div>
          </div>
        </el-card>
      </div>

      <!-- 右侧：生成结果 -->
      <div class="result-section">
        <el-card shadow="never">
          <template #header>
            <div class="result-header">
              <span class="card-title">
                生成结果
                <span v-if="aiGenerationStore.generatedQuestions.length > 0" class="count">
                  ({{ aiGenerationStore.generatedQuestions.length }} 题)
                </span>
              </span>
            </div>
          </template>

          <!-- 空状态 -->
          <div v-if="aiGenerationStore.generatedQuestions.length === 0" class="empty-state">
            <el-empty description="暂无生成结果">
              <template #image>
                <el-icon :size="60" color="var(--color-text-muted)"><MagicStick /></el-icon>
              </template>
            </el-empty>
          </div>

          <!-- 题目列表 -->
          <div v-else class="questions-list">
            <div
              v-for="(question, index) in aiGenerationStore.generatedQuestions"
              :key="question.id"
              class="question-item"
            >
              <div class="question-header">
                <span class="question-number">第 {{ index + 1 }} 题</span>
                <div class="question-tags">
                  <el-tag
                    size="small"
                    :type="
                      DifficultyColors[question.difficulty as keyof typeof DifficultyColors] ||
                      'info'
                    "
                  >
                    {{
                      DifficultyLabels[question.difficulty as keyof typeof DifficultyLabels] ||
                      question.difficulty
                    }}
                  </el-tag>
                  <el-tag size="small" type="info">
                    {{
                      getQuestionTypeLabel(
                        question.questionTypeEnum || question.questionType || 'SingleChoice'
                      )
                    }}
                  </el-tag>
                </div>
              </div>

              <div class="question-content">{{ question.questionText }}</div>

              <!-- 选择题选项显示 -->
              <div v-if="isChoiceQuestionData(question.data)" class="question-options">
                <div
                  v-for="(option, optIndex) in question.data.options"
                  :key="optIndex"
                  class="option-item"
                >
                  {{ String.fromCharCode(65 + optIndex) }}. {{ option }}
                </div>
              </div>

              <div class="question-meta">
                <div class="answer-section">
                  <span class="label">正确答案:</span>
                  <!-- 选择题答案 -->
                  <span v-if="isChoiceQuestionData(question.data)" class="answer">
                    {{ question.data.correctAnswers.join(', ') }}
                  </span>
                  <!-- 判断题答案 -->
                  <span v-else-if="isBooleanQuestionData(question.data)" class="answer">
                    {{ question.data.correctAnswer ? '正确' : '错误' }}
                  </span>
                  <!-- 填空题答案 -->
                  <span v-else-if="isFillBlankQuestionData(question.data)" class="answer">
                    {{ question.data.acceptableAnswers.join(', ') }}
                  </span>
                  <!-- 简答题答案 -->
                  <span v-else-if="isShortAnswerQuestionData(question.data)" class="answer">
                    {{ question.data.referenceAnswer }}
                  </span>
                  <span v-else class="answer">{{ question.correctAnswer || '(未知)' }}</span>
                </div>

                <div v-if="question.explanation" class="explanation-section">
                  <span class="label">解析:</span>
                  <span class="explanation">{{ question.explanation }}</span>
                </div>
              </div>

              <div class="question-actions">
                <el-button size="small" :icon="DocumentCopy" @click="copyQuestionText(question)">
                  复制
                </el-button>
              </div>
            </div>
          </div>
        </el-card>
      </div>
    </div>
  </div>
</template>

<style scoped>
.generate-container {
  @apply min-h-screen p-6;
  background: linear-gradient(to bottom right, var(--color-bg-secondary), var(--color-bg-tertiary));
}

.dark .generate-container {
  background: linear-gradient(to bottom right, var(--color-bg-secondary), var(--color-bg));
}

.generate-header {
  @apply flex items-center justify-between mb-6;
}

.header-left {
  @apply flex items-center gap-4;
}

.page-title {
  @apply text-[1.5rem] font-semibold m-0 flex items-center gap-2;
  color: var(--color-text-primary);
  font-family: 'Noto Serif SC', 'Songti SC', serif;
}

.dark .page-title {
  color: var(--color-text-primary);
}

.generate-content {
  @apply grid gap-6 items-start;
  grid-template-columns: 450px 1fr;
}

.form-section {
  @apply flex flex-col gap-4 sticky top-6;
}

.card-title {
  @apply text-base font-semibold;
  color: var(--color-text-primary);
}

.dark .card-title {
  color: var(--color-text-primary);
}

.form-hint {
  @apply text-xs ml-2;
  color: var(--color-text-muted);
}

.form-hint.error {
  color: var(--color-danger);
}

.action-buttons {
  @apply flex gap-3 mt-4;
}

.progress-card {
  @apply border;
  border-color: var(--color-border);
  background-color: var(--color-bg-secondary);
}

.dark .progress-card {
  background-color: var(--color-bg-secondary);
  border-color: var(--color-border);
}

.progress-content {
  @apply flex flex-col gap-4;
}

.progress-info {
  @apply flex items-center justify-between;
}

.progress-label {
  @apply text-sm font-medium;
  color: var(--color-text-primary);
}

.dark .progress-label {
  color: var(--color-text-primary);
}

.progress-details {
  @apply text-sm text-center;
  color: var(--color-text-secondary);
}

.dark .progress-details {
  color: var(--color-text-secondary);
}

.progress-error {
  @apply text-xs px-3 py-2 rounded-md;
  color: var(--color-danger-text);
  background-color: var(--color-danger-light);
  border-color: var(--color-danger);
}

.dark .progress-error {
  color: var(--color-danger);
  background-color: rgba(139, 58, 58, 0.15);
  border-color: var(--color-danger);
}

.task-id {
  @apply flex items-center gap-2 text-xs px-2 py-2 bg-bg rounded-md;
  background-color: var(--color-bg-secondary);
}

.task-id .label {
  color: var(--color-text-secondary);
}

.dark .task-id {
  background-color: var(--color-bg);
}

.task-id code {
  @apply font-mono text-xs;
  color: var(--color-primary);
}

.result-section {
  @apply min-h-[500px];
}

.result-header {
  @apply flex items-center justify-between;
}

.count {
  @apply text-sm font-normal;
  color: var(--color-text-secondary);
}

.dark .count {
  color: var(--color-text-secondary);
}

.empty-state {
  @apply py-12 flex justify-center;
}

.questions-list {
  @apply flex flex-col gap-4;
}

.question-item {
  @apply px-4 py-4 border rounded-md bg-bg;
  border-color: var(--color-border);
}

.dark .question-item {
  background: var(--color-bg-secondary);
  border-color: var(--color-border);
}

.question-header {
  @apply flex items-center justify-between mb-3;
}

.question-number {
  @apply text-sm font-semibold;
  color: var(--color-primary);
}

.question-tags {
  @apply flex gap-1.5;
}

.question-content {
  @apply text-[0.9375rem] leading-[1.6] mb-3;
  color: var(--color-text-primary);
}

.dark .question-content {
  color: var(--color-text-primary);
}

.question-options {
  @apply flex flex-col gap-1.5 mb-3 pl-4;
}

.option-item {
  @apply text-sm;
  color: var(--color-text-secondary);
}

.dark .option-item {
  color: var(--color-text-secondary);
}

.question-meta {
  @apply flex flex-col gap-2 px-3 py-3 rounded-md mb-3;
  background-color: var(--color-bg-secondary);
  border: 1px solid var(--color-border);
}

.dark .question-meta {
  background-color: var(--color-bg-tertiary);
  border-color: var(--color-border);
}

.answer-section,
.explanation-section {
  @apply flex gap-2 text-sm;
}

.answer-section .label {
  @apply font-semibold whitespace-nowrap;
  color: var(--color-accent);
}

.answer-section .answer {
  @apply font-medium;
  color: var(--color-text-primary);
}

.dark .answer-section .answer {
  color: var(--color-text-primary);
}

.explanation-section .label {
  @apply font-semibold whitespace-nowrap;
  color: var(--color-accent);
}

.explanation-section .explanation {
  color: var(--color-text-primary);
}

.dark .explanation-section .explanation {
  color: var(--color-text-primary);
}

.question-actions {
  @apply flex gap-2 justify-end;
}
</style>
