<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, View, Edit, Delete, Search, Refresh } from '@element-plus/icons-vue'
import { useQuestionBankStore } from '@/stores/questionBank'
import QuestionBankForm from '@/components/QuestionBankForm.vue'
import type { QuestionBank } from '@/stores/questionBank'

const router = useRouter()
const questionBankStore = useQuestionBankStore()

const loading = ref(false)
const searchKeyword = ref('')
const searchTimer = ref<number | null>(null)

// 表单弹窗状态
const formDialogVisible = ref(false)
const formMode = ref<'create' | 'edit'>('create')
const currentBank = ref<QuestionBank | null>(null)

onMounted(async () => {
  await fetchQuestionBanks()
})

const fetchQuestionBanks = async (reset = false) => {
  if (reset) {
    questionBankStore.questionBanks = []
  }
  loading.value = true
  try {
    await questionBankStore.fetchQuestionBanks({
      search: searchKeyword.value || undefined,
      pageSize: 100
    })
  } catch {
    ElMessage.error('获取题库列表失败')
  } finally {
    loading.value = false
  }
}

// 搜索防抖
watch(searchKeyword, () => {
  if (searchTimer.value) {
    clearTimeout(searchTimer.value)
  }
  searchTimer.value = window.setTimeout(() => {
    fetchQuestionBanks(true)
  }, 300)
})

const handleCreate = () => {
  formMode.value = 'create'
  currentBank.value = null
  formDialogVisible.value = true
}

const handleView = (bankId: string) => {
  router.push(`/question-banks/${bankId}`)
}

const handleEdit = (bank: QuestionBank) => {
  formMode.value = 'edit'
  currentBank.value = bank
  formDialogVisible.value = true
}

const handleDelete = async (bank: QuestionBank) => {
  try {
    await ElMessageBox.confirm(
      `确定要删除题库"${bank.name}"吗？删除后无法恢复。`,
      '删除确认',
      {
        confirmButtonText: '确定删除',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )
    await questionBankStore.deleteQuestionBank(bank.id)
    ElMessage.success('删除成功')
    await fetchQuestionBanks(true)
  } catch (error) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handleFormSuccess = (_bank: QuestionBank) => {
  formDialogVisible.value = false
  if (formMode.value === 'create') {
    ElMessage.success('创建题库成功')
  }
  fetchQuestionBanks(true)
}

const handleFormClose = () => {
  formDialogVisible.value = false
  currentBank.value = null
}

const formatDate = (dateString: string) => {
  return new Date(dateString).toLocaleString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit',
    hour: '2-digit',
    minute: '2-digit'
  })
}

const getDifficultyColor = (difficulty: string) => {
  switch (difficulty) {
    case 'easy': return 'success'
    case 'medium': return 'warning'
    case 'hard': return 'danger'
    default: return 'info'
  }
}

const getDifficultyLabel = (difficulty: string) => {
  switch (difficulty) {
    case 'easy': return '简单'
    case 'medium': return '中等'
    case 'hard': return '困难'
    default: return '未知'
  }
}
</script>

<template>
  <div class="question-banks-view">
    <!-- 页面标题和操作 -->
    <div class="page-header">
      <div class="header-content">
        <h2 class="page-title">题库管理</h2>
        <p class="page-subtitle">管理你的题库,创建和编辑题目</p>
      </div>
      <el-button type="primary" :icon="Plus" @click="handleCreate">
        创建题库
      </el-button>
    </div>

    <!-- 搜索和筛选 -->
    <div class="filter-bar">
      <el-input
        v-model="searchKeyword"
        placeholder="搜索题库名称或描述..."
        :prefix-icon="Search"
        clearable
        class="search-input"
        @clear="fetchQuestionBanks(true)"
      />
      <el-button :icon="Refresh" @click="fetchQuestionBanks(true)">刷新</el-button>
    </div>

    <!-- 题库表格 -->
    <el-card class="table-card" shadow="never">
      <el-table
        v-loading="loading"
        :data="questionBankStore.questionBanks"
        style="width: 100%"
        stripe
      >
        <el-table-column prop="name" label="题库名称" min-width="200">
          <template #default="{ row }">
            <div class="bank-name-cell">
              <span class="bank-name" :title="row.name">{{ row.name }}</span>
              <el-tag v-if="row.isDefault" size="small" type="primary">默认</el-tag>
            </div>
          </template>
        </el-table-column>

        <el-table-column prop="description" label="描述" min-width="300">
          <template #default="{ row }">
            <span class="bank-description" :title="row.description">
              {{ row.description || '暂无描述' }}
            </span>
          </template>
        </el-table-column>

        <el-table-column prop="questionCount" label="题目数量" width="120" align="center">
          <template #default="{ row }">
            <el-tag size="small" type="info">{{ row.questionCount }} 题</el-tag>
          </template>
        </el-table-column>

        <el-table-column prop="difficulty" label="难度" width="100" align="center">
          <template #default="{ row }">
            <el-tag
              v-if="row.difficulty"
              :type="getDifficultyColor(row.difficulty)"
              size="small"
            >
              {{ getDifficultyLabel(row.difficulty) }}
            </el-tag>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>

        <el-table-column prop="createdAt" label="创建时间" width="180">
          <template #default="{ row }">
            <span class="text-muted">{{ formatDate(row.createdAt) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="操作" width="280" fixed="right">
          <template #default="{ row }">
            <div class="table-actions">
              <el-button type="primary" size="small" :icon="View" @click="handleView(row.id)">
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
            description="暂无题库"
            :image-size="120"
          >
            <el-button type="primary" @click="handleCreate">创建第一个题库</el-button>
          </el-empty>
        </template>
      </el-table>

      <!-- 分页 -->
      <div v-if="questionBankStore.hasMore" class="pagination-wrapper">
        <el-button
          :loading="loading"
          @click="fetchQuestionBanks()"
        >
          加载更多
        </el-button>
      </div>
    </el-card>

    <!-- 创建/编辑题库表单 -->
    <QuestionBankForm
      :visible="formDialogVisible"
      :mode="formMode"
      :bank="currentBank"
      @close="handleFormClose"
      @success="handleFormSuccess"
    />
  </div>
</template>

<style scoped>
.question-banks-view {
  @apply flex flex-col gap-6;
}

/* 页面头部 */
.page-header {
  @apply flex items-start justify-between gap-4;
}

.header-content {
  @apply flex-1;
}

.page-title {
  @apply text-[1.5rem] font-bold m-0 mb-1;
  color: #073642;
}

.dark .page-title {
  color: #839496;
}

.page-subtitle {
  @apply text-sm m-0;
  color: #586E75;
}

.dark .page-subtitle {
  color: #93A1A1;
}

/* 筛选栏 */
.filter-bar {
  @apply flex gap-3 items-center;
}

.search-input {
  @apply max-w-[400px];
}

/* 表格卡片 */
.table-card {
  @apply overflow-visible;
}

.table-card :deep(.el-card__body) {
  @apply p-0;
}

.bank-name-cell {
  @apply flex items-center gap-2;
}

.bank-name {
  @apply font-medium overflow-hidden text-ellipsis whitespace-nowrap;
  color: #073642;
}

.dark .bank-name {
  color: #839496;
}

.bank-description {
  @apply text-sm overflow-hidden text-ellipsis whitespace-nowrap block;
  color: #586E75;
}

.dark .bank-description {
  color: #93A1A1;
}

.text-muted {
  @apply text-sm;
  color: #9ca3af;
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
  border-color: #e5e7eb;
}

.dark .pagination-wrapper {
  border-color: #374151;
}

/* 表格样式优化 */
:deep(.el-table) {
  @apply text-sm;
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

  .search-input {
    @apply max-w-none;
  }

  :deep(.el-table) {
    @apply text-xs;
  }

  .table-actions .el-button {
    @apply px-2 py-1 text-xs;
  }
}
</style>
