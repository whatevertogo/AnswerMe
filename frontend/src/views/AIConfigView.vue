<template>
  <div class="placeholder-view">
    <el-result icon="info" title="AI配置页面" sub-title="功能开发中...">
      <template #extra>
        <el-button type="primary" @click="$router.back">返回</el-button>
      </template>
    </el-result>
  </div>
</template>

<style scoped>
.placeholder-view {
  background-color: #ffffff;
  border-radius: 8px;
  padding: 20px;
}
</style>
