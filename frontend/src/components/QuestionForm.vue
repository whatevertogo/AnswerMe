<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { Delete, Plus } from '@element-plus/icons-vue'
import { useQuestionStore } from '@/stores/question'
import { QuestionType, Difficulty } from '@/types'
import type { Question, CreateQuestionDto, UpdateQuestionDto } from '@/types'

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

const form = ref<CreateQuestionDto>({
  questionBankId: props.questionBankId || '',
  content: '',
  type: QuestionType.CHOICE,
  options: ['', '', '', ''],
  correctAnswer: '',
  explanation: '',
  difficulty: Difficulty.MEDIUM,
  tags: []
})

const tagInput = ref('')
const inputTagVisible = ref(false)
const inputTagRef = ref<HTMLInputElement>()

// 判断是否需要选项
const needsOptions = computed(() => {
  return [
    QuestionType.CHOICE,
    QuestionType.MULTIPLE_CHOICE
  ].includes(form.value.type as any)
})

// 判断是否为多选题
const isMultipleChoice = computed(() => {
  return form.value.type === QuestionType.MULTIPLE_CHOICE
})

const rules: FormRules = {
  questionBankId: [
    { required: true, message: '请选择题库', trigger: 'change' }
  ],
  content: [
    { required: true, message: '请输入题目内容', trigger: 'blur' },
    { min: 5, max: 1000, message: '题目内容长度应在5-1000个字符之间', trigger: 'blur' }
  ],
  type: [
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
  { label: '单选题', value: QuestionType.CHOICE },
  { label: '多选题', value: QuestionType.MULTIPLE_CHOICE },
  { label: '判断题', value: QuestionType.TRUE_FALSE },
  { label: '填空题', value: QuestionType.SHORT_ANSWER }
]

const difficultyOptions = [
  { label: '简单', value: Difficulty.EASY },
  { label: '中等', value: Difficulty.MEDIUM },
  { label: '困难', value: Difficulty.HARD }
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
      // 编辑模式
      form.value = {
        questionBankId: props.questionBankId || '',
        content: props.question.content,
        type: props.question.type,
        options: props.question.options || ['', '', '', ''],
        correctAnswer: props.question.correctAnswer,
        explanation: props.question.explanation || '',
        difficulty: props.question.difficulty,
        tags: props.question.tags || []
      }
    } else if (val && props.mode === 'create') {
      // 创建模式
      form.value = {
        questionBankId: props.questionBankId || '',
        content: '',
        type: QuestionType.CHOICE,
        options: ['', '', '', ''],
        correctAnswer: '',
        explanation: '',
        difficulty: Difficulty.MEDIUM,
        tags: []
      }
    }
  }
)

// 监听题型变化,自动调整答案格式
watch(
  () => form.value.type,
  (newType, oldType) => {
    if (newType === oldType) return

    // 判断题
    if (newType === QuestionType.TRUE_FALSE) {
      form.value.correctAnswer = 'true'
      form.value.options = undefined
    }
    // 选择题
    else if (newType === QuestionType.CHOICE) {
      form.value.correctAnswer = ''
      form.value.options = ['', '', '', '']
    }
    // 多选题
    else if (newType === QuestionType.MULTIPLE_CHOICE) {
      form.value.correctAnswer = ''
      form.value.options = ['', '', '', '']
    }
    // 填空题
    else if (newType === QuestionType.SHORT_ANSWER) {
      form.value.correctAnswer = ''
      form.value.options = undefined
    }
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
      if (props.mode === 'create') {
        const result = await questionStore.createQuestion(form.value)
        ElMessage.success('创建题目成功')
        emit('success', result)
      } else if (props.question) {
        const updateData: UpdateQuestionDto = {
          content: form.value.content,
          type: form.value.type,
          options: form.value.options,
          correctAnswer: form.value.correctAnswer,
          explanation: form.value.explanation,
          difficulty: form.value.difficulty,
          tags: form.value.tags
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

      <el-form-item label="题型" prop="type">
        <el-select
          v-model="form.type"
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

      <el-form-item label="题目内容" prop="content">
        <el-input
          v-model="form.content"
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
      <el-form-item label="正确答案" prop="correctAnswer" v-else-if="form.type === QuestionType.TRUE_FALSE">
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
      <el-form-item label="参考答案" prop="correctAnswer" v-else-if="form.type === QuestionType.SHORT_ANSWER">
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
