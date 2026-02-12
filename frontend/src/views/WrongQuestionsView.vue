<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Document, Clock, Refresh, Check } from '@element-plus/icons-vue'
import { useWrongQuestionStore } from '@/stores/wrongQuestion'
import { useQuestionBankStore } from '@/stores/questionBank'
import type { WrongQuestion } from '@/api/wrongQuestion'

const router = useRouter()
const wrongQuestionStore = useWrongQuestionStore()
const questionBankStore = useQuestionBankStore()

// 筛选状态
const selectedBankId = ref<number | undefined>()
const selectedQuestionType = ref<string | undefined>()
const dateRange = ref<[string, string] | null>(null)

// 题目类型选项
const questionTypeOptions = [
  { label: '全部题型', value: '' },
  { label: '单选题', value: 'SingleChoice' },
  { label: '多选题', value: 'MultipleChoice' },
  { label: '判断题', value: 'TrueFalse' },
  { label: '填空题', value: 'FillBlank' },
  { label: '简答题', value: 'ShortAnswer' }
]

const loading = ref(false)

// 按题库分组的错题
const groupedQuestions = computed(() => {
  return wrongQuestionStore.questionsByBank
})

// 获取题库选项
const bankOptions = computed(() => {
  return questionBankStore.questionBanks.map((bank: { id: number; name: string }) => ({
    label: bank.name,
    value: bank.id
  }))
})

onMounted(async () => {
  await loadData()
  await questionBankStore.fetchQuestionBanks()
})

async function loadData() {
  loading.value = true
  try {
    await wrongQuestionStore.fetchWrongQuestions()
  } catch (e) {
    ElMessage.error('加载错题失败')
  } finally {
    loading.value = false
  }
}

async function handleSearch() {
  loading.value = true
  try {
    wrongQuestionStore.setFilters({
      questionBankId: selectedBankId.value,
      questionType: selectedQuestionType.value || undefined,
      startDate: dateRange.value?.[0],
      endDate: dateRange.value?.[1]
    })
    await wrongQuestionStore.fetchWrongQuestions(wrongQuestionStore.filters)
  } catch (e) {
    ElMessage.error('筛选失败')
  } finally {
    loading.value = false
  }
}

function handleReset() {
  selectedBankId.value = undefined
  selectedQuestionType.value = undefined
  dateRange.value = null
  wrongQuestionStore.resetFilters()
  loadData()
}

function handleRetryQuestion(question: WrongQuestion) {
  // 跳转到练习模式，用错题的题库
  router.push(`/practice?bankId=${question.questionBankId}`)
}

async function handleMarkMastered(question: WrongQuestion) {
  try {
    await ElMessageBox.confirm('确定要将此题标记为已掌握吗？', '确认', { type: 'info' })
    await wrongQuestionStore.markAsMastered(question.id)
    ElMessage.success('标记成功')
  } catch {
    // 用户取消
  }
}

function formatQuestionType(type: string): string {
  const typeMap: Record<string, string> = {
    SingleChoice: '单选题',
    MultipleChoice: '多选题',
    TrueFalse: '判断题',
    FillBlank: '填空题',
    ShortAnswer: '简答题'
  }
  return typeMap[type] || type
}

function formatDate(dateStr: string): string {
  return new Date(dateStr).toLocaleDateString('zh-CN', {
    year: 'numeric',
    month: '2-digit',
    day: '2-digit'
  })
}

function formatAnswer(answer: string | null | undefined): string {
  if (!answer) return '-'

  const trimmed = answer.trim()
  if (!trimmed) return '-'

  if (
    trimmed.startsWith('[') ||
    trimmed.startsWith('{') ||
    trimmed.startsWith('"') ||
    trimmed === 'true' ||
    trimmed === 'false'
  ) {
    try {
      const parsed = JSON.parse(trimmed) as unknown

      if (Array.isArray(parsed)) {
        return parsed.map(item => String(item)).join('、')
      }

      if (typeof parsed === 'boolean') {
        return parsed ? '正确' : '错误'
      }

      if (parsed && typeof parsed === 'object') {
        return Object.values(parsed as Record<string, unknown>)
          .map(item => String(item))
          .join('、')
      }

      return String(parsed)
    } catch {
      return answer
    }
  }

  return answer
}
</script>

<template>
  <div class="wrong-questions-container">
    <!-- 顶部筛选栏 -->
    <div class="filter-bar">
      <div class="filter-left">
        <el-select v-model="selectedBankId" placeholder="全部题库" clearable class="filter-select">
          <el-option
            v-for="bank in bankOptions"
            :key="bank.value"
            :label="bank.label"
            :value="bank.value"
          />
        </el-select>

        <el-select
          v-model="selectedQuestionType"
          placeholder="全部题型"
          clearable
          class="filter-select"
        >
          <el-option
            v-for="type in questionTypeOptions"
            :key="type.value"
            :label="type.label"
            :value="type.value"
          />
        </el-select>

        <el-date-picker
          v-model="dateRange"
          type="daterange"
          range-separator="至"
          start-placeholder="开始日期"
          end-placeholder="结束日期"
          value-format="YYYY-MM-DD"
          class="date-picker"
        />
      </div>

      <div class="filter-right">
        <el-button @click="handleReset">重置</el-button>
        <el-button type="primary" @click="handleSearch">筛选</el-button>
      </div>
    </div>

    <!-- 统计信息 -->
    <div class="stats-bar">
      <div class="stat-item">
        <el-icon><Document /></el-icon>
        <span>共 {{ wrongQuestionStore.totalCount }} 道错题</span>
      </div>
      <div class="stat-item">
        <el-icon><Clock /></el-icon>
        <span>涉及 {{ wrongQuestionStore.bankGroupCount }} 个题库</span>
      </div>
    </div>

    <!-- 错题列表 -->
    <div v-loading="loading" class="questions-content">
      <template v-if="Object.keys(groupedQuestions).length > 0">
        <div v-for="(questions, bankId) in groupedQuestions" :key="bankId" class="bank-group">
          <div class="bank-header">
            <span class="bank-name">{{ questions[0]?.questionBankName }}</span>
            <span class="bank-count">{{ questions.length }} 道错题</span>
          </div>

          <div class="questions-list">
            <div v-for="question in questions" :key="question.id" class="question-card">
              <div class="question-header">
                <el-tag size="small" type="warning">
                  {{ formatQuestionType(question.questionType) }}
                </el-tag>
                <span class="question-date">{{ formatDate(question.answeredAt) }}</span>
              </div>

              <div class="question-content">
                {{ question.questionText }}
              </div>

              <div v-if="question.options" class="question-options">
                <div class="option-item">
                  <span class="option-label">选项：</span>
                  {{ question.options }}
                </div>
              </div>

              <div class="answer-section">
                <div class="answer-item user-answer">
                  <span class="answer-label">你的答案：</span>
                  <span class="answer-value wrong">{{ formatAnswer(question.userAnswer) }}</span>
                </div>
                <div class="answer-item correct-answer">
                  <span class="answer-label">正确答案：</span>
                  <span class="answer-value correct">{{
                    formatAnswer(question.correctAnswer)
                  }}</span>
                </div>
              </div>

              <div v-if="question.explanation" class="explanation">
                <span class="explanation-label">解析：</span>
                {{ question.explanation }}
              </div>

              <div class="question-actions">
                <el-button
                  type="primary"
                  size="small"
                  :icon="Refresh"
                  @click="handleRetryQuestion(question)"
                >
                  重新答题
                </el-button>
                <el-button size="small" :icon="Check" @click="handleMarkMastered(question)">
                  标记已掌握
                </el-button>
              </div>
            </div>
          </div>
        </div>
      </template>

      <el-empty v-else description="暂无错题记录">
        <el-button type="primary" @click="router.push('/home')"> 去答题 </el-button>
      </el-empty>
    </div>
  </div>
</template>

<style scoped>
.wrong-questions-container {
  @apply p-6;
}

.filter-bar {
  @apply flex justify-between items-center mb-4 p-4 bg-bg rounded-lg;
}

.filter-left {
  @apply flex gap-3;
}

.filter-select {
  @apply w-40;
}

.date-picker {
  @apply w-60;
}

.filter-right {
  @apply flex gap-2;
}

.stats-bar {
  @apply flex gap-6 mb-4 text-sm text-text-secondary;
}

.stat-item {
  @apply flex items-center gap-2;
}

.bank-group {
  @apply mb-6;
}

.bank-header {
  @apply flex items-center gap-3 mb-3 pb-2 border-b border-border;
}

.bank-name {
  @apply font-semibold text-text-primary text-lg;
}

.bank-count {
  @apply text-text-secondary text-sm;
}

.questions-list {
  @apply grid grid-cols-1 md:grid-cols-2 gap-4;
}

.question-card {
  @apply p-4 bg-bg rounded-lg border border-border;
}

.question-header {
  @apply flex justify-between items-center mb-3;
}

.question-date {
  @apply text-xs text-text-muted;
}

.question-content {
  @apply text-text-primary mb-3 line-clamp-3;
}

.question-options {
  @apply text-sm text-text-secondary mb-3;
  word-break: break-word;
}

.answer-section {
  @apply space-y-2 mb-3 p-3 bg-bg-secondary rounded;
}

.answer-item {
  @apply flex items-start gap-2 text-sm;
}

.answer-label {
  @apply text-text-muted shrink-0;
}

.answer-value {
  @apply font-medium;
  word-break: break-word;
}

.answer-value.wrong {
  @apply text-danger;
}

.answer-value.correct {
  @apply text-success;
}

.explanation {
  @apply text-sm text-text-secondary p-3 bg-bg-secondary rounded mb-3;
}

.explanation-label {
  @apply font-medium text-text-primary;
}

.question-actions {
  @apply flex gap-2;
}

@media (max-width: 768px) {
  .wrong-questions-container {
    @apply p-4;
  }

  .filter-bar {
    @apply flex-col items-stretch gap-3;
  }

  .filter-left {
    @apply flex-col gap-2;
  }

  .filter-select,
  .date-picker {
    @apply w-full;
  }

  .filter-right {
    @apply grid grid-cols-2 gap-2;
  }

  .filter-right .el-button {
    @apply w-full;
  }

  .stats-bar {
    @apply flex-col gap-2;
  }

  .question-header {
    @apply items-start gap-2;
  }

  .answer-item {
    @apply flex-col gap-1;
  }

  .question-actions {
    @apply flex-col;
  }

  .question-actions .el-button {
    @apply w-full;
  }
}
</style>
