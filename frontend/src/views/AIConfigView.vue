<script setup lang="ts">
import { ref, onMounted, watch } from 'vue'
import { useRoute } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import type { FormInstance } from 'element-plus'
import { Plus, Edit, Delete, Star, CircleCheck, Connection } from '@element-plus/icons-vue'
import {
  getDataSourcesApi,
  createDataSourceApi,
  updateDataSourceApi,
  deleteDataSourceApi,
  setDefaultDataSourceApi,
  validateApiKeyApi,
  type DataSource,
  type CreateDataSourceParams,
  type UpdateDataSourceParams
} from '@/api/datasource'

const route = useRoute()

// State
const dataSources = ref<DataSource[]>([])
const loading = ref(false)
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

// Provider options - 所有 Provider 使用 OpenAI 兼容的 API 格式
// endpoint 和 model 作为默认值显示，用户可以选择性覆盖
const providerOptions = [
  {
    label: 'OpenAI',
    value: 'openai',
    model: 'gpt-3.5-turbo',
    endpoint: 'https://api.openai.com/v1/chat/completions'
  },
  {
    label: '通义千问',
    value: 'qwen',
    model: 'qwen-turbo',
    endpoint: 'https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions'
  },
  {
    label: '智谱GLM',
    value: 'zhipu',
    model: 'glm-4',
    endpoint: 'https://open.bigmodel.cn/api/paas/v4/chat/completions'
  },
  {
    label: 'Minimax',
    value: 'minimax',
    model: 'abab6.5s-chat',
    endpoint: 'https://api.minimax.chat/v1/text/chatcompletion_v2'
  },
  {
    label: '自定义API (OpenAI兼容)',
    value: 'custom_api',
    model: '',
    endpoint: 'https://your-api-endpoint.com/v1/chat/completions'
  }
]

const formRules = {
  name: [{ required: true, message: '请输入配置名称', trigger: 'blur' }],
  type: [{ required: true, message: '请选择提供商', trigger: 'change' }],
  apiKey: [{ required: true, message: '请输入API密钥', trigger: 'blur' }]
}

onMounted(() => {
  fetchDataSources()
  // 检查路由参数，自动触发创建/编辑对话框
  handleRouteQuery()
})

// 监听路由查询参数变化
watch(() => route.query, () => {
  handleRouteQuery()
}, { immediate: true })

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
  loading.value = true
  try {
    const response = await getDataSourcesApi()
    dataSources.value = response
  } catch (error: any) {
    ElMessage.error('加载数据源列表失败: ' + (error.message || '未知错误'))
  } finally {
    loading.value = false
  }
}

function handleAdd() {
  console.log('[AIConfigView] handleAdd 被调用')
  dialogMode.value = 'create'
  currentDataSource.value = null
  const defaultProvider = providerOptions[0]  // OpenAI
  if (defaultProvider) {
    formData.value = {
      name: '',
      type: 'openai',
      apiKey: '',
      endpoint: defaultProvider.endpoint,
      model: defaultProvider.model,
      isDefault: false
    }
  }
  dialogVisible.value = true
  console.log('[AIConfigView] dialogVisible 已设置为:', dialogVisible.value)
  // 强制触发响应式更新
  setTimeout(() => {
    console.log('[AIConfigView] dialogVisible 当前值:', dialogVisible.value)
  }, 100)
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

    await deleteDataSourceApi(dataSource.id)
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
    await setDefaultDataSourceApi(dataSource.id)
    ElMessage.success(`已将"${dataSource.name}"设置为默认数据源`)
    await fetchDataSources()
  } catch (error: any) {
    ElMessage.error('设置失败: ' + (error.message || '未知错误'))
  }
}

async function handleValidate(dataSource: DataSource) {
  validatingId.value = dataSource.id
  try {
    const response = await validateApiKeyApi(dataSource.id)
    if (response.valid) {
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
      if (dialogMode.value === 'create') {
        await createDataSourceApi(formData.value)
        ElMessage.success('创建成功')
      } else if (currentDataSource.value) {
        const updateData: UpdateDataSourceParams = {
          name: formData.value.name,
          endpoint: formData.value.endpoint || undefined,
          model: formData.value.model || undefined,
          isDefault: formData.value.isDefault
        }
        // 只有当用户输入了新密钥时才更新
        if (formData.value.apiKey) {
          updateData.apiKey = formData.value.apiKey
        }
        await updateDataSourceApi(currentDataSource.value.id, updateData)
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
  const provider = providerOptions.find(p => p.value === formData.value.type)
  if (provider) {
    // 自动填充默认端点和模型
    formData.value.endpoint = provider.endpoint || ''
    formData.value.model = provider.model || ''
  }
}

function handleDialogClose() {
  const defaultProvider = providerOptions[0]
  formData.value = {
    name: '',
    type: 'openai',
    apiKey: '',
    endpoint: defaultProvider?.endpoint || '',
    model: defaultProvider?.model || 'gpt-3.5-turbo',
    isDefault: false
  }
  formRef.value?.resetFields()
}

function getTypeLabel(type: string) {
  const typeMap: Record<string, string> = {
    openai: 'OpenAI',
    qwen: '通义千问',
    zhipu: '智谱GLM',
    minimax: 'Minimax',
    custom_api: '自定义API'
  }
  return typeMap[type] || type
}

function getCurrentProviderEndpoint() {
  const provider = providerOptions.find(p => p.value === formData.value.type)
  if (provider?.endpoint) {
    return `默认端点: ${provider.endpoint}`
  }
  return '请输入自定义端点地址'
}

function getCurrentProviderModel() {
  const provider = providerOptions.find(p => p.value === formData.value.type)
  if (provider?.model) {
    return `默认模型: ${provider.model}`
  }
  return '请输入模型名称'
}

function getTypeTagType(type: string) {
  const typeColorMap: Record<string, any> = {
    openai: 'success',
    qwen: 'primary',
    zhipu: 'warning',
    minimax: 'info',
    custom_api: 'info'
  }
  return typeColorMap[type] || 'info'
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
              v-for="option in providerOptions"
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
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: flex-start;
  margin-bottom: 24px;
}

.header-left h1 {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
  color: #303133;
}

.subtitle {
  margin: 4px 0 0 0;
  font-size: 14px;
  color: #909399;
}

.datasource-list {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.datasource-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 20px;
  border: 1px solid #e4e7ed;
  border-radius: 8px;
  transition: all 0.3s;
}

.datasource-item:hover {
  border-color: #409eff;
  box-shadow: 0 2px 12px rgba(64, 158, 255, 0.1);
}

.datasource-info {
  flex: 1;
}

.info-header {
  display: flex;
  align-items: flex-start;
  gap: 12px;
  margin-bottom: 12px;
}

.type-icon {
  color: #409eff;
  flex-shrink: 0;
}

.info-text {
  flex: 1;
}

.name-row {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-bottom: 8px;
}

.name {
  font-size: 16px;
  font-weight: 600;
  color: #303133;
}

.info-details {
  display: grid;
  grid-template-columns: repeat(2, 1fr);
  gap: 8px 24px;
  margin-left: 36px;
}

.detail-item {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.label {
  color: #909399;
  white-space: nowrap;
}

.value {
  color: #606266;
}

.masked {
  font-family: monospace;
  color: #909399;
}

.datasource-actions {
  display: flex;
  align-items: center;
  gap: 8px;
  margin-left: 24px;
}

:deep(.el-form-item__label) {
  font-weight: 500;
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
  margin-top: 4px;
  font-size: 12px;
  color: #909399;
  line-height: 1.5;
}

</style>
