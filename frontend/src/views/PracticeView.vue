<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Search, Refresh, Reading } from '@element-plus/icons-vue'
import { useQuestionBankStore } from '@/stores/questionBank'
import { useAuthStore } from '@/stores/auth'
import type { QuestionBank } from '@/stores/questionBank'
import { DifficultyLabels, DifficultyColors } from '@/types/question'

const router = useRouter()
const questionBankStore = useQuestionBankStore()
const authStore = useAuthStore()

const loading = ref(false)
const searchKeyword = ref('')
const searchTimer = ref<number | null>(null)

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

watch(searchKeyword, () => {
  if (searchTimer.value) {
    clearTimeout(searchTimer.value)
  }
  searchTimer.value = window.setTimeout(() => {
    fetchQuestionBanks(true)
  }, 300)
})

const handleStartPractice = async (bank: QuestionBank) => {
  // 1. 验证题库 ID 有效性
  if (!bank.id || bank.id <= 0) {
    ElMessage.error('题库信息无效')
    return
  }

  // 2. 验证用户权限
  const currentUserId = authStore.userInfo?.id
  if (bank.userId && bank.userId !== currentUserId) {
    ElMessage.error('您没有权限访问此题库')
    return
  }

  // 3. 检查题目数量
  if (!bank.questionCount || bank.questionCount <= 0) {
    ElMessage.warning('题库暂无题目，无法开始练习')
    return
  }

  // 4. 实时验证题库是否有题目（防止缓存数据不准确）
  try {
    loading.value = true
    // 重新获取题库列表以获取最新数据
    await questionBankStore.fetchQuestionBanks({
      search: searchKeyword.value || undefined,
      pageSize: 100
    })

    // 查找更新后的题库
    const updatedBank = questionBankStore.questionBanks.find(b => b.id === bank.id)
    if (!updatedBank) {
      ElMessage.error('题库不存在或已被删除')
      return
    }

    if (!updatedBank.questionCount || updatedBank.questionCount <= 0) {
      ElMessage.warning('题库暂无题目，无法开始练习')
      return
    }

    // 5. 再次验证用户权限（使用最新数据）
    if (updatedBank.userId && updatedBank.userId !== currentUserId) {
      ElMessage.error('您没有权限访问此题库')
      return
    }

    router.push(`/quiz/${bank.id}/new`)
  } catch (error: any) {
    const errorMessage = error?.response?.data?.message || error?.message || '验证题库失败，请重试'
    ElMessage.error(errorMessage)
  } finally {
    loading.value = false
  }
}

const handleCreateBank = () => {
  router.push('/question-banks?action=create')
}

const handleGenerateQuestions = () => {
  router.push('/generate')
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

// 使用共享的难度颜色
</script>

<template>
  <div class="practice-view">
    <div class="page-header">
      <div class="header-content">
        <h2 class="page-title">开始练习</h2>
        <p class="page-subtitle">选择一个题库开始答题</p>
      </div>
    </div>

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

    <div class="table-card">
      <el-table
        v-loading="loading"
        :data="questionBankStore.questionBanks"
        style="width: 100%"
        highlight-current-row
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
              :type="DifficultyColors[row.difficulty as keyof typeof DifficultyColors] || 'info'"
              size="small"
            >
              {{ DifficultyLabels[row.difficulty as keyof typeof DifficultyLabels] || '未知' }}
            </el-tag>
            <span v-else class="text-muted">-</span>
          </template>
        </el-table-column>

        <el-table-column prop="createdAt" label="创建时间" width="180">
          <template #default="{ row }">
            <span class="text-muted">{{ formatDate(row.createdAt) }}</span>
          </template>
        </el-table-column>

        <el-table-column label="操作" width="200" fixed="right">
          <template #default="{ row }">
            <el-tooltip
              v-if="!row.questionCount"
              content="题库暂无题目，无法开始练习"
              placement="top"
            >
              <span>
                <el-button type="primary" :icon="Reading" size="small" disabled>
                  开始练习
                </el-button>
              </span>
            </el-tooltip>
            <el-button
              v-else
              type="primary"
              :icon="Reading"
              size="small"
              @click="handleStartPractice(row)"
            >
              开始练习
            </el-button>
          </template>
        </el-table-column>

        <template #empty>
          <el-empty
            description="暂无题库"
            :image-size="120"
          >
            <el-button type="primary" @click="handleCreateBank">创建题库</el-button>
            <el-button @click="handleGenerateQuestions">生成题目</el-button>
          </el-empty>
        </template>
      </el-table>

      <div v-if="questionBankStore.hasMore" class="pagination-wrapper">
        <el-button
          :loading="loading"
          @click="fetchQuestionBanks()"
        >
          加载更多
        </el-button>
      </div>
    </div>
  </div>
</template>

<style scoped>
.practice-view {
  @apply flex flex-col gap-6;
}

.page-header {
  @apply flex items-start justify-between gap-4;
}

.header-content {
  @apply flex-1;
}

.page-title {
  @apply text-[1.5rem] font-bold m-0 mb-1;
  color: var(--color-text-primary);
}

.page-subtitle {
  @apply text-sm m-0;
  color: var(--color-text-secondary);
}

.filter-bar {
  @apply flex gap-3 items-center;
}

.search-input {
  @apply max-w-[400px];
}

.table-card {
  @apply overflow-hidden;
  border: 1px solid var(--color-border);
  border-radius: var(--radius-lg);
  background: var(--color-white);
}

.bank-name-cell {
  @apply flex items-center gap-2;
}

.bank-name {
  @apply font-medium overflow-hidden text-ellipsis whitespace-nowrap;
  color: var(--color-text-primary);
}

.bank-description {
  @apply text-sm overflow-hidden text-ellipsis whitespace-nowrap block;
  color: var(--color-text-secondary);
}

.text-muted {
  @apply text-sm;
  color: var(--color-text-muted);
}

.pagination-wrapper {
  @apply flex justify-center px-4 py-4 border-t;
  border-top-color: var(--color-border);
}

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
  border-color: var(--color-border);
}

:deep(.el-table td.el-table__cell:first-child) {
  padding-left: 1.5rem;
}

:deep(.el-table tr:hover > td) {
  background: var(--table-row-hover-bg) !important;
}

@media (max-width: 1024px) {
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
}
</style>
