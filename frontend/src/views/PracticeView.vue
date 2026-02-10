<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { Search, Refresh, Reading } from '@element-plus/icons-vue'
import { useQuestionBankStore } from '@/stores/questionBank'
import type { QuestionBank } from '@/stores/questionBank'
import { DifficultyLabels, DifficultyColors } from '@/types/question'

const router = useRouter()
const questionBankStore = useQuestionBankStore()

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
  // 检查题目数量，如果是0则提示
  if (!bank.questionCount) {
    ElMessage.warning('题库暂无题目，无法开始练习')
    return
  }

  // 实时验证题库是否有题目（防止缓存数据不准确）
  try {
    loading.value = true
    // 重新获取题库列表以获取最新数据
    await questionBankStore.fetchQuestionBanks({
      search: searchKeyword.value || undefined,
      pageSize: 100
    })

    // 查找更新后的题库
    const updatedBank = questionBankStore.questionBanks.find(b => b.id === bank.id)
    if (!updatedBank || !updatedBank.questionCount) {
      ElMessage.warning('题库暂无题目，无法开始练习')
      return
    }

    router.push(`/quiz/${bank.id}/new`)
  } catch {
    ElMessage.error('验证题库失败，请重试')
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
    </el-card>
  </div>
</template>

<style scoped>
.practice-view {
  display: flex;
  flex-direction: column;
  gap: 1.5rem;
}

.page-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  gap: 1rem;
}

.header-content {
  flex: 1;
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

.filter-bar {
  display: flex;
  gap: 0.75rem;
  align-items: center;
}

.search-input {
  max-width: 400px;
}

.table-card {
  overflow: visible;
}

.table-card :deep(.el-card__body) {
  padding: 0;
}

.bank-name-cell {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.bank-name {
  font-weight: 500;
  color: #073642;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.dark .bank-name {
  color: #839496;
}

.bank-description {
  font-size: 0.875rem;
  color: #586E75;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  display: block;
}

.dark .bank-description {
  color: #93A1A1;
}

.text-muted {
  font-size: 0.875rem;
  color: #9ca3af;
}

.pagination-wrapper {
  display: flex;
  justify-content: center;
  padding: 1rem;
  border-top: 1px solid #e5e7eb;
}

.dark .pagination-wrapper {
  border-top-color: #374151;
}

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

@media (max-width: 1024px) {
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

  .search-input {
    max-width: none;
  }

  :deep(.el-table) {
    font-size: 0.75rem;
  }
}
</style>
