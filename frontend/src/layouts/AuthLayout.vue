<script setup lang="ts">
</script>

<template>
  <div class="auth-layout">
    <!-- 左侧品牌卡片 -->
    <div class="brand-card">
      <div class="brand-content">
        <div class="brand-header">
          <div class="logo-icon">
            <span class="logo-emoji">🎓</span>
          </div>
          <h1 class="brand-title">AnswerMe</h1>
          <p class="brand-subtitle">智能题库系统</p>
        </div>

        <div class="brand-description">
          <p>创建属于你自己的题库，使用 AI 生成高质量题目，让学习更高效</p>
        </div>

        <div class="feature-grid">
          <div class="feature-box">
            <span class="feature-icon">✨</span>
            <h3>AI 智能生成</h3>
            <p>基于大语言模型自动生成高质量题目</p>
          </div>
          <div class="feature-box">
            <span class="feature-icon">📚</span>
            <h3>题库管理</h3>
            <p>多维度分类管理，支持导入导出</p>
          </div>
          <div class="feature-box">
            <span class="feature-icon">🎯</span>
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
        <router-view v-slot="{ Component }">
          <transition name="fade" mode="out-in">
            <component :is="Component" />
          </transition>
        </router-view>
      </div>
    </div>
  </div>
</template>

<style scoped>
/*
 * AuthLayout 采用全屏双栏布局，
 * 需要跳出 #app 的 max-width / padding 约束。
 */
.auth-layout {
  /* 跳出 #app 约束，占满整个视口 */
  position: fixed;
  inset: 0;
  display: flex;
  background: #FDF6E3;
  overflow: hidden;
}

/* ─── 左侧品牌区域 - 55% ─── */
.brand-card {
  flex: 0 0 55%;
  background: #EEE8D5;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 48px;
  overflow-y: auto;
}

.brand-content {
  width: 100%;
  max-width: 520px;
  background: #FFFFFF;
  border-radius: 24px;
  padding: 48px;
  box-shadow: 0 20px 60px rgba(0, 0, 0, 0.1);
  animation: slideUp 0.6s ease-out;
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
  text-align: center;
  margin-bottom: 32px;
}

.logo-icon {
  width: 80px;
  height: 80px;
  background: linear-gradient(135deg, #268BD2 0%, #2AA198 100%);
  border-radius: 20px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 20px;
  animation: pulse 2s ease-in-out infinite;
}

@keyframes pulse {
  0%, 100% {
    box-shadow: 0 8px 30px rgba(38, 139, 210, 0.3);
  }
  50% {
    box-shadow: 0 8px 40px rgba(38, 139, 210, 0.5);
  }
}

.logo-emoji {
  font-size: 40px;
  filter: grayscale(0.3);
}

.brand-title {
  font-size: 2.5rem;
  font-weight: 800;
  margin: 0 0 8px;
  color: #073642;
  letter-spacing: -0.02em;
}

.brand-subtitle {
  font-size: 1.125rem;
  margin: 0;
  color: #586E75;
  font-weight: 500;
}

.brand-description {
  text-align: center;
  margin-bottom: 32px;
  padding: 0 20px;
}

.brand-description p {
  font-size: 1rem;
  line-height: 1.7;
  margin: 0;
  color: #657B83;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  margin-bottom: 32px;
}

.feature-box {
  background: linear-gradient(135deg, #FDF6E3 0%, #F5F0E6 100%);
  border-radius: 16px;
  padding: 20px 12px;
  text-align: center;
  transition: all 0.3s ease;
  border: 1px solid rgba(238, 232, 213, 0.8);
}

.feature-box:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.08);
  background: linear-gradient(135deg, #FFFFFF 0%, #FDF6E3 100%);
}

.feature-box .feature-icon {
  font-size: 28px;
  margin-bottom: 10px;
  display: block;
}

.feature-box h3 {
  font-size: 0.875rem;
  font-weight: 600;
  margin: 0 0 6px;
  color: #073642;
}

.feature-box p {
  font-size: 0.75rem;
  margin: 0;
  color: #657B83;
  line-height: 1.4;
}

.brand-footer {
  text-align: center;
  padding-top: 16px;
  border-top: 1px solid #EEE8D5;
}

.brand-footer p {
  font-size: 0.8125rem;
  color: #839496;
  margin: 0;
}

/* ─── 右侧表单区域 - 45% ─── */
.form-card {
  flex: 1;
  background: #FDF6E3;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 48px;
  overflow-y: auto;
}

.form-content {
  width: 100%;
  max-width: 360px;
  margin: 0 auto;
}

/* ─── 过渡动画 ─── */
.fade-enter-active,
.fade-leave-active {
  transition: opacity 0.25s ease, transform 0.25s ease;
}

.fade-enter-from {
  opacity: 0;
  transform: translateX(10px);
}

.fade-leave-to {
  opacity: 0;
  transform: translateX(-10px);
}

/* ─── 响应式 - 平板 ─── */
@media (max-width: 1200px) {
  .brand-card {
    flex: 0 0 50%;
    padding: 32px;
  }

  .brand-content {
    padding: 36px;
  }

  .brand-title {
    font-size: 2rem;
  }

  .feature-grid {
    grid-template-columns: 1fr;
    gap: 12px;
  }

  .feature-box {
    display: flex;
    align-items: center;
    gap: 16px;
    text-align: left;
    padding: 16px;
  }

  .feature-box .feature-icon {
    margin-bottom: 0;
    font-size: 24px;
  }

  .form-card {
    padding: 32px;
  }
}

/* ─── 响应式 - 手机 ─── */
@media (max-width: 768px) {
  .auth-layout {
    flex-direction: column;
    position: relative;
    overflow: auto;
  }

  .brand-card {
    display: none;
  }

  .form-card {
    flex: 1;
    width: 100%;
    padding: 24px;
    min-height: 100vh;
  }

  .form-content {
    max-width: 100%;
  }
}
</style>
