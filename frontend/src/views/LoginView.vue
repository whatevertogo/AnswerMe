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
  @apply text-[1.8rem] font-bold text-text-primary m-0 mb-2;
}

.form-subtitle {
  @apply text-base text-text-secondary m-0;
}

.login-form {
  @apply flex flex-col;
}

.form-item {
  @apply mb-5;
}

.form-item :deep(.el-form-item__label) {
  @apply font-medium text-text-primary pb-2;
}

.custom-input :deep(.el-input__wrapper) {
  @apply rounded-md shadow-xs transition-all duration-400 ease-smooth;
}

.custom-input :deep(.el-input__wrapper:hover) {
  @apply shadow-sm;
}

.custom-input :deep(.el-input__wrapper.is-focus) {
  box-shadow: 0 0 0 3px rgba(59, 130, 246, 0.1);
}

.options-row {
  @apply flex justify-between items-center mb-6 text-sm;
}

.login-button {
  @apply w-full h-12 text-base font-semibold rounded-md mb-5;
}

.form-footer {
  @apply text-center;
}

.footer-text {
  @apply text-sm text-text-secondary;
}

.footer-link {
  @apply text-sm text-primary font-semibold ml-1;
}

.local-login-wrapper {
  @apply text-center mt-3;
}

.local-login-text {
  @apply text-sm text-primary font-semibold italic cursor-pointer transition-all duration-400 ease-smooth;
}

.local-login-text:hover {
  @apply text-primary-hover;
}
</style>
