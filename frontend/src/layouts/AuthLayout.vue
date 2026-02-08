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
 * AuthLayout 采用全屏双栏布局
 * 使用 vw/vh 单位确保响应式
 */
.auth-layout {
  width: 100vw;
  height: 100vh;
  display: flex;
  background: #FDF6E3;
}

/* ─── 左侧品牌区域 ─── */
.brand-card {
  width: 50vw;
  min-width: 320px;
  background: #EEE8D5;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 3vh 3vw;
  overflow-y: auto;
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
  margin-bottom: 4vh;
}

.logo-icon {
  width: 70px;
  height: 70px;
  background: linear-gradient(135deg, #268BD2 0%, #2AA198 100%);
  border-radius: 16px;
  display: flex;
  align-items: center;
  justify-content: center;
  margin: 0 auto 2vh;
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
  font-size: 36px;
  filter: grayscale(0.3);
}

.brand-title {
  font-size: clamp(1.5rem, 3vw, 2.2rem);
  font-weight: 800;
  margin: 0 0 1vh;
  color: #073642;
  letter-spacing: -0.02em;
}

.brand-subtitle {
  font-size: clamp(0.875rem, 1.2vw, 1.1rem);
  margin: 0;
  color: #586E75;
  font-weight: 500;
}

.brand-description {
  text-align: center;
  margin-bottom: 5vh;
  padding: 0 1vw;
}

.brand-description p {
  font-size: clamp(0.875rem, 1.1vw, 1rem);
  line-height: 1.7;
  margin: 0;
  color: #657B83;
}

.feature-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 1vw;
  margin-bottom: 6vh;
}

.feature-box {
  background: linear-gradient(135deg, #FDF6E3 0%, #F5F0E6 100%);
  border-radius: 14px;
  padding: 5vh 1vw;
  text-align: center;
  transition: all 0.3s ease;
  border: 1px solid rgba(238, 232, 213, 0.8);
  min-height: 200px;
  display: flex;
  flex-direction: column;
  justify-content: center;
}

.feature-box:hover {
  transform: translateY(-4px);
  box-shadow: 0 12px 30px rgba(0, 0, 0, 0.08);
  background: linear-gradient(135deg, #FFFFFF 0%, #FDF6E3 100%);
}

.feature-box .feature-icon {
  font-size: 24px;
  margin-bottom: 1vh;
  display: block;
}

.feature-box h3 {
  font-size: clamp(0.75rem, 1vw, 0.875rem);
  font-weight: 600;
  margin: 0 0 0.5vh;
  color: #073642;
}

.feature-box p {
  font-size: clamp(0.6875rem, 0.9vw, 0.75rem);
  margin: 0;
  color: #657B83;
  line-height: 1.4;
}

.brand-footer {
  text-align: center;
  padding-top: 4vh;
  margin-top: 4vh;
  border-top: 1px solid #D3D7CF;
}

.brand-footer p {
  font-size: 0.8125rem;
  color: #839496;
  margin: 0;
}

/* ─── 右侧表单区域 ─── */
.form-card {
  flex: 1;
  min-width: 300px;
  background: #FDF6E3;
  display: flex;
  flex-direction: column;
  justify-content: center;
  align-items: center;
  padding: 3vh 3vw;
  overflow-y: auto;
}

.form-content {
  width: 90%;
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

/* ─── 响应式 - 平板 (宽度 < 1024px) ─── */
@media (max-width: 1024px) {
  .brand-card {
    width: 50vw;
    padding: 2vh 2vw;
  }

  .brand-content {
    padding: 3vh 2vw;
  }

  .feature-grid {
    grid-template-columns: 1fr;
    gap: 1vh;
  }

  .feature-box {
    display: flex;
    align-items: center;
    gap: 12px;
    text-align: left;
    padding: 1.5vh 2vw;
    min-height: auto;
  }

  .feature-box .feature-icon {
    margin-bottom: 0;
    font-size: 24px;
  }

  .form-card {
    padding: 2vh 2vw;
  }
}

/* ─── 响应式 - 小平板 (宽度 < 900px) ─── */
@media (max-width: 900px) {
  .auth-layout {
    flex-direction: column;
    overflow-y: auto;
    height: auto;
    min-height: 100vh;
  }

  .brand-card {
    width: 100vw;
    max-width: none;
    min-height: auto;
    padding: 4vh 5vw;
  }

  .brand-content {
    max-width: 600px;
    padding: 4vh 4vw;
  }

  .feature-grid {
    grid-template-columns: repeat(3, 1fr);
    gap: 2vw;
  }

  .feature-box {
    flex-direction: column;
    text-align: center;
    padding: 2vh 2vw;
    min-height: auto;
  }

  .feature-box .feature-icon {
    margin-bottom: 1vh;
  }

  .form-card {
    width: 100vw;
    min-height: 50vh;
    padding: 4vh 5vw;
  }

  .form-content {
    max-width: 400px;
  }
}

/* ─── 响应式 - 手机 (宽度 < 640px) ─── */
@media (max-width: 640px) {
  .brand-card {
    display: none;
  }

  .form-card {
    width: 100vw;
    min-height: 100vh;
    padding: 3vh 5vw;
  }

  .form-content {
    max-width: 100%;
  }
}

/* ─── 超小屏幕 ─── */
@media (max-width: 360px) {
  .form-card {
    padding: 2vh 4vw;
  }
}

/* ─── 超宽屏幕优化 ─── */
@media (min-width: 1920px) {
  .brand-card {
    width: 50vw;
  }

  .brand-content {
    max-width: 560px;
  }

  .form-content {
    max-width: 420px;
  }
}

/* ─── 超高屏幕优化 ─── */
@media (min-height: 1200px) {
  .brand-content {
    padding: 5vh 3vw;
  }

  .form-card {
    padding: 5vh 3vw;
  }
}

/* ─── 矮屏幕优化 ─── */
@media (max-height: 700px) {
  .brand-content {
    padding: 2vh 2vw;
  }

  .brand-header {
    margin-bottom: 2vh;
  }

  .brand-description {
    margin-bottom: 2vh;
  }

  .feature-grid {
    margin-bottom: 2vh;
  }

  .logo-icon {
    width: 50px;
    height: 50px;
  }

  .logo-emoji {
    font-size: 28px;
  }
}
</style>
