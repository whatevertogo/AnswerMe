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
    authStore.setToken(response.data.token)
    authStore.setUser(response.data.user)
    ElMessage.success('登录成功')
    router.push('/home')
  } catch (error: any) {
    const message = error.response?.data?.message || '登录失败，请检查邮箱和密码'
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
        <router-link to="/forgot-password" class="forgot-link">忘记密码？</router-link>
      </div>

      <el-button type="primary" size="large" :loading="loading" class="login-button" @click="handleLogin">
        登录
      </el-button>

      <div class="form-footer">
        <span class="footer-text">还没有账号？</span>
        <router-link to="/register" class="footer-link">立即注册</router-link>
      </div>
    </el-form>
  </div>
</template>

<style scoped>
.login-content {
  width: 100%;
  max-width: 360px;
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
  text-align: center;
  margin-bottom: 32px;
}

.form-title {
  font-size: 1.8rem;
  font-weight: 1000;
  color: #073642;
  margin: 0 0 8px 0;
}

.form-subtitle {
  font-size: 1rem;
  color: #586E75;
  margin: 0;
}

.login-form {
  display: flex;
  flex-direction: column;
}

.form-item {
  margin-bottom: 20px;
}

.form-item :deep(.el-form-item__label) {
  font-weight: 500;
  color: #073642;
  padding-bottom: 8px;
}

.custom-input :deep(.el-input__wrapper) {
  border-radius: 10px;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.04);
  transition: all 0.3s ease;
}

.custom-input :deep(.el-input__wrapper:hover) {
  box-shadow: 0 4px 12px rgba(0, 0, 0, 0.08);
}

.custom-input :deep(.el-input__wrapper.is-focus) {
  box-shadow: 0 4px 16px rgba(38, 139, 210, 0.15);
}

.options-row {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 24px;
  font-size: 0.875rem;
}

.forgot-link {
  color: #268BD2;
  text-decoration: none;
  transition: color 0.3s ease;
}

.forgot-link:hover {
  color: #2AA198;
}

.login-button {
  width: 100%;
  height: 48px;
  font-size: 1rem;
  font-weight: 600;
  border-radius: 10px;
  background: linear-gradient(135deg, #268BD2 0%, #2AA198 100%);
  border: none;
  transition: all 0.3s ease;
  margin-bottom: 20px;
}

.login-button:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 20px rgba(38, 139, 210, 0.3);
}

.form-footer {
  text-align: center;
}

.footer-text {
  font-size: 0.875rem;
  color: #586E75;
}

.footer-link {
  font-size: 0.875rem;
  color: #268BD2;
  font-weight: 600;
  margin-left: 4px;
  text-decoration: none;
  transition: all 0.3s ease;
}

.footer-link:hover {
  color: #2AA198;
}
</style>
