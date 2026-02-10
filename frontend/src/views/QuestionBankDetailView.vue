<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import {
  ArrowLeft,
  Edit,
  Delete,
  Plus,
  MagicStick,
  Download,
  Refresh
} from '@element-plus/icons-vue'
import { useQuestionBankStore } from '@/stores/questionBank'
import QuestionBankForm from '@/components/QuestionBankForm.vue'
import type { QuestionBank } from '@/stores/questionBank'
import { getQuestionTypeLabel, getQuestionOptions, getQuestionCorrectAnswers } from '@/types/question'

const route = useRoute()
const router = useRouter()
const questionBankStore = useQuestionBankStore()

const loading = ref(false)
const formDialogVisible = ref(false)
const formMode = ref<'edit'>('edit')
const currentBank = ref<QuestionBank | null>(null)

const bankId = computed(() => parseInt(route.params.id as string))
const currentQuestionBank = computed(() => questionBankStore.currentBank)
const questions = computed(() => questionBankStore.questions)

// 过滤掉可能的 null 值，确保始终返回数组
const filteredQuestions = computed(() => {
  if (!Array.isArray(questions.value)) {
    console.warn('questions.value is not an array:', questions.value)
    return []
  }
  return questions.value.filter(q => q != null && q.id != null)
})

onMounted(async () => {
  await fetchBankDetail()
})

const fetchBankDetail = async () => {
  loading.value = true
  try {
    await questionBankStore.fetchQuestionBank(bankId.value)
  } catch {
    ElMessage.error('获取题库详情失败')
    router.back()
  } finally {
    loading.value = false
  }
}

const handleBack = () => {
  router.push('/question-banks')
}

const handleEdit = () => {
  if (currentQuestionBank.value) {
    currentBank.value = currentQuestionBank.value
    formMode.value = 'edit'
    formDialogVisible.value = true
  }
}

const handleDelete = async () => {
  if (!currentQuestionBank.value) return

  try {
    await ElMessageBox.confirm(
      `确定要删除题库"${currentQuestionBank.value.name}"吗？删除后无法恢复。`,
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    await questionBankStore.deleteQuestionBank(currentQuestionBank.value.id)
    ElMessage.success('删除成功')
    router.push('/question-banks')
  } catch {
    // 用户取消
  }
}

const handleGenerateQuestions = () => {
  router.push(`/generate?bankId=${bankId.value}`)
}

const handleAddQuestion = () => {
  ElMessage.info('手动添加题目功能开发中...')
}

const handleExport = async () => {
  if (!currentQuestionBank.value) return

  try {
    // 调用后端导出API
    const response = await fetch(`/api/questionbanks/${bankId.value}/export`, {
      headers: {
        'Authorization': `Bearer ${localStorage.getItem('token')}`
      }
    })

    if (!response.ok) {
      throw new Error('导出失败')
    }

    // 获取文件名
    const contentDisposition = response.headers.get('Content-Disposition')
    let fileName = `${currentQuestionBank.value.name}.json`
    if (contentDisposition) {
      const fileNameMatch = contentDisposition.match(/filename[^;=\n]*=((['"]).*?\2|[^;\n]*)/)
      if (fileNameMatch && fileNameMatch[1]) {
        fileName = fileNameMatch[1].replace(/['"]/g, '')
      }
    }

    // 下载文件
    const blob = await response.blob()
    const url = URL.createObjectURL(blob)
    const link = document.createElement('a')
    link.href = url
    link.download = fileName
    document.body.appendChild(link)
    link.click()
    document.body.removeChild(link)
    URL.revokeObjectURL(url)

    ElMessage.success('导出成功')
  } catch (error) {
    console.error('导出失败:', error)
    ElMessage.error('导出失败')
  }
}

const handleFormSuccess = (_bank: QuestionBank) => {
  formDialogVisible.value = false
  ElMessage.success('更新题库成功')
  fetchBankDetail()
}

const handleFormClose = () => {
  formDialogVisible.value = false
  currentBank.value = null
}

// 题目类型标签颜色
const getQuestionTypeTagType = (type: string): string => {
  const typeMap: Record<string, string> = {
    'SingleChoice': 'success',
    'MultipleChoice': 'warning',
    'TrueFalse': 'info',
    'FillBlank': '',
    'ShortAnswer': ''
  }
  return typeMap[type] || ''
}

// 题目难度标签颜色
const getDifficultyTagType = (difficulty: string): string => {
  const typeMap: Record<string, string> = {
    'easy': 'success',
    'medium': 'warning',
    'hard': 'danger'
  }
  return typeMap[difficulty] || ''
}

// 获取题目显示文本
const getQuestionDisplayText = (question: any) => {
  return question.questionText || ''
}

// 获取题型显示文本
const getQuestionTypeDisplay = (question: any) => {
  return getQuestionTypeLabel(question.questionTypeEnum)
}

// 获取正确答案显示文本
const getCorrectAnswerDisplay = (question: any): string => {
  const answer = getQuestionCorrectAnswers(question)
  if (Array.isArray(answer)) {
    return answer.join(', ')
  }
  if (typeof answer === 'boolean') {
    return answer ? '正确' : '错误'
  }
  return String(answer)
}

// 获取题目索引（用于选项字母）
const getOptionIndex = (index: number) => {
  return String.fromCharCode(65 + index)
}
</script>

<template>
  <div v-loading="loading" class="question-bank-detail">
    <!-- 返回按钮和标题 -->
    <div class="page-header">
      <el-button :icon="ArrowLeft" @click="handleBack">返回题库列表</el-button>
      <h2 v-if="currentQuestionBank">{{ currentQuestionBank.name }}</h2>
    </div>

    <!-- 题库信息 -->
    <el-card v-if="currentQuestionBank" class="bank-info">
      <div class="info-header">
        <div class="info-content">
          <p class="description">{{ currentQuestionBank.description || '暂无描述' }}</p>
          <div class="meta">
            <span
              ><el-icon><Document /></el-icon> 共 {{ currentQuestionBank.questionCount }} 道题目</span
            >
            <span
              ><el-icon><Clock /></el-icon> 创建于
              {{ new Date(currentQuestionBank.createdAt).toLocaleDateString() }}</span
            >
            <span
              ><el-icon><Clock /></el-icon> 更新于
              {{ new Date(currentQuestionBank.updatedAt).toLocaleDateString() }}</span
            >
          </div>
        </div>
        <div class="info-actions">
          <el-button :icon="Edit" @click="handleEdit">编辑信息</el-button>
          <el-button type="danger" :icon="Delete" @click="handleDelete">删除题库</el-button>
        </div>
      </div>
    </el-card>

    <!-- 操作按钮区 -->
    <div class="action-bar">
      <el-button type="primary" :icon="MagicStick" @click="handleGenerateQuestions">
        AI 生成题目
      </el-button>
      <el-button :icon="Plus" @click="handleAddQuestion">手动添加</el-button>
      <el-button :icon="Download" @click="handleExport">导出题库</el-button>
      <el-button :icon="Refresh" @click="fetchBankDetail">刷新</el-button>
    </div>

    <!-- 题目列表 -->
    <el-card class="questions-section">
      <template #header>
        <span>题目列表</span>
      </template>

      <el-empty
        v-if="questions.length === 0"
        description="暂无题目"
        :image-size="120"
      >
        <el-button type="primary" @click="handleGenerateQuestions">AI 生成题目</el-button>
        <el-button @click="handleAddQuestion">手动添加</el-button>
      </el-empty>

      <div v-else class="questions-list">
        <div
          v-for="(question, index) in filteredQuestions"
          :key="question.id"
          class="question-item"
        >
          <div class="question-header">
            <span class="question-number">#{{ index + 1 }}</span>
            <el-tag size="small" :type="getQuestionTypeTagType(question.questionTypeEnum)">
              {{ getQuestionTypeDisplay(question) }}
            </el-tag>
            <el-tag size="small" :type="getDifficultyTagType(question.difficulty)">
              {{ question.difficulty === 'easy'
                ? '简单'
                : question.difficulty === 'medium'
                  ? '中等'
                  : '困难' }}
            </el-tag>
          </div>

          <div class="question-content">
            <p>{{ getQuestionDisplayText(question) }}</p>
          </div>

          <div v-if="getQuestionOptions(question).length > 0" class="question-options">
            <p class="options-label">选项：</p>
            <ul>
              <li v-for="(option, optIndex) in getQuestionOptions(question)" :key="optIndex">
                {{ getOptionIndex(optIndex as number) }}. {{ option }}
              </li>
            </ul>
          </div>

          <div class="question-answer">
            <span class="answer-label">答案：</span>
            <span>{{ getCorrectAnswerDisplay(question) }}</span>
          </div>

          <div v-if="question.explanation" class="question-explanation">
            <span class="explanation-label">解析：</span>
            <span>{{ question.explanation }}</span>
          </div>

          <div v-if="question.tags && question.tags.length > 0" class="question-tags">
            <el-tag
              v-for="tag in question.tags"
              :key="tag"
              size="small"
              type="info"
            >
              {{ tag }}
            </el-tag>
          </div>
        </div>
      </div>
    </el-card>

    <!-- 编辑题库表单 -->
    <QuestionBankForm
      :visible="formDialogVisible"
      :mode="formMode"
      :bank="currentBank"
      @close="handleFormClose"
      @success="handleFormSuccess"
    />
  </div>
</template>

<script lang="ts">
// 为了使用 Element Plus 图标，需要导入
import { Document, Clock } from '@element-plus/icons-vue'
export default {
  components: {
    Document,
    Clock
  }
}
</script>

<style scoped>
.question-bank-detail {
  @apply min-h-[calc(100vh-140px)];
}

.page-header {
  @apply flex items-center gap-4 mb-6;
}

.page-header h2 {
  @apply m-0 text-[1.5rem] font-bold;
  color: var(--color-text-primary);
}

.bank-info {
  @apply mb-6;
}

.info-header {
  @apply flex justify-between items-start gap-6;
}

.description {
  @apply text-[0.9375rem] mb-3 leading-[1.6];
  color: var(--color-text-secondary);
}

.meta {
  @apply flex gap-6 text-[0.8125rem] flex-wrap;
  color: var(--color-text-secondary);
}

.meta span {
  @apply flex items-center gap-1.5;
}

.info-actions {
  @apply flex gap-3;
}

.action-bar {
  @apply flex gap-3 mb-6 px-4 py-4 rounded-lg;
  background-color: var(--color-bg-tertiary);
}

.questions-section {
  @apply min-h-[200px];
}

.questions-list {
  @apply grid gap-4;
  grid-template-columns: repeat(auto-fill, minmax(600px, 1fr));
}

.question-item {
  @apply px-4 py-4 rounded-lg bg-bg
         transition-all duration-150
         hover:shadow-[0_2px_8px_rgba(0,0,0,0.06)];
  border: 1px solid var(--color-border);
}

.question-item:hover {
  border-color: var(--color-primary);
}

.question-header {
  @apply flex items-center gap-3 mb-3;
}

.question-number {
  @apply font-semibold text-sm;
  color: var(--color-primary);
}

.question-content {
  @apply text-[0.9375rem] leading-[1.6] mb-3;
  color: var(--color-text-primary);
}

.question-options {
  @apply mb-3 pl-5;
}

.options-label {
  @apply text-xs mb-2 font-medium;
  color: var(--color-text-secondary);
}

.question-options ul {
  @apply m-0 p-0 list-none;
}

.question-options li {
  @apply py-1 text-sm;
  color: var(--color-text-secondary);
}

.question-answer {
  @apply mb-2 px-3 py-2 bg-[#DBF0E8] rounded-md border border-[#2AA198];
}

.answer-label {
  @apply font-medium text-[#2AA198] text-xs;
}

.question-explanation {
  @apply mb-3 px-3 py-2 bg-[#FDF6E3] rounded-md border border-[#EEE8D5];
}

.explanation-label {
  @apply font-medium text-[#B58900] text-xs;
}

.question-tags {
  @apply flex gap-2 flex-wrap;
}

/* 响应式 */
@media (max-width: 768px) {
  .info-header {
    @apply flex-col;
  }

  .questions-list {
    @apply grid-cols-1;
  }

  .action-bar {
    @apply flex-wrap;
  }
}
</style>
