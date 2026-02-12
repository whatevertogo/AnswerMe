<script setup lang="ts">
import { ref, computed, watch } from 'vue'
import { ElMessage, type FormInstance, type FormRules } from 'element-plus'
import { useQuestionBankStore } from '@/stores/questionBank'
import type { QuestionBank, CreateQuestionBankDto, UpdateQuestionBankDto } from '@/types'

interface Props {
  bank?: QuestionBank | null
  visible: boolean
  mode: 'create' | 'edit'
}

const props = defineProps<Props>()

const emit = defineEmits<{
  close: []
  success: [bank: QuestionBank]
}>()

const questionBankStore = useQuestionBankStore()
const formRef = ref<FormInstance>()
const loading = ref(false)

const form = ref<CreateQuestionBankDto>({
  name: '',
  description: ''
})

const rules: FormRules = {
  name: [
    { required: true, message: '请输入题库名称', trigger: 'blur' },
    { min: 2, max: 100, message: '名称长度应在2-100个字符之间', trigger: 'blur' }
  ]
}

const dialogTitle = computed(() => (props.mode === 'create' ? '创建题库' : '编辑题库'))

watch(
  () => props.visible,
  val => {
    if (val && props.bank) {
      form.value = {
        name: props.bank.name,
        description: props.bank.description || ''
      }
    } else if (val && props.mode === 'create') {
      form.value = { name: '', description: '' }
    }
  }
)

const handleSubmit = async () => {
  if (!formRef.value) return

  await formRef.value.validate(async valid => {
    if (!valid) return

    loading.value = true
    try {
      if (props.mode === 'create') {
        const result = await questionBankStore.createQuestionBank(form.value)
        ElMessage.success('创建题库成功')
        emit('success', result)
      } else if (props.bank) {
        const updateData: UpdateQuestionBankDto = {
          name: form.value.name,
          description: form.value.description,
          version: props.bank.version
        }
        const result = await questionBankStore.updateQuestionBank(props.bank.id, updateData)
        ElMessage.success('更新题库成功')
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
    width="500px"
    :close-on-click-modal="false"
    @close="handleClose"
  >
    <el-form ref="formRef" :model="form" :rules="rules" label-width="100px" :disabled="loading">
      <el-form-item label="题库名称" prop="name">
        <el-input
          v-model="form.name"
          placeholder="请输入题库名称"
          maxlength="100"
          show-word-limit
        />
      </el-form-item>

      <el-form-item label="题库描述" prop="description">
        <el-input
          v-model="form.description"
          type="textarea"
          :rows="4"
          placeholder="请输入题库描述（可选）"
          maxlength="500"
          show-word-limit
        />
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
