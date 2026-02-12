<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { User, Lock, Message, Reading } from '@element-plus/icons-vue'
import { authApi } from '@/api/auth'

const router = useRouter()

const registerForm = ref({
  username: '',
  email: '',
  password: '',
  confirmPassword: ''
})

const loading = ref(false)

const validateEmail = (email: string): boolean => {
  const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/
  return emailRegex.test(email)
}

const handleRegister = async () => {
  // 表单验证
  if (!registerForm.value.username || !registerForm.value.email || !registerForm.value.password) {
    ElMessage.warning('请填写所有必填项')
    return
  }

  if (!validateEmail(registerForm.value.email)) {
    ElMessage.warning('请输入有效的邮箱地址')
    return
  }

  if (registerForm.value.password.length < 6) {
    ElMessage.warning('密码长度至少为6位')
    return
  }

  if (registerForm.value.password !== registerForm.value.confirmPassword) {
    ElMessage.warning('两次输入的密码不一致')
    return
  }

  loading.value = true
  try {
    await authApi.register({
      username: registerForm.value.username,
      email: registerForm.value.email,
      password: registerForm.value.password
    })
    ElMessage.success('注册成功，请登录')
    router.push('/login')
  } catch (error: any) {
    const message = error.response?.data?.message || '注册失败，请稍后重试'
    ElMessage.error(message)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="register-container">
    <!-- 左侧品牌区域 -->
    <div class="brand-section">
      <div class="brand-content">
        <div class="brand-icon">
          <el-icon :size="48" color="var(--color-primary)"><Reading /></el-icon>
        </div>
        <h1 class="brand-title">加入 AnswerMe</h1>
        <p class="brand-subtitle">开启智能学习之旅</p>
        <p class="brand-description">
          创建账户，建立你的专属题库，让 AI 帮助你生成高质量的学习内容
        </p>
        <div class="feature-list">
          <div class="feature-item">
            <el-icon :size="16" color="var(--color-primary)"><Reading /></el-icon>
            <span class="feature-text">智能题目生成</span>
          </div>
          <div class="feature-item">
            <el-icon :size="16" color="var(--color-primary)"><Reading /></el-icon>
            <span class="feature-text">多题型支持</span>
          </div>
          <div class="feature-item">
            <el-icon :size="16" color="var(--color-primary)"><Reading /></el-icon>
            <span class="feature-text">学习数据分析</span>
          </div>
        </div>
      </div>
    </div>

    <!-- 右侧注册表单 -->
    <div class="form-section">
      <div class="form-card">
        <div class="form-header">
          <h2 class="form-title">创建账户</h2>
          <p class="form-subtitle">开始你的学习之旅</p>
        </div>

        <el-form :model="registerForm" class="register-form" label-position="top">
          <el-form-item label="用户名">
            <el-input
              v-model="registerForm.username"
              size="large"
              placeholder="请输入用户名"
              :prefix-icon="User"
            />
          </el-form-item>

          <el-form-item label="邮箱">
            <el-input
              v-model="registerForm.email"
              size="large"
              placeholder="your@email.com"
              :prefix-icon="Message"
            />
          </el-form-item>

          <el-form-item label="密码">
            <el-input
              v-model="registerForm.password"
              type="password"
              size="large"
              placeholder="至少6位字符"
              :prefix-icon="Lock"
              show-password
            />
          </el-form-item>

          <el-form-item label="确认密码">
            <el-input
              v-model="registerForm.confirmPassword"
              type="password"
              size="large"
              placeholder="再次输入密码"
              :prefix-icon="Lock"
              show-password
              @keyup.enter="handleRegister"
            />
          </el-form-item>

          <el-button
            type="primary"
            size="large"
            :loading="loading"
            class="register-button"
            @click="handleRegister"
          >
            创建账户
          </el-button>

          <div class="form-footer">
            <span class="footer-text">已有账号？</span>
            <router-link to="/login" class="footer-link">立即登录</router-link>
          </div>
        </el-form>
      </div>
    </div>
  </div>
</template>

<style scoped>
.register-container {
  @apply min-h-screen grid grid-cols-[1fr_480px] bg-bg;
}

/* 品牌区域 */
.brand-section {
  @apply flex items-center justify-center p-12;
  background-color: var(--color-bg-secondary);
}

.brand-content {
  @apply text-center max-w-[480px];
}

.brand-icon {
  @apply mb-6;
}

.brand-title {
  @apply text-[3rem] font-bold text-text-primary m-0 mb-3;
  letter-spacing: -0.02em;
}

.brand-subtitle {
  @apply text-[1.125rem] text-text-secondary m-0 font-medium;
}

.brand-description {
  @apply text-[0.9375rem] text-text-secondary leading-[1.7] m-0 mb-8;
}

.feature-list {
  @apply flex flex-col gap-3 mt-8;
}

.feature-item {
  @apply flex items-center gap-3 text-left justify-center;
}

.feature-text {
  @apply text-[0.9375rem] text-text-primary font-medium;
}

/* 表单区域 */
.form-section {
  @apply flex items-center justify-center p-12 bg-bg;
  border-left: 1px solid var(--color-border);
}

.form-card {
  @apply w-full max-w-[400px];
}

.form-header {
  @apply text-center mb-8;
}

.form-title {
  @apply text-[1.75rem] font-bold text-text-primary m-0 mb-2;
}

.form-subtitle {
  @apply text-[0.9375rem] text-text-secondary m-0;
}

.register-form {
  @apply flex flex-col gap-5;
}

.register-form :deep(.el-form-item__label) {
  @apply font-medium text-text-primary;
}

.register-button {
  @apply w-full h-11 text-base mt-2;
}

.form-footer {
  @apply text-center pt-2;
}

.footer-text {
  @apply text-sm text-text-secondary;
}

.footer-link {
  @apply text-sm text-primary font-medium ml-1;
}

/* 响应式 */
@media (max-width: 1024px) {
  .register-container {
    @apply grid-cols-1;
  }

  .brand-section {
    @apply p-16 pt-16 pb-8 px-8;
  }

  .form-section {
    border-left: 0;
    border-top: 1px solid var(--color-border);
  }

  .feature-list {
    @apply flex-row flex-wrap justify-center;
  }

  .feature-item {
    @apply flex-col text-center gap-2;
  }
}

@media (max-width: 640px) {
  .brand-title {
    @apply text-[2.25rem];
  }

  .form-section {
    @apply p-8 px-6;
  }
}
</style>
