<script setup lang="ts">
import { Reading, Star, Collection, Trophy } from '@element-plus/icons-vue'
</script>

<template>
  <div class="auth-layout">
    <!-- 左侧品牌卡片 -->
    <div class="brand-card">
      <div class="brand-content">
        <div class="brand-header">
          <div class="logo-icon">
            <el-icon :size="32" color="var(--color-white)"><Reading /></el-icon>
          </div>
          <h1 class="brand-title">AnswerMe</h1>
          <p class="brand-subtitle">智能题库系统</p>
        </div>

        <div class="brand-description">
          <p>创建属于你自己的题库，使用 AI 生成高质量题目，让学习更高效</p>
        </div>

        <div class="feature-grid">
          <div class="feature-box">
            <div class="feature-icon">
              <el-icon :size="24" color="var(--color-primary)"><Star /></el-icon>
            </div>
            <h3>AI 智能生成</h3>
            <p>基于大语言模型自动生成高质量题目</p>
          </div>
          <div class="feature-box">
            <div class="feature-icon">
              <el-icon :size="24" color="var(--color-primary)"><Collection /></el-icon>
            </div>
            <h3>题库管理</h3>
            <p>多维度分类管理，支持导入导出</p>
          </div>
          <div class="feature-box">
            <div class="feature-icon">
              <el-icon :size="24" color="var(--color-primary)"><Trophy /></el-icon>
            </div>
            <h3>智能练习</h3>
            <p>个性化推荐，针对性提升</p>
          </div>
        </div>

        <div class="brand-footer">
          <p>© 2024 AnswerMe. All rights reserved.</p>
        </div>
      </div>
    </div>

    <!-- 右侧登录卡片 -->
    <div class="form-card">
      <div class="form-content">
        <router-view v-slot="{ Component, route }">
          <transition :name="(route.meta?.transition as string) || 'fade'" mode="out-in">
            <component :is="Component" :key="route.path" />
          </transition>
        </router-view>
      </div>
    </div>
  </div>
</template>

<style scoped>
/*
 * AuthLayout 采用全屏双栏布局
 * 使用 vw/vh 单位确保响应式
 */
.auth-layout {
  @apply w-screen h-screen flex bg-bg;
}

/* ─── 左侧品牌区域 ─── */
.brand-card {
  @apply w-[50vw] min-w-[320px]
         flex flex-col justify-center items-center
         py-[3vh] px-[3vw] overflow-y-auto
         border-r border-border;
  background: linear-gradient(135deg, var(--color-bg-secondary) 0%, var(--color-bg-tertiary) 100%);
}

@keyframes slideUp {
  from {
    opacity: 0;
    transform: translateY(30px);
  }
  to {
    opacity: 1;
    transform: translateY(0);
  }
}

.brand-header {
  @apply text-center mb-[4vh];
}

.logo-icon {
  @apply w-16 h-16 rounded-lg
         flex items-center justify-center
         mx-auto mb-[2vh] shadow-md
         transition-all duration-300 ease-in-out
         hover:-translate-y-0.5 hover:shadow-lg;
  background: var(--color-primary-gradient);
}

.brand-title {
  @apply text-[clamp(1.5rem,3vw,2.2rem)] font-bold m-0 mb-[1vh];
  background: var(--color-primary-gradient);
  -webkit-background-clip: text;
  -webkit-text-fill-color: transparent;
  background-clip: text;
  letter-spacing: -0.02em;
}

.brand-subtitle {
  @apply text-[clamp(0.875rem,1.2vw,1.1rem)] m-0 font-medium;
  color: var(--color-text-secondary);
}

.brand-description {
  @apply text-center mb-[5vh] px-[1vw];
}

.brand-description p {
  @apply text-[clamp(0.875rem,1.1vw,1rem)] leading-[1.7] m-0 text-text-secondary;
}

.feature-grid {
  @apply grid grid-cols-3 gap-[1vw] mb-[6vh];
}

.feature-box {
  @apply bg-white rounded-lg py-[2vh] px-[1vw]
         text-center transition-all duration-300 ease-in-out
         border border-border min-h-[180px]
         flex flex-col justify-center shadow-sm;
  background: linear-gradient(135deg, var(--color-white) 0%, var(--color-bg-secondary) 100%);
}

.feature-box:hover {
  @apply -translate-y-1 shadow-lg border-primary;
  box-shadow: 0 8px 30px rgba(35, 131, 228, 0.1);
}

.feature-box .feature-icon {
  @apply mb-[1vh] flex justify-center items-center;
}

.feature-box h3 {
  @apply text-[clamp(0.75rem,1vw,0.875rem)] font-semibold
         m-0 mb-[0.5vh];
  color: var(--color-text-primary);
}

.feature-box p {
  @apply text-[clamp(0.6875rem,0.9vw,0.75rem)] m-0 leading-[1.4];
  color: var(--color-text-secondary);
}

.brand-footer {
  @apply text-center pt-[4vh] mt-[4vh] border-t border-border;
}

.brand-footer p {
  @apply text-[0.8125rem] text-text-muted m-0;
}

/* ─── 右侧表单区域 ─── */
.form-card {
  @apply flex-1 min-w-[300px] bg-bg
         flex flex-col justify-center items-center
         py-[3vh] px-[3vw] overflow-y-auto;
}

.form-content {
  @apply w-[90%] max-w-[360px] mx-auto;
}

/* ─── 过渡动画 ─── */
.fade-enter-active,
.fade-leave-active {
  @apply transition-all duration-300 ease-in-out;
}

.fade-enter-from {
  @apply opacity-0 translate-x-2.5;
}

.fade-leave-to {
  @apply opacity-0 -translate-x-2.5;
}

/* ─── 响应式 - 平板 (宽度 < 1024px) ─── */
@media (max-width: 1024px) {
  .brand-card {
    @apply w-[50vw] py-[2vh] px-[2vw];
  }

  .brand-content {
    @apply py-[3vh] px-[2vw];
  }

  .feature-grid {
    @apply grid-cols-1 gap-[1vh];
  }

  .feature-box {
    @apply flex items-center gap-3 text-left py-[1.5vh] px-[2vw];
  }

  .feature-box .feature-icon {
    @apply mb-0;
  }

  .form-card {
    @apply py-[2vh] px-[2vw];
  }
}

/* ─── 响应式 - 小平板 (宽度 < 900px) ─── */
@media (max-width: 900px) {
  .auth-layout {
    @apply flex-col overflow-y-auto h-auto min-h-screen;
  }

  .brand-card {
    @apply w-screen max-w-none py-[4vh] px-[5vw]
           border-r-0 border-b border-border;
  }

  .brand-content {
    @apply max-w-[600px] py-[4vh] px-[4vw];
  }

  .feature-grid {
    @apply grid-cols-3 gap-[2vw];
  }

  .feature-box {
    @apply flex-col text-center py-[2vh] px-[2vw];
  }

  .feature-box .feature-icon {
    @apply mb-[1vh];
  }

  .form-card {
    @apply w-screen min-h-[50vh] py-[4vh] px-[5vw];
  }

  .form-content {
    @apply max-w-[400px];
  }
}

/* ─── 响应式 - 手机 (宽度 < 640px) ─── */
@media (max-width: 640px) {
  .brand-card {
    @apply hidden;
  }

  .form-card {
    @apply w-screen min-h-screen py-[3vh] px-[5vw];
  }

  .form-content {
    @apply max-w-full;
  }
}

/* ─── 超小屏幕 ─── */
@media (max-width: 360px) {
  .form-card {
    @apply py-[2vh] px-[4vw];
  }
}

/* ─── 超宽屏幕优化 ─── */
@media (min-width: 1920px) {
  .brand-card {
    @apply w-[50vw];
  }

  .brand-content {
    @apply max-w-[560px];
  }

  .form-content {
    @apply max-w-[420px];
  }
}

/* ─── 超高屏幕优化 ─── */
@media (min-height: 1200px) {
  .brand-content {
    @apply py-[5vh] px-[3vw];
  }

  .form-card {
    @apply py-[5vh] px-[3vw];
  }
}

/* ─── 矮屏幕优化 ─── */
@media (max-height: 700px) {
  .brand-content {
    @apply py-[2vh] px-[2vw];
  }

  .brand-header {
    @apply mb-[2vh];
  }

  .brand-description {
    @apply mb-[2vh];
  }

  .feature-grid {
    @apply mb-[2vh];
  }

  .logo-icon {
    @apply w-[52px] h-[52px];
  }
}
</style>
