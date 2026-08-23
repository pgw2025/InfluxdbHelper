<template>
  <el-row :gutter="16" v-loading="loading">
    <el-col :span="14">
      <el-card>
        <template #header>
          <div class="card-header">
            <span>InfluxDB 连接配置</span>
            <el-button size="small" @click="onTest">测试连接</el-button>
          </div>
        </template>

        <el-form :model="form" label-width="90px">
          <el-form-item label="服务地址">
            <el-input v-model="form.url" placeholder="http://localhost:8086" />
          </el-form-item>
          <el-form-item label="Token">
            <el-input
              v-model="form.token"
              placeholder="留空表示沿用当前配置（已脱敏显示）"
              clearable
            />
          </el-form-item>
          <el-form-item label="组织 Org">
            <el-input v-model="form.org" />
          </el-form-item>
          <el-form-item label="Bucket">
            <el-input v-model="form.bucket" />
          </el-form-item>
        </el-form>
      </el-card>
    </el-col>

    <el-col :span="10">
      <el-card>
        <template #header>钉钉推送配置</template>
        <el-form :model="form" label-width="90px">
          <el-form-item label="启用推送">
            <el-switch v-model="form.dingTalkEnabled" />
          </el-form-item>
          <el-form-item label="Webhook">
            <el-input v-model="form.dingTalkWebhookUrl" placeholder="https://oapi.dingtalk.com/robot/send?..." />
          </el-form-item>
          <el-form-item label="加签密钥">
            <el-input v-model="form.dingTalkSecret" placeholder="留空表示沿用当前配置" clearable />
          </el-form-item>
          <el-form-item label="推送时间">
            <el-time-picker
              v-model="sendTime"
              format="HH:mm"
              placeholder="选择时间"
              :clearable="false"
            />
          </el-form-item>
          <el-form-item label="消息模板">
            <el-input
              v-model="form.dingTalkMessageTemplate"
              type="textarea"
              :rows="6"
              placeholder="支持占位符：{{date}} {{total_count}} {{start_time}} {{end_time}} {{variable_stats}}"
            />
          </el-form-item>
        </el-form>
      </el-card>
    </el-col>

    <el-col :span="24">
      <div class="actions">
        <el-checkbox v-model="persist">写入配置文件（appsettings.json）</el-checkbox>
        <el-button type="primary" :loading="saving" @click="onSave">保存配置</el-button>
      </div>
    </el-col>
  </el-row>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { ElMessage } from 'element-plus'
import {
  getConfig,
  saveConfig,
  testConnection,
  type AppConfig
} from '@/api/config'

const loading = ref(false)
const saving = ref(false)
const persist = ref(true)

const form = ref<AppConfig>({
  url: '',
  token: '',
  org: '',
  bucket: '',
  dingTalkWebhookUrl: '',
  dingTalkSecret: '',
  dingTalkEnabled: false,
  dingTalkSendHour: 9,
  dingTalkSendMinute: 0,
  dingTalkMessageTemplate: ''
})

const sendTime = ref(new Date(2026, 0, 1, 9, 0))

const sendTimeValid = computed(() => sendTime.value instanceof Date && !isNaN(sendTime.value.getTime()))

onMounted(async () => {
  loading.value = true
  try {
    form.value = await getConfig()
    sendTime.value = new Date(2026, 0, 1, form.value.dingTalkSendHour, form.value.dingTalkSendMinute)
  } finally {
    loading.value = false
  }
})

async function onTest() {
  const res = await testConnection({ url: form.value.url, token: form.value.token })
  if (res.connectionOk) {
    ElMessage.success('连接成功')
  } else {
    ElMessage.error('连接失败：InfluxDB 无响应')
  }
}

async function onSave() {
  saving.value = true
  try {
    if (sendTimeValid.value) {
      form.value.dingTalkSendHour = sendTime.value.getHours()
      form.value.dingTalkSendMinute = sendTime.value.getMinutes()
    }
    const res = await saveConfig({ ...form.value, persist: persist.value })
    if (res.connectionOk) {
      ElMessage.success('配置已保存，连接测试成功')
    } else {
      ElMessage.warning(res.error || '配置已保存，但连接测试失败')
    }
    // 重新拉取脱敏后的配置
    form.value = await getConfig()
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.card-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.actions {
  display: flex;
  align-items: center;
  justify-content: flex-end;
  gap: 20px;
  margin-top: 16px;
}
</style>
