<script setup lang="ts">
import { ref } from 'vue'
import { useRouter } from 'vue-router'
import { ElMessage } from 'element-plus'
import { User, Lock, Message } from '@element-plus/icons-vue'
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
        <div class="brand-icon">🎓</div>
        <h1 class="brand-title">加入 AnswerMe</h1>
        <p class="brand-subtitle">开启智能学习之旅</p>
        <p class="brand-description">
          创建账户，建立你的专属题库，让 AI 帮助你生成高质量的学习内容
        </p>
        <div class="feature-list">
          <div class="feature-item">
            <div class="feature-icon">✓</div>
            <span class="feature-text">智能题目生成</span>
          </div>
          <div class="feature-item">
            <div class="feature-icon">✓</div>
            <span class="feature-text">多题型支持</span>
          </div>
          <div class="feature-item">
            <div class="feature-icon">✓</div>
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
  min-height: 100vh;
  display: grid;
  grid-template-columns: 1fr 480px;
  background: #FDF6E3;
}

/* 品牌区域 */
.brand-section {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  background: linear-gradient(135deg, #EEE8D5 0%, #FDF6E3 100%);
}

.brand-content {
  text-align: center;
  max-width: 480px;
}

.brand-icon {
  font-size: 5rem;
  line-height: 1;
  margin-bottom: 1.5rem;
}

.brand-title {
  font-size: 3rem;
  font-weight: 700;
  color: #073642;
  margin: 0 0 0.75rem 0;
  letter-spacing: -0.02em;
}

.brand-subtitle {
  font-size: 1.125rem;
  color: #586E75;
  margin: 0 0 1rem 0;
  font-weight: 500;
}

.brand-description {
  font-size: 0.9375rem;
  color: #657B83;
  line-height: 1.7;
  margin: 0 0 2rem 0;
}

.feature-list {
  display: flex;
  flex-direction: column;
  gap: 0.75rem;
  margin-top: 2rem;
}

.feature-item {
  display: flex;
  align-items: center;
  gap: 0.75rem;
  text-align: left;
}

.feature-icon {
  width: 24px;
  height: 24px;
  border-radius: 50%;
  background: #268BD2;
  color: #FFFFFF;
  display: flex;
  align-items: center;
  justify-content: center;
  font-size: 0.875rem;
  font-weight: 700;
  flex-shrink: 0;
}

.feature-text {
  font-size: 0.9375rem;
  color: #073642;
  font-weight: 500;
}

/* 表单区域 */
.form-section {
  display: flex;
  align-items: center;
  justify-content: center;
  padding: 3rem;
  background: #FFFFFF;
  border-left: 1px solid #E8E4CE;
}

.form-card {
  width: 100%;
  max-width: 400px;
}

.form-header {
  text-align: center;
  margin-bottom: 2rem;
}

.form-title {
  font-size: 1.75rem;
  font-weight: 700;
  color: #073642;
  margin: 0 0 0.5rem 0;
}

.form-subtitle {
  font-size: 0.9375rem;
  color: #586E75;
  margin: 0;
}

.register-form {
  display: flex;
  flex-direction: column;
  gap: 1.25rem;
}

.register-form :deep(.el-form-item__label) {
  font-weight: 500;
  color: #073642;
}

.register-button {
  width: 100%;
  height: 44px;
  font-size: 1rem;
  margin-top: 0.5rem;
}

.form-footer {
  text-align: center;
  padding-top: 0.5rem;
}

.footer-text {
  font-size: 0.875rem;
  color: #586E75;
}

.footer-link {
  font-size: 0.875rem;
  color: #268BD2;
  font-weight: 500;
  margin-left: 0.25rem;
}

.footer-link:hover {
  color: #2AA198;
}

/* 响应式 */
@media (max-width: 1024px) {
  .register-container {
    grid-template-columns: 1fr;
  }

  .brand-section {
    padding: 4rem 2rem 2rem;
  }

  .form-section {
    border-left: none;
    border-top: 1px solid #E8E4CE;
  }

  .feature-list {
    flex-direction: row;
    flex-wrap: wrap;
    justify-content: center;
  }

  .feature-item {
    flex-direction: column;
    text-align: center;
    gap: 0.5rem;
  }
}

@media (max-width: 640px) {
  .brand-icon {
    font-size: 4rem;
  }

  .brand-title {
    font-size: 2.25rem;
  }

  .form-section {
    padding: 2rem 1.5rem;
  }
}
</style>
