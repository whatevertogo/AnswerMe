<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { User, Lock } from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores/auth'
import { authApi } from '@/api/auth'

const router = useRouter()
const authStore = useAuthStore()

const loginForm = ref({
  email: '',
  password: ''
})

const loading = ref(false)

const handleLogin = async () => {
  if (!loginForm.value.email || !loginForm.value.password) {
    ElMessage.warning('请输入邮箱和密码')
    return
  }

  loading.value = true
  try {
    const response = await authApi.login(loginForm.value)
    authStore.setAuth(response.token, response.user)
    ElMessage.success('登录成功')
    router.push('/home')
  } catch (error: any) {
    const message = error.response?.data?.message || '登录失败，请检查邮箱和密码'
    ElMessage.error(message)
  } finally {
    loading.value = false
  }
}

const handleLocalLogin = async () => {
  loading.value = true
  try {
    const response = await authApi.localLogin()
    authStore.setAuth(response.token, response.user)
    ElMessage.success('本地登录成功')
    router.push('/home')
  } catch (error: any) {
    const message = error.response?.data?.message || '本地登录失败'
    ElMessage.error(message)
  } finally {
    loading.value = false
  }
}
</script>

<template>
  <div class="login-content">
    <div class="form-header">
      <h2 class="form-title">欢迎回来</h2>
      <p class="form-subtitle">登录到你的账户</p>
    </div>

    <el-form :model="loginForm" class="login-form" label-position="top">
      <el-form-item label="邮箱" class="form-item">
        <el-input v-model="loginForm.email" size="large" placeholder="请输入邮箱" :prefix-icon="User" class="custom-input"
          @keyup.enter="handleLogin" />
      </el-form-item>

      <el-form-item label="密码" class="form-item">
        <el-input v-model="loginForm.password" type="password" size="large" placeholder="请输入密码" :prefix-icon="Lock"
          show-password class="custom-input" @keyup.enter="handleLogin" />
      </el-form-item>

      <div class="options-row">
        <el-checkbox>记住我</el-checkbox>
      </div>

      <el-button type="primary" size="large" :loading="loading" class="login-button" @click="handleLogin">
        登录
      </el-button>

      <div class="form-footer">
        <span class="footer-text">还没有账号？</span>
        <router-link to="/register" class="footer-link">立即注册</router-link>
      </div>

      <div class="local-login-wrapper">
        <span class="local-login-text" @click="handleLocalLogin">本地登录</span>
      </div>
    </el-form>
  </div>
</template>

<style scoped>
.login-content {
  @apply w-full max-w-[360px];
  animation: fadeIn 0.5s ease-out;
}

@keyframes fadeIn {
  from {
    opacity: 0;
    transform: translateX(20px);
  }

  to {
    opacity: 1;
    transform: translateX(0);
  }
}

.form-header {
  @apply text-center mb-8;
}

.form-title {
  @apply text-[1.8rem] font-bold m-0 mb-2;
  color: var(--color-primary);
  font-family: 'Noto Serif SC', 'Songti SC', serif;
}

.form-subtitle {
  @apply text-base m-0;
  color: var(--color-text-secondary);
}

.login-form {
  @apply flex flex-col;
}

.form-item {
  @apply mb-5;
}

.form-item :deep(.el-form-item__label) {
  @apply font-medium pb-2;
  color: var(--color-text-primary);
  font-family: 'Noto Serif SC', 'Songti SC', serif;
}

.custom-input :deep(.el-input__wrapper) {
  @apply shadow-xs transition-all duration-200 ease-out;
  border-radius: var(--radius-sm);
  background-color: var(--color-white);
  box-shadow: 0 0 0 1px var(--color-border) inset;
}

.custom-input :deep(.el-input__wrapper:hover) {
  border-color: var(--color-primary-light);
  box-shadow: 0 0 0 1px var(--color-primary-light), var(--shadow-xs) inset;
}

.custom-input :deep(.el-input__wrapper.is-focus) {
  border-color: var(--color-primary);
  box-shadow: 0 0 0 3px var(--color-primary-light), 0 0 0 1px var(--color-primary) inset;
}

.options-row {
  @apply flex justify-between items-center mb-6 text-sm;
}

.login-button {
  @apply w-full h-12 text-base font-semibold mb-5;
  background: var(--color-primary-gradient);
  border: none;
  border-radius: var(--radius-sm);
}

.login-button:hover {
  opacity: 0.9;
  box-shadow: var(--shadow-sm);
}

.form-footer {
  @apply text-center;
}

.footer-text {
  @apply text-sm;
  color: var(--color-text-secondary);
}

.footer-link {
  @apply text-sm font-semibold ml-1;
  color: var(--color-primary);
  transition: color var(--transition-fast);
}

.footer-link:hover {
  color: var(--color-primary-hover);
}

.local-login-wrapper {
  @apply text-center mt-3;
}

.local-login-text {
  @apply text-sm font-semibold italic cursor-pointer transition-all duration-200 ease-out;
  color: var(--color-primary);
}

.local-login-text:hover {
  color: var(--color-primary-hover);
  text-decoration: underline;
  text-decoration-color: var(--color-primary-light);
}
</style>
