<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, View, Edit, Delete, Search, Refresh, ArrowLeft } from '@element-plus/icons-vue'
import { useQuestionStore } from '@/stores/question'
import { useQuestionBankStore } from '@/stores/questionBank'
import QuestionForm from '@/components/QuestionForm.vue'
import type { Question } from '@/stores/question'
import {
  getQuestionCorrectAnswers,
  getQuestionOptions,
  getQuestionTypeLabel,
  DifficultyLabels,
  DifficultyColors,
  QuestionType
} from '@/types/question'

const route = useRoute()
const questionStore = useQuestionStore()
const questionBankStore = useQuestionBankStore()

const loading = ref(false)
const searchKeyword = ref('')
const selectedType = ref<QuestionType | ''>('')
const selectedDifficulty = ref<string>('')
const searchTimer = ref<number | null>(null)

// 表单弹窗状态
const formDialogVisible = ref(false)
const formMode = ref<'create' | 'edit'>('create')
const currentQuestion = ref<Question | null>(null)

// 当前题库ID
const currentBankId = computed(
  () => (route.params.bankId as string) || (route.query.bankId as string) || ''
)

onMounted(async () => {
  if (currentBankId.value) {
    // 先加载题库信息
    await questionBankStore.fetchQuestionBank(Number(currentBankId.value))
  }
  await fetchQuestions()
})

const fetchQuestions = async (options?: { append?: boolean }) => {
  loading.value = true
  try {
    const lastId = options?.append ? (questionStore.pagination.nextCursor ?? undefined) : undefined
    await questionStore.fetchQuestions(
      {
        pageSize: 20,
        lastId,
        questionBankId: Number(currentBankId.value) || undefined,
        questionTypeEnum: selectedType.value || undefined,
        difficulty: selectedDifficulty.value || undefined,
        search: searchKeyword.value || undefined
      },
      { append: options?.append }
    )
  } catch {
    ElMessage.error('获取题目列表失败')
  } finally {
    loading.value = false
  }
}

// 搜索防抖
watch([searchKeyword, selectedType, selectedDifficulty], () => {
  if (searchTimer.value) {
    clearTimeout(searchTimer.value)
  }
  searchTimer.value = window.setTimeout(() => {
    fetchQuestions()
  }, 300)
})

const handleCreate = () => {
  formMode.value = 'create'
  currentQuestion.value = null
  formDialogVisible.value = true
}

const handleView = (_question: Question) => {
  // 可以实现查看详情的逻辑
  ElMessage.info('查看题目详情功能开发中')
}

const handleEdit = (question: Question) => {
  formMode.value = 'edit'
  currentQuestion.value = question
  formDialogVisible.value = true
}

const handleDelete = async (question: Question) => {
  try {
    const questionText = question.questionText || ''
    const displayText = questionText.substring(0, 50)

    await ElMessageBox.confirm(
      `确定要删除题目"${displayText}..."吗？删除后无法恢复。`,
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    await questionStore.deleteQuestion(question.id)
    ElMessage.success('删除成功')
    await fetchQuestions()
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handleFormSuccess = (_question: Question) => {
  formDialogVisible.value = false
  if (formMode.value === 'create') {
    ElMessage.success('创建题目成功')
  } else {
    ElMessage.success('更新题目成功')
  }
  fetchQuestions()
}

const handleFormClose = () => {
  formDialogVisible.value = false
  currentQuestion.value = null
}

const handleBackToBank = () => {
  if (currentBankId.value) {
    window.history.back()
  }
}

// 题型颜色映射（与标签不同，保留在本地）
const getQuestionTypeColor = (type: string) => {
  const colors: Record<string, string> = {
    SingleChoice: 'primary',
    MultipleChoice: 'success',
    TrueFalse: 'warning',
    FillBlank: 'info',
    ShortAnswer: 'info',
    choice: 'primary',
    'multiple-choice': 'success',
    'true-false': 'warning',
    'short-answer': 'info'
  }
  return colors[type] || 'info'
}

const formatCorrectAnswer = (question: Question) => {
  const answer = getQuestionCorrectAnswers(question)

  if (answer === undefined || answer === null || answer === '') return '-'

  if (typeof answer === 'boolean') {
    return answer ? '正确' : '错误'
  }
  if (Array.isArray(answer)) {
    return answer.join(', ')
  }
  return answer
}
</script>

<template>
  <div class="questions-view">
    <!-- 页面标题和操作 -->
    <div class="page-header">
      <div class="header-content">
        <div class="header-title">
          <el-button
            v-if="currentBankId"
            :icon="ArrowLeft"
            circle
            size="small"
            class="back-button"
            @click="handleBackToBank"
          />
          <div>
            <h2 class="page-title">
              {{
                currentBankId
                  ? `${questionBankStore.currentBank?.name || '题库'} - 题目管理`
                  : '题目管理'
              }}
            </h2>
            <p class="page-subtitle">管理题目,创建和编辑题目内容</p>
          </div>
        </div>
      </div>
      <el-button type="primary" :icon="Plus" @click="handleCreate"> 创建题目 </el-button>
    </div>

    <!-- 搜索和筛选 -->
    <div class="filter-bar">
      <el-input
        v-model="searchKeyword"
        placeholder="搜索题目内容..."
        :prefix-icon="Search"
        clearable
        class="search-input"
        @clear="fetchQuestions()"
      />
      <el-select v-model="selectedType" placeholder="题型筛选" clearable class="filter-select">
        <el-option label="单选题" :value="QuestionType.SingleChoice" />
        <el-option label="多选题" :value="QuestionType.MultipleChoice" />
        <el-option label="判断题" :value="QuestionType.TrueFalse" />
        <el-option label="填空题" :value="QuestionType.FillBlank" />
        <el-option label="简答题" :value="QuestionType.ShortAnswer" />
      </el-select>
      <el-select
        v-model="selectedDifficulty"
        placeholder="难度筛选"
        clearable
        class="filter-select"
      >
        <el-option label="简单" value="easy" />
        <el-option label="中等" value="medium" />
        <el-option label="困难" value="hard" />
      </el-select>
      <el-button :icon="Refresh" @click="fetchQuestions()">刷新</el-button>
    </div>

    <!-- 题目表格 -->
    <el-card class="table-card" shadow="never">
      <el-table v-loading="loading" :data="questionStore.questions" style="width: 100%" stripe>
        <el-table-column prop="questionText" label="题目内容" min-width="300">
          <template #default="{ row }">
            <div class="question-content">
              <span class="question-text" :title="row.questionText">{{ row.questionText }}</span>
              <div class="question-meta">
                <el-tag :type="getQuestionTypeColor(row.questionTypeEnum)" size="small">
                  {{ getQuestionTypeLabel(row.questionTypeEnum) }}
                </el-tag>
                <el-tag
                  v-if="row.difficulty"
                  :type="
                    DifficultyColors[row.difficulty as keyof typeof DifficultyColors] || 'info'
                  "
                  size="small"
                >
                  {{ DifficultyLabels[row.difficulty as keyof typeof DifficultyLabels] || '未知' }}
                </el-tag>
              </div>
            </div>
          </template>
        </el-table-column>

        <el-table-column label="选项/答案" min-width="250">
          <template #default="{ row }">
            <div class="answer-cell">
              <div
                v-if="
                  row.questionTypeEnum === QuestionType.SingleChoice ||
                  row.questionTypeEnum === QuestionType.MultipleChoice
                "
                class="options-preview"
              >
                <div
                  v-for="(opt, idx) in getQuestionOptions(row).slice(0, 2)"
                  :key="idx"
                  class="option-line"
                >
                  {{ String.fromCharCode(65 + (idx as number)) }}. {{ opt }}
                </div>
                <span v-if="getQuestionOptions(row).length > 2" class="more-hint">
                  +{{ getQuestionOptions(row).length - 2 }} 个选项
                </span>
              </div>
              <div v-else class="answer-preview">
                {{ formatCorrectAnswer(row) }}
              </div>
              <div class="correct-answer">
                <strong>正确答案:</strong> {{ formatCorrectAnswer(row) }}
              </div>
            </div>
          </template>
        </el-table-column>

        <el-table-column label="标签" width="200">
          <template #default="{ row }">
            <div v-if="row.tags && row.tags.length > 0" class="tags-cell">
              <el-tag
                v-for="tag in row.tags.slice(0, 3)"
                :key="tag"
                size="small"
                style="margin-right: 4px; margin-bottom: 4px"
              >
                {{ tag }}
              </el-tag>
              <span v-if="row.tags.length > 3" class="more-tags"> +{{ row.tags.length - 3 }} </span>
            </div>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>

        <el-table-column label="操作" width="240" fixed="right">
          <template #default="{ row }">
            <div class="table-actions">
              <el-button type="primary" size="small" :icon="View" @click="handleView(row)">
                查看
              </el-button>
              <el-button size="small" :icon="Edit" @click="handleEdit(row)"> 编辑 </el-button>
              <el-button type="danger" size="small" :icon="Delete" @click="handleDelete(row)">
                删除
              </el-button>
            </div>
          </template>
        </el-table-column>

        <template #empty>
          <el-empty description="暂无题目" :image-size="120">
            <el-button type="primary" @click="handleCreate">创建第一道题目</el-button>
          </el-empty>
        </template>
      </el-table>

      <!-- 分页 -->
      <div v-if="questionStore.questions.length > 0" class="pagination-wrapper">
        <div class="pagination-summary">
          已加载 {{ questionStore.questions.length }} / {{ questionStore.pagination.totalCount }} 题
        </div>
        <el-button
          v-if="questionStore.pagination.hasMore"
          :loading="loading"
          @click="fetchQuestions({ append: true })"
        >
          加载更多
        </el-button>
      </div>
    </el-card>

    <!-- 创建/编辑题目表单 -->
    <QuestionForm
      :visible="formDialogVisible"
      :mode="formMode"
      :question="currentQuestion"
      :question-bank-id="currentBankId"
      @close="handleFormClose"
      @success="handleFormSuccess"
    />
  </div>
</template>

<style scoped>
.questions-view {
  @apply flex flex-col gap-6;
}

/* 页面头部 */
.page-header {
  @apply flex items-start justify-between gap-4;
}

.header-content {
  @apply flex-1;
}

.header-title {
  @apply flex items-center gap-4;
}

.back-button {
  @apply flex-shrink-0;
}

.page-title {
  @apply text-[1.5rem] font-bold m-0 mb-1;
  color: var(--color-text-primary);
  font-family: 'Noto Serif SC', 'Songti SC', serif;
}

.dark .page-title {
  color: var(--color-text-primary);
}

.page-subtitle {
  @apply text-sm m-0;
  color: var(--color-text-secondary);
}

.dark .page-subtitle {
  color: var(--color-text-secondary);
}

/* 筛选栏 */
.filter-bar {
  @apply flex gap-3 items-center flex-wrap;
}

.search-input {
  @apply flex-1 min-w-[200px] max-w-[400px];
}

.filter-select {
  @apply w-[150px];
}

/* 表格卡片 */
.table-card {
  @apply overflow-visible;
}

.table-card :deep(.el-card__body) {
  @apply p-0;
}

/* 题目内容 */
.question-content {
  @apply flex flex-col gap-2;
}

.question-text {
  @apply font-medium overflow-hidden text-ellipsis
         line-clamp-2 leading-[1.5];
  color: var(--color-text-primary);
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
}

.dark .question-text {
  color: var(--color-text-primary);
}

.question-meta {
  @apply flex gap-2 flex-wrap;
}

/* 答案单元格 */
.answer-cell {
  @apply flex flex-col gap-2 text-sm;
}

.options-preview {
  @apply flex flex-col gap-1;
}

.option-line {
  @apply overflow-hidden text-ellipsis whitespace-nowrap;
  color: var(--color-text-secondary);
}

.dark .option-line {
  color: var(--color-text-secondary);
}

.more-hint {
  @apply text-xs;
  color: var(--color-text-muted);
}

.answer-preview {
  color: var(--color-text-secondary);
}

.dark .answer-preview {
  color: var(--color-text-secondary);
}

.correct-answer {
  @apply font-medium;
  color: var(--color-success);
}

/* 标签单元格 */
.tags-cell {
  @apply flex flex-wrap items-center;
}

.more-tags {
  @apply text-xs;
  color: var(--color-text-muted);
}

.text-muted {
  @apply text-sm;
  color: var(--color-text-muted);
}

.table-actions {
  @apply flex gap-2;
}

.table-actions .el-button {
  @apply flex-shrink-0;
}

/* 分页 */
.pagination-wrapper {
  @apply flex justify-center px-4 py-4 border-t;
  border-top-color: var(--color-border);
}

.dark .pagination-wrapper {
  border-top-color: var(--color-border);
}

/* 表格样式优化 */
:deep(.el-table) {
  @apply text-sm;
}

:deep(.el-table th.el-table__cell) {
  background: var(--color-bg-tertiary);
  color: var(--color-text-primary);
  font-weight: 600;
  padding-left: 1.5rem;
  padding-top: 1rem;
  padding-bottom: 1rem;
}

:deep(.el-table td.el-table__cell) {
  padding-top: 0.875rem;
  padding-bottom: 0.875rem;
}

:deep(.el-table td.el-table__cell:first-child) {
  padding-left: 1.5rem;
}

.dark :deep(.el-table th.el-table__cell) {
  background: var(--color-bg-tertiary);
  color: var(--color-text-primary);
}

:deep(.el-table tr:hover > td) {
  background: var(--color-hover-light) !important;
}

.dark :deep(.el-table tr:hover > td) {
  background: var(--color-hover-light) !important;
}

:deep(.el-table td.el-table__cell) {
  border-color: var(--color-border);
}

.dark :deep(.el-table td.el-table__cell) {
  border-color: var(--color-border);
}

/* 响应式 */
@media (max-width: 1024px) {
  .table-actions {
    @apply flex-col gap-1;
  }

  :deep(.el-table-column) {
    @apply py-2;
  }
}

@media (max-width: 768px) {
  .page-header {
    @apply flex-col items-stretch;
  }

  .filter-bar {
    @apply flex-col items-stretch;
  }

  .search-input,
  .filter-select {
    @apply max-w-none w-full;
  }

  :deep(.el-table) {
    @apply text-xs;
  }

  .table-actions .el-button {
    @apply px-2 py-1 text-xs;
  }
}
</style>
