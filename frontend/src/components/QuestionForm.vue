<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Delete, Plus } from '@element-plus/icons-vue'
import { useQuestionStore } from '@/stores/question'
import type { Question, CreateQuestionDto, UpdateQuestionDto } from '@/types'
import type {
  QuestionType,
  Difficulty,
  QuestionData,
  ChoiceQuestionData,
  BooleanQuestionData,
  FillBlankQuestionData,
  ShortAnswerQuestionData
} from '@/types/question'
import { QuestionType as QuestionTypeEnum } from '@/types/question'

interface Props {
  question?: Question | null
  visible: boolean
  mode: 'create' | 'edit'
  questionBankId?: string
}

const props = defineProps<Props>()

const emit = defineEmits<{
  close: []
  success: [question: Question]
}>()

const questionStore = useQuestionStore()
const formRef = ref<FormInstance>()
const loading = ref(false)

interface QuestionFormState {
  questionBankId: number
  questionText: string
  questionTypeEnum: QuestionType
  options?: string[]
  correctAnswer: string | string[]
  explanation?: string
  difficulty: Difficulty
  tags?: string[]
}

const form = ref<QuestionFormState>({
  questionBankId: props.questionBankId ? Number(props.questionBankId) : 0,
  questionText: '',
  questionTypeEnum: QuestionTypeEnum.SingleChoice,
  options: ['', '', '', ''],
  correctAnswer: '',
  explanation: '',
  difficulty: 'medium',
  tags: []
})

const tagInput = ref('')
const inputTagVisible = ref(false)
const inputTagRef = ref<HTMLInputElement>()

// 判断是否需要选项
const needsOptions = computed(() => {
  return [QuestionTypeEnum.SingleChoice, QuestionTypeEnum.MultipleChoice].includes(form.value.questionTypeEnum)
})

// 判断是否为多选题
const isMultipleChoice = computed(() => {
  return form.value.questionTypeEnum === QuestionTypeEnum.MultipleChoice
})

const rules: FormRules = {
  questionBankId: [
    { required: true, message: '请选择题库', trigger: 'change' }
  ],
  questionText: [
    { required: true, message: '请输入题目内容', trigger: 'blur' },
    { min: 5, max: 1000, message: '题目内容长度应在5-1000个字符之间', trigger: 'blur' }
  ],
  questionTypeEnum: [
    { required: true, message: '请选择题型', trigger: 'change' }
  ],
  options: [
    {
      validator: (_rule: any, _value: any, callback: any) => {
        if (needsOptions.value) {
          const options = form.value.options || []
          const validOptions = options.filter((opt: string) => opt && opt.trim())
          if (validOptions.length < 2) {
            callback(new Error('至少需要2个选项'))
          } else {
            callback()
          }
        } else {
          callback()
        }
      },
      trigger: 'change'
    }
  ],
  correctAnswer: [
    { required: true, message: '请配置正确答案', trigger: 'change' }
  ],
  difficulty: [
    { required: true, message: '请选择难度', trigger: 'change' }
  ]
}

const dialogTitle = computed(() => (props.mode === 'create' ? '创建题目' : '编辑题目'))

const questionTypeOptions = [
  { label: '单选题', value: QuestionTypeEnum.SingleChoice },
  { label: '多选题', value: QuestionTypeEnum.MultipleChoice },
  { label: '判断题', value: QuestionTypeEnum.TrueFalse },
  { label: '填空题', value: QuestionTypeEnum.FillBlank },
  { label: '简答题', value: QuestionTypeEnum.ShortAnswer }
]

const difficultyOptions = [
  { label: '简单', value: 'easy' },
  { label: '中等', value: 'medium' },
  { label: '困难', value: 'hard' }
]

// 判断题选项
const trueFalseOptions = [
  { label: '正确', value: 'true' },
  { label: '错误', value: 'false' }
]

watch(
  () => props.visible,
  val => {
    if (val && props.question) {
      // 编辑模式 - 需要适配新旧数据格式
      form.value = {
        questionBankId: props.questionBankId ? Number(props.questionBankId) : 0,
        // 兼容新旧数据格式
        questionText: (props.question as any).questionText || (props.question as any).content || '',
        questionTypeEnum: normalizeQuestionType((props.question as any).questionType || (props.question as any).type),
        options: (props.question as any).data?.options || (props.question as any).options || ['', '', '', ''],
        correctAnswer: extractCorrectAnswer(props.question),
        explanation: props.question.explanation || (props.question as any).data?.explanation || '',
        difficulty: (props.question as any).data?.difficulty || props.question.difficulty || 'medium',
        tags: (props.question as any).tags || []
      }
    } else if (val && props.mode === 'create') {
      // 创建模式
      form.value = {
        questionBankId: props.questionBankId ? Number(props.questionBankId) : 0,
        questionText: '',
        questionTypeEnum: QuestionTypeEnum.SingleChoice,
        options: ['', '', '', ''],
        correctAnswer: '',
        explanation: '',
        difficulty: 'medium',
        tags: []
      }
    }
  }
)

// 辅助函数：从题目中提取正确答案
function normalizeQuestionType(type: string | undefined): QuestionType {
  if (!type) return QuestionTypeEnum.SingleChoice
  const map: Record<string, QuestionType> = {
    choice: QuestionTypeEnum.SingleChoice,
    single: QuestionTypeEnum.SingleChoice,
    'multiple-choice': QuestionTypeEnum.MultipleChoice,
    multiple: QuestionTypeEnum.MultipleChoice,
    'true-false': QuestionTypeEnum.TrueFalse,
    boolean: QuestionTypeEnum.TrueFalse,
    fill: QuestionTypeEnum.FillBlank,
    'fill-blank': QuestionTypeEnum.FillBlank,
    'short-answer': QuestionTypeEnum.ShortAnswer
  }
  return map[type] || (type as QuestionType)
}

function extractCorrectAnswer(question: Question): string {
  const data = (question as any).data as QuestionData | undefined
  if (data) {
    if ((data as ChoiceQuestionData).correctAnswers) {
      return (data as ChoiceQuestionData).correctAnswers.join(',')
    }
    if ((data as BooleanQuestionData).correctAnswer !== undefined) {
      return (data as BooleanQuestionData).correctAnswer ? 'true' : 'false'
    }
    if ((data as FillBlankQuestionData).acceptableAnswers) {
      return (data as FillBlankQuestionData).acceptableAnswers.join(',')
    }
    if ((data as ShortAnswerQuestionData).referenceAnswer) {
      return (data as ShortAnswerQuestionData).referenceAnswer
    }
  }
  return question.correctAnswer || ''
}

// 监听题型变化,自动调整答案格式
watch(
  () => form.value.questionTypeEnum,
  (newType, oldType) => {
    if (newType === oldType) return

    const needsOptions = newType === QuestionTypeEnum.SingleChoice || newType === QuestionTypeEnum.MultipleChoice
    const isTrueFalse = newType === QuestionTypeEnum.TrueFalse

    form.value.options = needsOptions ? ['', '', '', ''] : undefined
    form.value.correctAnswer = isTrueFalse ? 'true' : ''
  }
)

const handleAddOption = () => {
  if (!form.value.options) {
    form.value.options = []
  }
  form.value.options.push('')
}

const handleRemoveOption = (index: number) => {
  if (form.value.options && form.value.options.length > 2) {
    form.value.options.splice(index, 1)
    // 如果删除的是当前选中的答案,清空答案
    if (form.value.correctAnswer === String.fromCharCode(65 + index)) {
      form.value.correctAnswer = ''
    }
  } else {
    ElMessage.warning('至少需要保留2个选项')
  }
}

const handleOptionChange = (index: number, value: any) => {
  if (form.value.options) {
    form.value.options[index] = value as string
  }
}

const getOptionLabel = (index: number) => {
  return String.fromCharCode(65 + index) // A, B, C, D, ...
}

// 标签相关方法
const showTagInput = () => {
  inputTagVisible.value = true
  setTimeout(() => {
    inputTagRef.value?.focus()
  }, 100)
}

const handleTagInputConfirm = () => {
  const tag = tagInput.value.trim()
  if (tag) {
    if (!form.value.tags) {
      form.value.tags = []
    }
    if (!form.value.tags.includes(tag)) {
      form.value.tags.push(tag)
    }
  }
  inputTagVisible.value = false
  tagInput.value = ''
}

const handleTagClose = (tag: string) => {
  if (form.value.tags) {
    const index = form.value.tags.indexOf(tag)
    if (index > -1) {
      form.value.tags.splice(index, 1)
    }
  }
}

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async valid => {
    if (!valid) return

    loading.value = true
    try {
      const data = buildQuestionData()

      if (props.mode === 'create') {
        const createData: CreateQuestionDto = {
          questionBankId: form.value.questionBankId,
          questionText: form.value.questionText,
          questionTypeEnum: form.value.questionTypeEnum,
          data,
          explanation: form.value.explanation,
          difficulty: form.value.difficulty
        }
        const result = await questionStore.createQuestion(createData)
        ElMessage.success('创建题目成功')
        emit('success', result)
      } else if (props.question) {
        const updateData: UpdateQuestionDto = {
          questionText: form.value.questionText,
          questionTypeEnum: form.value.questionTypeEnum,
          data,
          explanation: form.value.explanation,
          difficulty: form.value.difficulty
        }
        const result = await questionStore.updateQuestion(props.question.id, updateData)
        ElMessage.success('更新题目成功')
        emit('success', result)
      }
    } catch {
      // 错误已在 store 中处理
    } finally {
      loading.value = false
    }
  })
}

const handleClose = () => {
  emit('close')
}

function buildQuestionData(): QuestionData | undefined {
  const explanation = form.value.explanation || undefined
  const difficulty = form.value.difficulty

  if (form.value.questionTypeEnum === QuestionTypeEnum.SingleChoice ||
      form.value.questionTypeEnum === QuestionTypeEnum.MultipleChoice) {
    const options = (form.value.options || []).map(opt => opt.trim()).filter(Boolean)
    const answers = Array.isArray(form.value.correctAnswer)
      ? form.value.correctAnswer
      : String(form.value.correctAnswer || '')
          .split(',')
          .map(value => value.trim())
          .filter(Boolean)

    return {
      type: 'choice',
      options,
      correctAnswers: answers,
      explanation,
      difficulty
    } as ChoiceQuestionData
  }

  if (form.value.questionTypeEnum === QuestionTypeEnum.TrueFalse) {
    return {
      type: 'boolean',
      correctAnswer: String(form.value.correctAnswer) === 'true',
      explanation,
      difficulty
    } as BooleanQuestionData
  }

  if (form.value.questionTypeEnum === QuestionTypeEnum.FillBlank) {
    const answers = String(form.value.correctAnswer || '')
      .split(/[,，\n]/)
      .map(value => value.trim())
      .filter(Boolean)
    return {
      type: 'fillBlank',
      acceptableAnswers: answers,
      explanation,
      difficulty
    } as FillBlankQuestionData
  }

  if (form.value.questionTypeEnum === QuestionTypeEnum.ShortAnswer) {
    return {
      type: 'shortAnswer',
      referenceAnswer: String(form.value.correctAnswer || ''),
      explanation,
      difficulty
    } as ShortAnswerQuestionData
  }

  return undefined
}
</script>

<template>
  <el-dialog
    :model-value="visible"
    :title="dialogTitle"
    width="700px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <el-form
      ref="formRef"
      :model="form"
      :rules="rules"
      label-width="100px"
      :disabled="loading"
    >

      <el-form-item label="题型" prop="questionTypeEnum">
        <el-select
          v-model="form.questionTypeEnum"
          placeholder="请选择题型"
          style="width: 100%"
        >
          <el-option
            v-for="option in questionTypeOptions"
            :key="option.value"
            :label="option.label"
            :value="option.value"
          />
        </el-select>
      </el-form-item>

      <el-form-item label="题目内容" prop="questionText">
        <el-input
          v-model="form.questionText"
          type="textarea"
          :rows="3"
          placeholder="请输入题目内容"
          maxlength="1000"
          show-word-limit
        />
      </el-form-item>

      <!-- 选项配置 (单选/多选) -->
      <template v-if="needsOptions">
        <el-form-item label="选项" prop="options" required>
          <div class="options-container">
            <div
              v-for="(option, index) in form.options"
              :key="index"
              class="option-item"
            >
              <span class="option-label">{{ getOptionLabel(index) }}.</span>
              <el-input
                :model-value="option"
                @update:model-value="(val: any) => handleOptionChange(index, val as string)"
                placeholder="请输入选项内容"
                maxlength="200"
              >
                <template #append>
                  <el-button
                    :icon="Delete"
                    @click="handleRemoveOption(index)"
                    :disabled="form.options!.length <= 2"
                  />
                </template>
              </el-input>
            </div>
            <el-button
              type="primary"
              plain
              size="small"
              @click="handleAddOption"
              :icon="Plus"
            >
              添加选项
            </el-button>
          </div>
        </el-form-item>

        <el-form-item label="正确答案" prop="correctAnswer">
          <el-select
            v-model="form.correctAnswer"
            :multiple="isMultipleChoice"
            :placeholder="isMultipleChoice ? '请选择正确答案(可多选)' : '请选择正确答案'"
            style="width: 100%"
          >
            <el-option
              v-for="(option, index) in form.options?.filter(opt => opt?.trim())"
              :key="index"
              :label="`${getOptionLabel(index)}. ${option}`"
              :value="getOptionLabel(index)"
            />
          </el-select>
        </el-form-item>
      </template>

      <!-- 判断题答案 -->
      <el-form-item label="正确答案" prop="correctAnswer" v-else-if="form.questionTypeEnum === QuestionTypeEnum.TrueFalse">
        <el-radio-group v-model="form.correctAnswer">
          <el-radio
            v-for="option in trueFalseOptions"
            :key="option.value"
            :label="option.value"
          >
            {{ option.label }}
          </el-radio>
        </el-radio-group>
      </el-form-item>

      <!-- 填空题答案 -->
      <el-form-item label="参考答案" prop="correctAnswer" v-else-if="form.questionTypeEnum === QuestionTypeEnum.FillBlank || form.questionTypeEnum === QuestionTypeEnum.ShortAnswer">
        <el-input
          v-model="form.correctAnswer"
          type="textarea"
          :rows="2"
          placeholder="请输入参考答案"
          maxlength="500"
          show-word-limit
        />
      </el-form-item>

      <el-form-item label="难度" prop="difficulty">
        <el-radio-group v-model="form.difficulty">
          <el-radio
            v-for="option in difficultyOptions"
            :key="option.value"
            :label="option.value"
          >
            {{ option.label }}
          </el-radio>
        </el-radio-group>
      </el-form-item>

      <el-form-item label="解析" prop="explanation">
        <el-input
          v-model="form.explanation"
          type="textarea"
          :rows="3"
          placeholder="请输入题目解析(可选)"
          maxlength="1000"
          show-word-limit
        />
      </el-form-item>

      <el-form-item label="标签" prop="tags">
        <div class="tags-container">
          <el-tag
            v-for="tag in form.tags"
            :key="tag"
            closable
            @close="handleTagClose(tag)"
            style="margin-right: 8px"
          >
            {{ tag }}
          </el-tag>
          <el-input
            v-if="inputTagVisible"
            ref="inputTagRef"
            v-model="tagInput"
            size="small"
            style="width: 100px"
            @blur="handleTagInputConfirm"
            @keyup.enter="handleTagInputConfirm"
          />
          <el-button
            v-else
            size="small"
            @click="showTagInput"
          >
            + 添加标签
          </el-button>
        </div>
      </el-form-item>
    </el-form>

    <template #footer>
      <el-button @click="handleClose">取消</el-button>
      <el-button type="primary" :loading="loading" @click="handleSubmit">
        {{ loading ? '保存中...' : '确定' }}
      </el-button>
    </template>
  </el-dialog>
</template>

<style scoped>
.options-container {
  width: 100%;
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
}

.option-item {
  display: flex;
  align-items: center;
  gap: 0.5rem;
}

.option-label {
  font-weight: 600;
  color: #606266;
  min-width: 24px;
}

.tags-container {
  display: flex;
  flex-wrap: wrap;
  align-items: center;
  gap: 0.5rem;
}
</style>
