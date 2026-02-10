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
const getQuestionTypeTagType = (type: any): string => {
  const typeMap: Record<string, string> = {
    'choice': 'success',
    'multiple-choice': 'warning',
    'true-false': 'info',
    'short-answer': '',
    'SingleChoice': 'success',
    'MultipleChoice': 'warning',
    'TrueFalse': 'info',
    'FillBlank': '',
    'ShortAnswer': ''
  }
  return typeMap[type] || ''
}

// 题目难度标签颜色
const getDifficultyTagType = (difficulty: any): string => {
  const typeMap: Record<string, string> = {
    'easy': 'success',
    'medium': 'warning',
    'hard': 'danger'
  }
  return typeMap[difficulty] || ''
}

// 获取题目显示文本（兼容新旧格式）
const getQuestionDisplayText = (question: any) => {
  return question.questionText || question.content || ''
}

// 获取题型显示文本
const getQuestionTypeDisplay = (question: any) => {
  const type = question.questionType || question.type
  const typeMap: Record<string, string> = {
    'choice': '单选',
    'multiple-choice': '多选',
    'true-false': '判断',
    'short-answer': '问答',
    'SingleChoice': '单选',
    'MultipleChoice': '多选',
    'TrueFalse': '判断',
    'FillBlank': '填空',
    'ShortAnswer': '简答'
  }
  return typeMap[type] || '未知'
}

// 获取选项列表（兼容新旧格式）
const getQuestionOptions = (question: any): string[] => {
  if (question.data?.options) {
    return question.data.options
  }
  return question.options || []
}

// 获取题目索引（用于选项字母）
const getOptionIndex = (index: number) => {
  if (typeof index === 'number') {
    return String.fromCharCode(65 + index)
  }
  return index
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
          v-for="(question, index) in questions"
          :key="question.id"
          class="question-item"
        >
          <div class="question-header">
            <span class="question-number">#{{ index + 1 }}</span>
            <el-tag size="small" :type="getQuestionTypeTagType((question as any).questionType || (question as any).type)">
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
            <span>{{ question.correctAnswer }}</span>
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
  min-height: calc(100vh - 140px);
}

.page-header {
  display: flex;
  align-items: center;
  gap: 1rem;
  margin-bottom: 1.5rem;
}

.page-header h2 {
  margin: 0;
  color: #073642;
  font-size: 1.5rem;
  font-weight: 700;
}

.bank-info {
  margin-bottom: 1.5rem;
}

.info-header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  gap: 1.5rem;
}

.description {
  color: #657B83;
  font-size: 0.9375rem;
  margin-bottom: 0.75rem;
  line-height: 1.6;
}

.meta {
  display: flex;
  gap: 1.5rem;
  color: #586E75;
  font-size: 0.8125rem;
  flex-wrap: wrap;
}

.meta span {
  display: flex;
  align-items: center;
  gap: 0.375rem;
}

.info-actions {
  display: flex;
  gap: 0.75rem;
}

.action-bar {
  display: flex;
  gap: 0.75rem;
  margin-bottom: 1.5rem;
  padding: 1rem;
  background: #EEE8D5;
  border-radius: 8px;
}

.questions-section {
  min-height: 200px;
}

.questions-list {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(600px, 1fr));
  gap: 1rem;
}

.question-item {
  padding: 1rem;
  border: 1px solid #E8E4CE;
  border-radius: 8px;
  background: #FFFFFF;
  transition: all 0.15s;
}

.question-item:hover {
  border-color: #268BD2;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.06);
}

.question-header {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  margin-bottom: 0.75rem;
}

.question-number {
  font-weight: 600;
  color: #268BD2;
  font-size: 0.875rem;
}

.question-content {
  font-size: 0.9375rem;
  color: #073642;
  margin-bottom: 0.75rem;
  line-height: 1.6;
}

.question-options {
  margin-bottom: 0.75rem;
  padding-left: 1.25rem;
}

.options-label {
  color: #586E75;
  font-size: 0.8125rem;
  margin-bottom: 0.5rem;
  font-weight: 500;
}

.question-options ul {
  margin: 0;
  padding-left: 0;
  list-style-type: none;
}

.question-options li {
  padding: 0.25rem 0;
  color: #657B83;
  font-size: 0.875rem;
}

.question-answer {
  margin-bottom: 0.5rem;
  padding: 0.5rem 0.75rem;
  background: #DBF0E8;
  border-radius: 6px;
  border: 1px solid #2AA198;
}

.answer-label {
  font-weight: 500;
  color: #2AA198;
  font-size: 0.8125rem;
}

.question-explanation {
  margin-bottom: 0.75rem;
  padding: 0.5rem 0.75rem;
  background: #FDF6E3;
  border-radius: 6px;
  border: 1px solid #EEE8D5;
}

.explanation-label {
  font-weight: 500;
  color: #B58900;
  font-size: 0.8125rem;
}

.question-tags {
  display: flex;
  gap: 0.5rem;
  flex-wrap: wrap;
}

/* 响应式 */
@media (max-width: 768px) {
  .info-header {
    flex-direction: column;
  }

  .questions-list {
    grid-template-columns: 1fr;
  }

  .action-bar {
    flex-wrap: wrap;
  }
}
</style>
