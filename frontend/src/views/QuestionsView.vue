<script setup lang="ts">
import { ref, onMounted, watch, computed } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, View, Edit, Delete, Search, Refresh, ArrowLeft } from '@element-plus/icons-vue'
import { useQuestionStore } from '@/stores/question'
import { useQuestionBankStore } from '@/stores/questionBank'
import QuestionForm from '@/components/QuestionForm.vue'
import { QuestionType, Difficulty, type Question } from '@/stores/question'

const route = useRoute()
const questionStore = useQuestionStore()
const questionBankStore = useQuestionBankStore()

const loading = ref(false)
const searchKeyword = ref('')
const selectedType = ref<string>('')
const selectedDifficulty = ref<string>('')
const searchTimer = ref<number | null>(null)

// 表单弹窗状态
const formDialogVisible = ref(false)
const formMode = ref<'create' | 'edit'>('create')
const currentQuestion = ref<Question | null>(null)

// 当前题库ID
const currentBankId = computed(() => route.params.bankId as string || route.query.bankId as string || '')

onMounted(async () => {
  if (currentBankId.value) {
    // 先加载题库信息
    await questionBankStore.fetchQuestionBank(currentBankId.value)
  }
  await fetchQuestions()
})

const fetchQuestions = async (page = 1) => {
  loading.value = true
  try {
    await questionStore.fetchQuestions({
      page,
      pageSize: 20,
      questionBankId: currentBankId.value,
      type: selectedType.value || undefined,
      difficulty: selectedDifficulty.value || undefined,
      search: searchKeyword.value || undefined
    })
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
    fetchQuestions(1)
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
    await ElMessageBox.confirm(
      `确定要删除题目"${question.content.substring(0, 50)}..."吗？删除后无法恢复。`,
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

const handlePageChange = (page: number) => {
  fetchQuestions(page)
}

const handleBackToBank = () => {
  if (currentBankId.value) {
    window.history.back()
  }
}

const getQuestionTypeLabel = (type: string) => {
  const labels: Record<string, string> = {
    [QuestionType.CHOICE]: '单选',
    [QuestionType.MULTIPLE_CHOICE]: '多选',
    [QuestionType.TRUE_FALSE]: '判断',
    [QuestionType.SHORT_ANSWER]: '填空'
  }
  return labels[type] || '未知'
}

const getQuestionTypeColor = (type: string) => {
  const colors: Record<string, string> = {
    [QuestionType.CHOICE]: 'primary',
    [QuestionType.MULTIPLE_CHOICE]: 'success',
    [QuestionType.TRUE_FALSE]: 'warning',
    [QuestionType.SHORT_ANSWER]: 'info'
  }
  return colors[type] || 'info'
}

const getDifficultyColor = (difficulty: string) => {
  switch (difficulty) {
    case Difficulty.EASY: return 'success'
    case Difficulty.MEDIUM: return 'warning'
    case Difficulty.HARD: return 'danger'
    default: return 'info'
  }
}

const getDifficultyLabel = (difficulty: string) => {
  switch (difficulty) {
    case Difficulty.EASY: return '简单'
    case Difficulty.MEDIUM: return '中等'
    case Difficulty.HARD: return '困难'
    default: return '未知'
  }
}

const formatCorrectAnswer = (question: Question) => {
  if (question.type === QuestionType.TRUE_FALSE) {
    return question.correctAnswer === 'true' ? '正确' : '错误'
  }
  if (question.type === QuestionType.MULTIPLE_CHOICE) {
    return question.correctAnswer.split('').join(', ')
  }
  return question.correctAnswer
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
            @click="handleBackToBank"
            class="back-button"
          />
          <div>
            <h2 class="page-title">
              {{ currentBankId ? `${questionBankStore.currentBank?.name || '题库'} - 题目管理` : '题目管理' }}
            </h2>
            <p class="page-subtitle">管理题目,创建和编辑题目内容</p>
          </div>
        </div>
      </div>
      <el-button type="primary" :icon="Plus" @click="handleCreate">
        创建题目
      </el-button>
    </div>

    <!-- 搜索和筛选 -->
    <div class="filter-bar">
      <el-input
        v-model="searchKeyword"
        placeholder="搜索题目内容..."
        :prefix-icon="Search"
        clearable
        class="search-input"
        @clear="fetchQuestions(1)"
      />
      <el-select
        v-model="selectedType"
        placeholder="题型筛选"
        clearable
        class="filter-select"
      >
        <el-option label="单选题" :value="QuestionType.CHOICE" />
        <el-option label="多选题" :value="QuestionType.MULTIPLE_CHOICE" />
        <el-option label="判断题" :value="QuestionType.TRUE_FALSE" />
        <el-option label="填空题" :value="QuestionType.SHORT_ANSWER" />
      </el-select>
      <el-select
        v-model="selectedDifficulty"
        placeholder="难度筛选"
        clearable
        class="filter-select"
      >
        <el-option label="简单" :value="Difficulty.EASY" />
        <el-option label="中等" :value="Difficulty.MEDIUM" />
        <el-option label="困难" :value="Difficulty.HARD" />
      </el-select>
      <el-button :icon="Refresh" @click="fetchQuestions(1)">刷新</el-button>
    </div>

    <!-- 题目表格 -->
    <el-card class="table-card" shadow="never">
      <el-table
        v-loading="loading"
        :data="questionStore.questions"
        style="width: 100%"
        stripe
      >
        <el-table-column prop="content" label="题目内容" min-width="300">
          <template #default="{ row }">
            <div class="question-content">
              <span class="question-text" :title="row.content">{{ row.content }}</span>
              <div class="question-meta">
                <el-tag
                  :type="getQuestionTypeColor(row.type)"
                  size="small"
                >
                  {{ getQuestionTypeLabel(row.type) }}
                </el-tag>
                <el-tag
                  v-if="row.difficulty"
                  :type="getDifficultyColor(row.difficulty)"
                  size="small"
                >
                  {{ getDifficultyLabel(row.difficulty) }}
                </el-tag>
              </div>
            </div>
          </template>
        </el-table-column>

        <el-table-column label="选项/答案" min-width="250">
          <template #default="{ row }">
            <div class="answer-cell">
              <div v-if="row.type === QuestionType.CHOICE || row.type === QuestionType.MULTIPLE_CHOICE" class="options-preview">
                <div v-for="(opt, idx) in (row.options?.slice(0, 2) || [])" :key="idx" class="option-line">
                  {{ String.fromCharCode(65 + idx) }}. {{ opt }}
                </div>
                <span v-if="row.options && row.options.length > 2" class="more-hint">
                  +{{ row.options.length - 2 }} 个选项
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
              <span v-if="row.tags.length > 3" class="more-tags">
                +{{ row.tags.length - 3 }}
              </span>
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
              <el-button size="small" :icon="Edit" @click="handleEdit(row)">
                编辑
              </el-button>
              <el-button type="danger" size="small" :icon="Delete" @click="handleDelete(row)">
                删除
              </el-button>
            </div>
          </template>
        </el-table-column>

        <template #empty>
          <el-empty
            description="暂无题目"
            :image-size="120"
          >
            <el-button type="primary" @click="handleCreate">创建第一道题目</el-button>
          </el-empty>
        </template>
      </el-table>

      <!-- 分页 -->
      <div v-if="questionStore.pagination.total > 0" class="pagination-wrapper">
        <el-pagination
          :current-page="questionStore.pagination.page"
          :page-size="questionStore.pagination.pageSize"
          :total="questionStore.pagination.total"
          layout="total, prev, pager, next, jumper"
          @current-change="handlePageChange"
        />
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
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

/* 页面头部 */
.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.header-content {
  flex: 1;
}

.header-title {
  display: flex;
  align-items: center;
  gap: 1rem;
}

.back-button {
  flex-shrink: 0;
}

.page-title {
  font-size: 1.5rem;
  font-weight: 700;
  color: #073642;
  margin: 0 0 0.25rem 0;
}

.dark .page-title {
  color: #839496;
}

.page-subtitle {
  font-size: 0.875rem;
  color: #586E75;
  margin: 0;
}

.dark .page-subtitle {
  color: #93A1A1;
}

/* 筛选栏 */
.filter-bar {
  display: flex;
  gap: 0.75rem;
  align-items: center;
  flex-wrap: wrap;
}

.search-input {
  flex: 1;
  min-width: 200px;
  max-width: 400px;
}

.filter-select {
  width: 150px;
}

/* 表格卡片 */
.table-card {
  overflow: visible;
}

.table-card :deep(.el-card__body) {
  padding: 0;
}

/* 题目内容 */
.question-content {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
}

.question-text {
  font-weight: 500;
  color: #073642;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  line-height: 1.5;
}

.dark .question-text {
  color: #839496;
}

.question-meta {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

/* 答案单元格 */
.answer-cell {
  display: flex;
  flex-direction: column;
  gap: 0.5rem;
  font-size: 0.875rem;
}

.options-preview {
  display: flex;
  flex-direction: column;
  gap: 0.25rem;
}

.option-line {
  color: #586E75;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dark .option-line {
  color: #93A1A1;
}

.more-hint {
  color: #9ca3af;
  font-size: 0.75rem;
}

.answer-preview {
  color: #586E75;
}

.dark .answer-preview {
  color: #93A1A1;
}

.correct-answer {
  color: #10b981;
  font-weight: 500;
}

/* 标签单元格 */
.tags-cell {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
}

.more-tags {
  color: #9ca3af;
  font-size: 0.75rem;
}

.text-muted {
  font-size: 0.875rem;
  color: #9ca3af;
}

.table-actions {
  display: flex;
  gap: 0.5rem;
}

.table-actions .el-button {
  flex-shrink: 0;
}

/* 分页 */
.pagination-wrapper {
  display: flex;
  justify-content: center;
  padding: 1rem;
  border-top: 1px solid #e5e7eb;
}

.dark .pagination-wrapper {
  border-top-color: #374151;
}

/* 表格样式优化 */
:deep(.el-table) {
  font-size: 0.875rem;
}

:deep(.el-table th.el-table__cell) {
  background: #EEE8D5;
  color: #073642;
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
  background: #073642;
  color: #839496;
}

:deep(.el-table tr:hover > td) {
  background: #FDF6E3 !important;
}

.dark :deep(.el-table tr:hover > td) {
  background: #073642 !important;
}

:deep(.el-table td.el-table__cell) {
  border-color: #E8E4CE;
}

.dark :deep(.el-table td.el-table__cell) {
  border-color: #586E75;
}

/* 响应式 */
@media (max-width: 1024px) {
  .table-actions {
    flex-direction: column;
    gap: 0.25rem;
  }

  :deep(.el-table-column) {
    padding: 0.5rem 0;
  }
}

@media (max-width: 768px) {
  .page-header {
    flex-direction: column;
    align-items: stretch;
  }

  .filter-bar {
    flex-direction: column;
    align-items: stretch;
  }

  .search-input,
  .filter-select {
    max-width: none;
    width: 100%;
  }

  :deep(.el-table) {
    font-size: 0.75rem;
  }

  .table-actions .el-button {
    padding: 0.25rem 0.5rem;
    font-size: 0.75rem;
  }
}
</style>
