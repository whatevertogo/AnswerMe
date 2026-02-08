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
      pageSize: 20
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
    // 如果列表为空，重新获取
    if (questionBankStore.questionBanks.length === 0) {
      await fetchQuestionBanks(true)
    }
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
</script>

<template>
  <div class="question-banks-view">
    <div class="header-actions">
      <h2>我的题库</h2>
      <el-button type="primary" :icon="Plus" @click="handleCreate">创建题库</el-button>
    </div>

    <el-divider />

    <!-- 搜索和筛选 -->
    <div class="filter-bar">
      <el-input
        v-model="searchKeyword"
        placeholder="搜索题库名称..."
        :prefix-icon="Search"
        clearable
        class="search-input"
        @clear="fetchQuestionBanks(true)"
      />
      <el-button :icon="Refresh" @click="fetchQuestionBanks(true)">刷新</el-button>
    </div>

    <div v-loading="loading" class="content">
      <el-empty
        v-if="!loading && questionBankStore.questionBanks.length === 0"
        description="暂无题库"
      >
        <el-button type="primary" @click="handleCreate">创建第一个题库</el-button>
      </el-empty>

      <el-row v-else :gutter="20">
        <el-col
          v-for="bank in questionBankStore.questionBanks"
          :key="bank.id"
          :xs="24"
          :sm="12"
          :md="8"
          :lg="6"
        >
          <el-card class="bank-card" shadow="hover">
            <div class="bank-header">
              <h3 class="bank-name" :title="bank.name">{{ bank.name }}</h3>
              <el-tag size="small" type="info">{{ bank.questionCount }} 题</el-tag>
            </div>

            <p class="bank-description">{{ bank.description || '暂无描述' }}</p>

            <div class="bank-meta">
              <span class="bank-date"
                >创建于 {{ new Date(bank.createdAt).toLocaleDateString() }}</span
              >
            </div>

            <div class="bank-actions">
              <el-button type="primary" size="small" :icon="View" @click="handleView(bank.id)">
                查看
              </el-button>
              <el-button size="small" :icon="Edit" @click="handleEdit(bank)">编辑</el-button>
              <el-button type="danger" size="small" :icon="Delete" @click="handleDelete(bank)">
                删除
              </el-button>
            </div>
          </el-card>
        </el-col>
      </el-row>

      <!-- 加载更多 -->
      <div v-if="questionBankStore.hasMore" class="load-more">
        <el-button :loading="loading" @click="fetchQuestionBanks()">加载更多</el-button>
      </div>
    </div>

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
  background-color: #ffffff;
  border-radius: 8px;
  padding: 20px;
}

.header-actions {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-actions h2 {
  margin: 0;
  color: #303133;
}

.filter-bar {
  display: flex;
  gap: 12px;
  margin-bottom: 20px;
}

.search-input {
  max-width: 300px;
}

.content {
  min-height: 200px;
}

.load-more {
  text-align: center;
  margin-top: 20px;
}

.bank-card {
  margin-bottom: 20px;
  height: 100%;
  display: flex;
  flex-direction: column;
}

.bank-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 12px;
}

.bank-name {
  margin: 0;
  font-size: 18px;
  color: #303133;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  flex: 1;
  margin-right: 8px;
}

.bank-description {
  color: #606266;
  font-size: 14px;
  margin-bottom: 12px;
  flex-grow: 1;
  overflow: hidden;
  text-overflow: ellipsis;
  display: -webkit-box;
  -webkit-line-clamp: 2;
  -webkit-box-orient: vertical;
  min-height: 42px;
}

.bank-meta {
  margin-top: auto;
  padding-top: 12px;
  border-top: 1px solid #ebeef5;
}

.bank-date {
  color: #909399;
  font-size: 12px;
}

.bank-actions {
  display: flex;
  gap: 8px;
  margin-top: 12px;
}

.bank-actions .el-button {
  flex: 1;
}
</style>
