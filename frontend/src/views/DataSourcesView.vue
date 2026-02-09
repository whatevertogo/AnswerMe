<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { useDataSourceStore } from '@/stores/dataSource'
import type { DataSource } from '@/api/datasource'

const router = useRouter()
const dataSourceStore = useDataSourceStore()

const searchQuery = ref('')
const validatingId = ref<number | null>(null)

onMounted(async () => {
  try {
    await dataSourceStore.fetchDataSources()
  } catch (error) {
    ElMessage.error('加载数据源列表失败')
  }
})

const filteredDataSources = computed(() => {
  if (!searchQuery.value) {
    return dataSourceStore.dataSources
  }
  const query = searchQuery.value.toLowerCase()
  return dataSourceStore.dataSources.filter(
    ds =>
      ds.name.toLowerCase().includes(query) ||
      ds.type.toLowerCase().includes(query)
  )
})

const handleAdd = () => {
  // 跳转到 AI 配置页面并触发创建模式
  router.push('/ai-config?action=create')
}

const handleEdit = (dataSource: DataSource) => {
  dataSourceStore.setCurrentDataSource(dataSource)
  // 跳转到 AI 配置页面并触发编辑模式
  router.push(`/ai-config?action=edit&id=${dataSource.id}`)
}

const handleDelete = async (dataSource: DataSource) => {
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
  } catch (error: any) {
    if (error !== 'cancel') {
      ElMessage.error('删除失败')
    }
  }
}

const handleSetDefault = async (dataSource: DataSource) => {
  try {
    await dataSourceStore.setDefault(dataSource.id)
    ElMessage.success(`已将"${dataSource.name}"设置为默认数据源`)
  } catch (error) {
    ElMessage.error('设置失败')
  }
}

const handleValidate = async (dataSource: DataSource) => {
  validatingId.value = dataSource.id
  try {
    const isValid = await dataSourceStore.validateApiKey(dataSource.id)
    if (isValid) {
      ElMessage.success('API密钥有效')
    } else {
      ElMessage.error('API密钥无效')
    }
  } catch (error) {
    ElMessage.error('验证失败')
  } finally {
    validatingId.value = null
  }
}

const getTypeLabel = (type: string) => {
  const typeMap: Record<string, string> = {
    openai: 'OpenAI',
    qwen: '通义千问',
    zhipu: '智谱GLM',
    minimax: 'Minimax',
    custom_api: '自定义API'
  }
  return typeMap[type] || type
}

const getTypeTagType = (type: string) => {
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
  <div class="datasources-view">
    <div class="header">
      <h1>AI配置管理</h1>
      <el-button type="primary" :icon="'Plus'" @click="handleAdd">
        添加配置
      </el-button>
    </div>

    <el-card class="search-card">
      <el-input
        v-model="searchQuery"
        placeholder="搜索数据源名称或类型..."
        :prefix-icon="'Search'"
        clearable
      />
    </el-card>

    <div v-if="dataSourceStore.loading" class="loading-container">
      <el-skeleton :rows="3" animated />
    </div>

    <div v-else-if="filteredDataSources.length === 0" class="empty-container">
      <el-empty :description="searchQuery ? '未找到匹配的数据源' : '暂无数据源配置'">
        <el-button v-if="!searchQuery" type="primary" @click="handleAdd">
          添加第一个配置
        </el-button>
      </el-empty>
    </div>

    <div v-else class="datasources-grid">
      <el-card
        v-for="dataSource in filteredDataSources"
        :key="dataSource.id"
        class="datasource-card"
        shadow="hover"
      >
        <template #header>
          <div class="card-header">
            <div class="header-left">
              <el-icon class="type-icon" :size="20">
                <Connection />
              </el-icon>
              <span class="name">{{ dataSource.name }}</span>
              <el-tag v-if="dataSource.isDefault" type="success" size="small">
                默认
              </el-tag>
            </div>
          </div>
        </template>

        <div class="card-body">
          <div class="info-row">
            <span class="label">类型:</span>
            <el-tag :type="getTypeTagType(dataSource.type)" size="small">
              {{ getTypeLabel(dataSource.type) }}
            </el-tag>
          </div>

          <div v-if="dataSource.endpoint" class="info-row">
            <span class="label">端点:</span>
            <span class="value text-truncate">{{ dataSource.endpoint }}</span>
          </div>

          <div v-if="dataSource.model" class="info-row">
            <span class="label">模型:</span>
            <span class="value">{{ dataSource.model }}</span>
          </div>

          <div class="info-row">
            <span class="label">API密钥:</span>
            <span class="value masked">{{ dataSource.maskedApiKey }}</span>
          </div>

          <div class="info-row">
            <span class="label">创建时间:</span>
            <span class="value">{{
              new Date(dataSource.createdAt).toLocaleString('zh-CN')
            }}</span>
          </div>
        </div>

        <template #footer>
          <div class="card-actions">
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

            <div class="right-actions">
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
        </template>
      </el-card>
    </div>
  </div>
</template>

<style scoped>
.datasources-view {
  padding: 24px;
  max-width: 1400px;
  margin: 0 auto;
}

.header {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
}

.header h1 {
  margin: 0;
  font-size: 24px;
  font-weight: 600;
  color: #333;
}

.search-card {
  margin-bottom: 24px;
}

.loading-container,
.empty-container {
  padding: 40px 0;
}

.datasources-grid {
  display: grid;
  grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
  gap: 20px;
}

.datasource-card {
  border-radius: 8px;
}

.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 10px;
}

.type-icon {
  color: #409eff;
}

.name {
  font-weight: 600;
  font-size: 16px;
  color: #333;
}

.card-body {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.info-row {
  display: flex;
  align-items: center;
  gap: 8px;
  font-size: 14px;
}

.label {
  color: #909399;
  min-width: 70px;
}

.value {
  color: #303133;
}

.text-truncate {
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
  max-width: 250px;
}

.masked {
  font-family: monospace;
  color: #909399;
}

.card-actions {
  display: flex;
  align-items: center;
  gap: 8px;
}

.right-actions {
  margin-left: auto;
  display: flex;
  gap: 8px;
}

:deep(.el-card__header) {
  padding: 16px;
  border-bottom: 1px solid #f0f0f0;
}

:deep(.el-card__body) {
  padding: 16px;
}

:deep(.el-card__footer) {
  padding: 12px 16px;
  border-top: 1px solid #f0f0f0;
  background-color: #fafafa;
}
</style>
