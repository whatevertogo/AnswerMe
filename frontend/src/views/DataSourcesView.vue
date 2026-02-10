<script setup lang="ts">
import { ref, onMounted, computed } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage, ElMessageBox } from 'element-plus'
import { Plus, Search, Connection, CircleCheck, Star, Edit, Delete } from '@element-plus/icons-vue'
import { useDataSourceStore } from '@/stores/dataSource'
import type { DataSource } from '@/api/datasource'
import { getProviderLabel, getProviderTagType } from '@/constants/aiProviders'

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
  return getProviderLabel(type)
}

const getTypeTagType = (type: string) => {
  return getProviderTagType(type)
}
</script>

<template>
  <div class="datasources-view">
    <div class="header">
      <h1>AI配置管理</h1>
      <el-button type="primary" :icon="Plus" @click="handleAdd">
        添加配置
      </el-button>
    </div>

    <el-card class="search-card">
      <el-input
        v-model="searchQuery"
        placeholder="搜索数据源名称或类型..."
        :prefix-icon="Search"
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
  @apply p-6 max-w-[1400px] mx-auto;
}

.header {
  @apply flex justify-between items-center mb-6;
}

.header h1 {
  @apply m-0 text-[24px] font-semibold;
  color: #333;
}

.search-card {
  @apply mb-6;
}

.loading-container,
.empty-container {
  @apply py-10;
}

.datasources-grid {
  @apply grid gap-5;
  grid-template-columns: repeat(auto-fill, minmax(380px, 1fr));
}

.datasource-card {
  @apply rounded-lg;
}

.card-header {
  @apply flex justify-between items-center;
}

.header-left {
  @apply flex items-center gap-2.5;
}

.type-icon {
  @apply text-[#409eff];
}

.name {
  @apply font-semibold text-base;
  color: #333;
}

.card-body {
  @apply flex flex-col gap-3;
}

.info-row {
  @apply flex items-center gap-2 text-sm;
}

.label {
  @apply text-[#909399] min-w-[70px];
}

.value {
  @apply text-[#303133];
}

.text-truncate {
  @apply overflow-hidden text-ellipsis whitespace-nowrap max-w-[250px];
}

.masked {
  @apply font-mono text-[#909399];
}

.card-actions {
  @apply flex items-center gap-2;
}

.right-actions {
  @apply ml-auto flex gap-2;
}

:deep(.el-card__header) {
  @apply px-4 py-4 border-b border-[#f0f0f0];
}

:deep(.el-card__body) {
  @apply px-4 py-4;
}

:deep(.el-card__footer) {
  @apply px-4 py-3 border-t border-[#f0f0f0] bg-[#fafafa];
}
</style>
