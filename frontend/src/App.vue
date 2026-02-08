<script setup lang="ts">
import { ElConfigProvider } from 'element-plus'
// @ts-ignore - Element Plus locale types not available
import zhCn from 'element-plus/dist/locale/zh-cn.mjs'
</script>

<template>
  <ElConfigProvider :locale="zhCn">
    <el-container class="app-container">
      <el-header class="app-header">
        <div class="header-content">
          <h1 class="app-title">AI题库系统</h1>
          <nav class="app-nav">
            <router-link to="/question-banks">题库</router-link>
            <router-link to="/ai-config">AI配置</router-link>
            <router-link to="/generate">生成题目</router-link>
          </nav>
        </div>
      </el-header>
      <el-main class="app-main">
        <router-view />
      </el-main>
    </el-container>
  </ElConfigProvider>
</template>

<style scoped>
.app-container {
  min-height: 100vh;
  background-color: #f5f7fa;
}

.app-header {
  background-color: #ffffff;
  box-shadow: 0 2px 4px rgba(0, 0, 0, 0.1);
  padding: 0;
}

.header-content {
  max-width: 1200px;
  margin: 0 auto;
  height: 100%;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 20px;
}

.app-title {
  font-size: 24px;
  font-weight: 600;
  color: #409eff;
  margin: 0;
}

.app-nav {
  display: flex;
  gap: 20px;
}

.app-nav a {
  color: #606266;
  text-decoration: none;
  font-size: 16px;
  transition: color 0.3s;
}

.app-nav a:hover,
.app-nav a.router-link-active {
  color: #409eff;
}

.app-main {
  max-width: 1200px;
  margin: 0 auto;
  padding: 20px;
}
</style>
