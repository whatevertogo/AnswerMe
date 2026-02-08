<script setup lang="ts">
import { ref, onMounted } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, View, Edit, Delete } from '@element-plus/icons-vue'
import { useQuestionBankStore } from '@/stores/questionBank'

const router = useRouter()
const questionBankStore = useQuestionBankStore()

const loading = ref(false)

onMounted(async () => {
  await fetchQuestionBanks()
})

const fetchQuestionBanks = async () => {
  loading.value = true
  try {
    await questionBankStore.fetchQuestionBanks()
  } catch {
    ElMessage.error('获取题库列表失败')
  } finally {
    loading.value = false
  }
}

const handleCreate = () => {
  // TODO: 打开创建题库对话框
  ElMessage.info('创建题库功能开发中...')
}

const handleView = (bankId: string) => {
  router.push(`/question-banks/${bankId}`)
}

const handleEdit = () => {
  // TODO: 打开编辑题库对话框
  ElMessage.info('编辑题库功能开发中...')
}

const handleDelete = async () => {
  try {
    await ElMessageBox.confirm('确定要删除这个题库吗？', '提示', {
      confirmButtonText: '确定',
      cancelButtonText: '取消',
      type: 'warning'
    })
    // TODO: 调用删除API
    ElMessage.success('删除成功')
    await fetchQuestionBanks()
  } catch {
    // 用户取消
  }
}
</script>

<template>
  <div class="question-banks-view">
    <div class="header-actions">
      <h2>我的题库</h2>
      <el-button type="primary" :icon="Plus" @click="handleCreate">创建题库</el-button>
    </div>

    <el-divider />

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
              <h3 class="bank-name">{{ bank.name }}</h3>
              <el-tag size="small">{{ bank.questionCount }} 题</el-tag>
            </div>

            <p class="bank-description">{{ bank.description }}</p>

            <div class="bank-meta">
              <span class="bank-date"
                >创建于 {{ new Date(bank.createdAt).toLocaleDateString() }}</span
              >
            </div>

            <div class="bank-actions">
              <el-button type="primary" size="small" :icon="View" @click="handleView(bank.id)">
                查看
              </el-button>
              <el-button size="small" :icon="Edit" @click="handleEdit">编辑</el-button>
              <el-button type="danger" size="small" :icon="Delete" @click="handleDelete">
                删除
              </el-button>
            </div>
          </el-card>
        </el-col>
      </el-row>
    </div>
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

.content {
  min-height: 200px;
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
