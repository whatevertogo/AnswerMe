<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance } from 'element-plus'
import { Plus, Edit, Delete, Star, CircleCheck, Connection } from '@element-plus/icons-vue'
import { storeToRefs } from 'pinia'
import { useDataSourceStore } from '@/stores/dataSource'
import {
  type DataSource,
  type CreateDataSourceParams,
  type UpdateDataSourceParams
} from '@/api/datasource'
import {
  aiProviders,
  getProviderByValue,
  getProviderLabel,
  getProviderTagType,
  getProviderDefaultEndpoint,
  getProviderDefaultModel
} from '@/constants/aiProviders'

const route = useRoute()
const dataSourceStore = useDataSourceStore()
const { dataSources, loading } = storeToRefs(dataSourceStore)

// State
const dialogVisible = ref(false)
const dialogMode = ref<'create' | 'edit'>('create')
const currentDataSource = ref<DataSource | null>(null)
const validatingId = ref<number | null>(null)
const formRef = ref<FormInstance>()

// Form
const formData = ref<CreateDataSourceParams>({
  name: '',
  type: 'openai',
  apiKey: '',
  endpoint: '',
  model: 'gpt-3.5-turbo',
  isDefault: false
})

const validateApiKey = (_rule: any, value: string, callback: (error?: Error) => void) => {
  if (dialogMode.value === 'create' && (!value || !value.trim())) {
    callback(new Error('请输入API密钥'))
    return
  }
  callback()
}

const formRules = {
  name: [{ required: true, message: '请输入配置名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择提供商', trigger: 'change' }],
  apiKey: [{ validator: validateApiKey, trigger: 'blur' }]
}

onMounted(() => {
  fetchDataSources().then(() => {
    handleRouteQuery()
  })
})

// 监听路由查询参数变化
watch(
  [() => route.query, () => dataSources.value.length],
  () => {
    handleRouteQuery()
  },
  { immediate: true }
)

// 根据路由参数自动打开对话框
function handleRouteQuery() {
  const { action, id } = route.query

  if (action === 'create') {
    // 创建模式
    handleAdd()
  } else if (action === 'edit' && id) {
    // 编辑模式
    const dataSource = dataSources.value.find(ds => ds.id === Number(id))
    if (dataSource) {
      handleEdit(dataSource)
    }
  }
}

// Methods
async function fetchDataSources() {
  try {
    await dataSourceStore.fetchDataSources()
  } catch (error: any) {
    ElMessage.error('加载数据源列表失败: ' + (error.message || '未知错误'))
  }
}

function handleAdd() {
  dialogMode.value = 'create'
  currentDataSource.value = null
  const defaultProvider = aiProviders[0]
  formData.value = {
    name: '',
    type: defaultProvider?.value || 'openai',
    apiKey: '',
    endpoint: defaultProvider?.defaultEndpoint || '',
    model: defaultProvider?.defaultModel || '',
    isDefault: false
  }
  dialogVisible.value = true
}

function handleEdit(dataSource: DataSource) {
  dialogMode.value = 'edit'
  currentDataSource.value = dataSource
  formData.value = {
    name: dataSource.name,
    type: dataSource.type,
    apiKey: '', // 编辑时不显示已有密钥
    endpoint: dataSource.endpoint || '',
    model: dataSource.model || '',
    isDefault: dataSource.isDefault
  }
  dialogVisible.value = true
}

async function handleDelete(dataSource: DataSource) {
  try {
    await ElMessageBox.confirm(
      `确定要删除数据源"${dataSource.name}"吗？此操作不可恢复。`,
      '删除确认',
      {
        confirmButtonText: '确定',
        cancelButtonText: '取消',
        type: 'warning'
      }
    )

    await dataSourceStore.deleteDataSource(dataSource.id)
    ElMessage.success('删除成功')
    await fetchDataSources()
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败: ' + (error.message || '未知错误'))
    }
  }
}

async function handleSetDefault(dataSource: DataSource) {
  try {
    await dataSourceStore.setDefault(dataSource.id)
    ElMessage.success(`已将"${dataSource.name}"设置为默认数据源`)
    await fetchDataSources()
  } catch (error: any) {
    ElMessage.error('设置失败: ' + (error.message || '未知错误'))
  }
}

async function handleValidate(dataSource: DataSource) {
  validatingId.value = dataSource.id
  try {
    const isValid = await dataSourceStore.validateApiKey(dataSource.id)
    if (isValid) {
      ElMessage.success('API密钥有效')
    } else {
      ElMessage.error('API密钥无效')
    }
  } catch (error: any) {
    ElMessage.error('验证失败: ' + (error.message || '未知错误'))
  } finally {
    validatingId.value = null
  }
}

async function handleSubmit() {
  if (!formRef.value) return

  await formRef.value.validate(async (valid) => {
    if (!valid) return

    try {
      const provider = getProviderByValue(formData.value.type)
      const endpointInput = (formData.value.endpoint ?? '').trim()
      const modelInput = (formData.value.model ?? '').trim()
      const normalizedEndpoint = endpointInput || provider?.defaultEndpoint || ''
      const normalizedModel = modelInput || provider?.defaultModel || ''

      if (dialogMode.value === 'create') {
        const createData: CreateDataSourceParams = {
          ...formData.value,
          endpoint: normalizedEndpoint,
          model: normalizedModel
        }
        await dataSourceStore.createDataSource(createData)
        ElMessage.success('创建成功')
      } else if (currentDataSource.value) {
        const updateData: UpdateDataSourceParams = {
          name: formData.value.name,
          type: formData.value.type,
          endpoint: normalizedEndpoint,
          model: normalizedModel,
          isDefault: formData.value.isDefault
        }
        // 只有当用户输入了新密钥时才更新
        if (formData.value.apiKey) {
          updateData.apiKey = formData.value.apiKey
        }
        await dataSourceStore.updateDataSource(currentDataSource.value.id, updateData)
        ElMessage.success('更新成功')
      }
      dialogVisible.value = false
      await fetchDataSources()
    } catch (error: any) {
      ElMessage.error((dialogMode.value === 'create' ? '创建' : '更新') + '失败: ' + (error.message || '未知错误'))
    }
  })
}

function handleProviderChange() {
  const provider = getProviderByValue(formData.value.type)
  if (provider) {
    formData.value.endpoint = provider.defaultEndpoint || ''
    formData.value.model = provider.defaultModel || ''
  }
}

function handleDialogClose() {
  const defaultProvider = aiProviders[0]
  formData.value = {
    name: '',
    type: defaultProvider?.value || 'openai',
    apiKey: '',
    endpoint: defaultProvider?.defaultEndpoint || '',
    model: defaultProvider?.defaultModel || 'gpt-3.5-turbo',
    isDefault: false
  }
  formRef.value?.resetFields()
  formRef.value?.clearValidate()
}

function getTypeLabel(type: string) {
  return getProviderLabel(type)
}

function getCurrentProviderEndpoint() {
  const endpoint = getProviderDefaultEndpoint(formData.value.type)
  if (endpoint) {
    return `默认端点: ${endpoint}`
  }
  return '请输入自定义端点地址'
}

function getCurrentProviderModel() {
  const model = getProviderDefaultModel(formData.value.type)
  if (model) {
    return `默认模型: ${model}`
  }
  return '请输入模型名称'
}

function getTypeTagType(type: string) {
  return getProviderTagType(type)
}
</script>

<template>
  <div class="ai-config-view">
    <!-- 页面头部 -->
    <div class="header">
      <div class="header-left">
        <h1>AI配置管理</h1>
        <p class="subtitle">管理您的AI数据源，用于题目生成</p>
      </div>
      <el-button type="primary" :icon="Plus" @click="handleAdd">
        添加配置
      </el-button>
    </div>

    <!-- 数据源列表 -->
    <el-card v-loading="loading" shadow="never">
      <el-empty v-if="!loading && dataSources.length === 0" description="暂无AI配置">
        <el-button type="primary" @click="handleAdd">添加第一个配置</el-button>
      </el-empty>

      <div v-else class="datasource-list">
        <div
          v-for="dataSource in dataSources"
          :key="dataSource.id"
          class="datasource-item"
        >
          <div class="datasource-info">
            <div class="info-header">
              <el-icon class="type-icon" :size="24">
                <Connection />
              </el-icon>
              <div class="info-text">
                <div class="name-row">
                  <span class="name">{{ dataSource.name }}</span>
                  <el-tag v-if="dataSource.isDefault" type="success" size="small">
                    默认
                  </el-tag>
                </div>
                <el-tag :type="getTypeTagType(dataSource.type)" size="small">
                  {{ getTypeLabel(dataSource.type) }}
                </el-tag>
              </div>
            </div>

            <div class="info-details">
              <div v-if="dataSource.endpoint" class="detail-item">
                <span class="label">端点:</span>
                <span class="value">{{ dataSource.endpoint }}</span>
              </div>
              <div v-if="dataSource.model" class="detail-item">
                <span class="label">模型:</span>
                <span class="value">{{ dataSource.model }}</span>
              </div>
              <div class="detail-item">
                <span class="label">API密钥:</span>
                <span class="value masked">{{ dataSource.maskedApiKey }}</span>
              </div>
              <div class="detail-item">
                <span class="label">创建时间:</span>
                <span class="value">{{
                  new Date(dataSource.createdAt).toLocaleString('zh-CN')
                }}</span>
              </div>
            </div>
          </div>

          <div class="datasource-actions">
            <el-button
              text
              :loading="validatingId === dataSource.id"
              @click="handleValidate(dataSource)"
            >
              <el-icon><CircleCheck /></el-icon>
              测试
            </el-button>

            <el-button
              v-if="!dataSource.isDefault"
              text
              @click="handleSetDefault(dataSource)"
            >
              <el-icon><Star /></el-icon>
              设为默认
            </el-button>

            <el-button text @click="handleEdit(dataSource)">
              <el-icon><Edit /></el-icon>
              编辑
            </el-button>

            <el-button text type="danger" @click="handleDelete(dataSource)">
              <el-icon><Delete /></el-icon>
              删除
            </el-button>
          </div>
        </div>
      </div>
    </el-card>

    <!-- 添加/编辑对话框 -->
    <el-dialog
      v-model="dialogVisible"
      :title="dialogMode === 'create' ? '添加AI配置' : '编辑AI配置'"
      width="600px"
      :append-to-body="true"
      :close-on-click-modal="false"
      :destroy-on-close="true"
      @close="handleDialogClose"
    >
      <el-form
        ref="formRef"
        :model="formData"
        :rules="formRules"
        label-width="100px"
      >
        <el-form-item label="配置名称" prop="name">
          <el-input
            v-model="formData.name"
            placeholder="例如：我的OpenAI配置"
          />
        </el-form-item>

        <el-form-item label="提供商" prop="type">
          <el-select
            v-model="formData.type"
            placeholder="选择AI提供商"
            @change="handleProviderChange"
          >
            <el-option
              v-for="option in aiProviders"
              :key="option.value"
              :label="option.label"
              :value="option.value"
            />
          </el-select>
        </el-form-item>

        <el-form-item label="API密钥" prop="apiKey">
          <el-input
            v-model="formData.apiKey"
            type="password"
            show-password
            :placeholder="dialogMode === 'edit' ? '留空则不修改' : '请输入API密钥'"
          />
        </el-form-item>

        <el-form-item label="端点地址">
          <el-input
            v-model="formData.endpoint"
            placeholder="使用默认端点（自动填充）或输入自定义端点"
          />
          <div class="form-tip">
            {{ getCurrentProviderEndpoint() }}
          </div>
        </el-form-item>

        <el-form-item label="模型">
          <el-input
            v-model="formData.model"
            placeholder="使用默认模型（自动填充）或输入自定义模型"
          />
          <div class="form-tip">
            {{ getCurrentProviderModel() }}
          </div>
        </el-form-item>

        <el-form-item label="设为默认">
          <el-switch v-model="formData.isDefault" />
        </el-form-item>
      </el-form>

      <template #footer>
        <el-button @click="dialogVisible = false">取消</el-button>
        <el-button type="primary" @click="handleSubmit">
          {{ dialogMode === 'create' ? '创建' : '保存' }}
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<style scoped>
.ai-config-view {
  @apply p-6 max-w-[1400px] mx-auto;
}

.header {
  @apply flex justify-between items-start mb-6;
}

.header-left h1 {
  @apply m-0 text-[24px] font-semibold;
  color: #303133;
}

.subtitle {
  @apply mt-1 mb-0 text-sm;
  color: #909399;
}

.datasource-list {
  @apply flex flex-col gap-4;
}

.datasource-item {
  @apply flex justify-between items-center px-5 py-5
         border border-[#e4e7ed] rounded-lg
         transition-all duration-300
         hover:border-[#409eff] hover:shadow-[0_2px_12px_rgba(64,158,255,0.1)];
}

.datasource-info {
  @apply flex-1;
}

.info-header {
  @apply flex items-start gap-3 mb-3;
}

.type-icon {
  @apply text-[#409eff] flex-shrink-0;
}

.info-text {
  @apply flex-1;
}

.name-row {
  @apply flex items-center gap-2 mb-2;
}

.name {
  @apply text-base font-semibold;
  color: #303133;
}

.info-details {
  @apply grid grid-cols-2 gap-x-6 gap-y-2 ml-9;
}

.detail-item {
  @apply flex items-center gap-2 text-sm;
}

.label {
  @apply text-[#909399] whitespace-nowrap;
}

.value {
  @apply text-[#606266];
}

.masked {
  @apply font-mono text-[#909399];
}

.datasource-actions {
  @apply flex items-center gap-2 ml-6;
}

:deep(.el-form-item__label) {
  @apply font-medium;
}

/* 确保对话框正确显示 */
:deep(.el-dialog) {
  position: fixed !important;
  z-index: 9999 !important;
}

:deep(.el-dialog__wrapper) {
  position: fixed !important;
  z-index: 9999 !important;
}

:deep(.el-overlay) {
  position: fixed !important;
  z-index: 9998 !important;
}

.form-tip {
  @apply mt-1 text-xs text-[#909399] leading-[1.5];
}

</style>
