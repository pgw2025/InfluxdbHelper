<template>
  <div class="login-page">
    <div class="login-box">
      <div class="brand-header">
        <div class="brand-logo">
          <el-icon :size="24"><DataLine /></el-icon>
        </div>
        <h1 class="brand-title">InfluxDB 助手</h1>
        <p class="brand-subtitle">工业时序数据运维与监控中心</p>
      </div>

      <el-card class="login-card" :body-style="{ padding: '28px' }">
        <el-form
          ref="formRef"
          :model="form"
          :rules="rules"
          size="large"
          @keyup.enter="onSubmit"
        >
          <el-form-item prop="username">
            <el-input
              v-model="form.username"
              placeholder="账号 / 用户名"
              :prefix-icon="User"
              autofocus
            />
          </el-form-item>

          <el-form-item prop="password">
            <el-input
              v-model="form.password"
              type="password"
              placeholder="登录密码"
              show-password
              :prefix-icon="Lock"
            />
          </el-form-item>

          <el-form-item class="submit-item">
            <el-button type="primary" class="login-btn" :loading="loading" @click="onSubmit">
              进入工作台
            </el-button>
          </el-form-item>

          <div class="login-hint-box">
            <div class="hint-title">演示账号</div>
            <div class="hint-creds font-mono">
              admin / admin123
            </div>
          </div>
        </el-form>
      </el-card>

      <div class="login-footer">
        <span>© 2026 InfluxDB Helper · 时序数据监控系统</span>
      </div>
    </div>
  </div>
</template>

<script setup lang="ts">
import { reactive, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import { User, Lock, DataLine } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import type { FormInstance, FormRules } from 'element-plus'
import { useAuthStore } from '@/stores/auth'

const router = useRouter()
const route = useRoute()
const auth = useAuthStore()

const formRef = ref<FormInstance>()
const loading = ref(false)
const form = reactive({ username: 'admin', password: 'admin123' })

const rules: FormRules = {
  username: [{ required: true, message: '请输入用户名', trigger: 'blur' }],
  password: [{ required: true, message: '请输入密码', trigger: 'blur' }]
}

async function onSubmit() {
  await formRef.value?.validate()
  loading.value = true
  try {
    await auth.doLogin(form.username, form.password)
    ElMessage.success('登录成功')
    router.push((route.query.redirect as string) || '/')
  } finally {
    loading.value = false
  }
}
</script>

<style scoped>
.login-page {
  min-height: 100vh;
  display: flex;
  align-items: center;
  justify-content: center;
  background-color: #0f172a;
  background-image: 
    radial-gradient(at 0% 0%, rgba(37, 99, 235, 0.15) 0px, transparent 50%),
    radial-gradient(at 100% 100%, rgba(79, 70, 229, 0.12) 0px, transparent 50%);
  padding: 20px;
}

.login-box {
  width: 100%;
  max-width: 400px;
  display: flex;
  flex-direction: column;
  align-items: center;
}

.brand-header {
  display: flex;
  flex-direction: column;
  align-items: center;
  margin-bottom: 24px;
  text-align: center;
}

.brand-logo {
  width: 48px;
  height: 48px;
  background: #2563eb;
  border-radius: 12px;
  display: flex;
  align-items: center;
  justify-content: center;
  color: #ffffff;
  margin-bottom: 12px;
  box-shadow: 0 4px 14px rgba(37, 99, 235, 0.35);
}

.brand-title {
  margin: 0;
  font-size: 22px;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: -0.5px;
}

.brand-subtitle {
  margin: 4px 0 0;
  font-size: 13px;
  color: #94a3b8;
}

.login-card {
  width: 100%;
  border-radius: 14px;
  border: 1px solid rgba(255, 255, 255, 0.1);
  background: #ffffff;
  box-shadow: 0 20px 25px -5px rgba(0, 0, 0, 0.3), 0 8px 10px -6px rgba(0, 0, 0, 0.2);
}

.submit-item {
  margin-top: 8px;
  margin-bottom: 16px;
}

.login-btn {
  width: 100%;
  font-weight: 600;
  font-size: 15px;
  height: 44px;
  border-radius: 8px;
}

.login-hint-box {
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 10px 12px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 12px;
}

.hint-title {
  color: #64748b;
  font-weight: 500;
}

.hint-creds {
  color: #2563eb;
  font-weight: 600;
  background: #eff6ff;
  padding: 2px 8px;
  border-radius: 4px;
}

.login-footer {
  margin-top: 24px;
  font-size: 12px;
  color: #64748b;
  text-align: center;
}
</style>
