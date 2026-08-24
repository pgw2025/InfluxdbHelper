<template>
  <div class="config-page" v-loading="loading">
    <el-row :gutter="16">
      <!-- InfluxDB 配置卡片 -->
      <el-col :xs="24" :sm="24" :md="13">
        <el-card class="config-card">
          <template #header>
            <div class="card-header-flex">
              <div class="header-title">
                <el-icon class="header-icon primary"><Connection /></el-icon>
                <span>InfluxDB 连接配置</span>
              </div>
              <el-button size="small" type="primary" plain :icon="Link" @click="onTest">
                测试连接
              </el-button>
            </div>
          </template>

          <el-form :model="form" :label-position="isMobile ? 'top' : 'right'" label-width="95px">
            <el-form-item label="服务地址">
              <el-input v-model="form.url" placeholder="http://localhost:8086" class="font-mono">
                <template #prefix>
                  <el-icon><Compass /></el-icon>
                </template>
              </el-input>
            </el-form-item>

            <el-form-item label="鉴权 Token">
              <el-input
                v-model="form.token"
                placeholder="留空表示沿用当前 Token（脱敏保护）"
                type="password"
                show-password
                clearable
                class="font-mono"
              >
                <template #prefix>
                  <el-icon><Key /></el-icon>
                </template>
              </el-input>
            </el-form-item>

            <el-form-item label="组织 (Org)">
              <el-input v-model="form.org" placeholder="例如：jinxin" class="font-mono">
                <template #prefix>
                  <el-icon><OfficeBuilding /></el-icon>
                </template>
              </el-input>
            </el-form-item>

            <el-form-item label="数据桶 Bucket">
              <el-input v-model="form.bucket" placeholder="例如：historydb" class="font-mono">
                <template #prefix>
                  <el-icon><Files /></el-icon>
                </template>
              </el-input>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>

      <!-- 钉钉推送配置卡片 -->
      <el-col :xs="24" :sm="24" :md="11">
        <el-card class="config-card">
          <template #header>
            <div class="card-header-flex">
              <div class="header-title">
                <el-icon class="header-icon warning"><Bell /></el-icon>
                <span>钉钉每日统计推送</span>
              </div>
              <el-switch v-model="form.dingTalkEnabled" inline-prompt active-text="开启" inactive-text="关闭" />
            </div>
          </template>

          <el-form :model="form" :label-position="isMobile ? 'top' : 'right'" label-width="95px">
            <el-form-item label="Webhook">
              <el-input
                v-model="form.dingTalkWebhookUrl"
                placeholder="https://oapi.dingtalk.com/robot/send?access_token=..."
                class="font-mono"
              />
            </el-form-item>

            <el-form-item label="加签密钥">
              <el-input
                v-model="form.dingTalkSecret"
                type="password"
                show-password
                placeholder="SEC 开头的密钥（留空沿用现有）"
                clearable
                class="font-mono"
              />
            </el-form-item>

            <el-form-item label="定时发送">
              <el-time-picker
                v-model="sendTime"
                format="HH:mm"
                placeholder="选择每天推送时间"
                :clearable="false"
                class="full-width-control"
              />
            </el-form-item>

            <el-form-item label="消息模板">
              <div class="template-box">
                <div class="tag-chips">
                  <span class="chip-label">快捷插入变量：</span>
                  <el-tag
                    v-for="tag in templateTags"
                    :key="tag"
                    size="small"
                    effect="plain"
                    class="tag-chip"
                    @click="insertTag(tag)"
                  >
                    + {{ tag }}
                  </el-tag>
                </div>
                <el-input
                  ref="templateInputRef"
                  v-model="form.dingTalkMessageTemplate"
                  type="textarea"
                  :rows="4"
                  placeholder="自定义推送消息格式"
                  class="font-mono template-input"
                />
              </div>
            </el-form-item>
          </el-form>
        </el-card>
      </el-col>

      <!-- 底部持久化与保存操作条 -->
      <el-col :span="24">
        <div class="save-bar-card">
          <div class="save-bar-left">
            <el-checkbox v-model="persist" class="persist-check">
              持久化写入服务器配置文件 (appsettings.json)
            </el-checkbox>
            <span class="save-tip">勾选后服务器重启仍保留当前设置</span>
          </div>

          <div class="save-bar-right">
            <el-button type="primary" :loading="saving" :icon="Check" @click="onSave" class="save-btn">
              保存并应用配置
            </el-button>
          </div>
        </div>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import {
  Connection,
  Bell,
  Link,
  Compass,
  Key,
  OfficeBuilding,
  Files,
  Check
} from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import {
  getConfig,
  saveConfig,
  testConnection,
  type AppConfig
} from '@/api/config'
import { useIsMobile } from '@/composables/useIsMobile'

const { isMobile } = useIsMobile()

const loading = ref(false)
const saving = ref(false)
const persist = ref(true)

const templateTags = ['{{date}}', '{{total_count}}', '{{start_time}}', '{{end_time}}', '{{variable_stats}}']
const templateInputRef = ref<any>()

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

function insertTag(tag: string) {
  form.value.dingTalkMessageTemplate = (form.value.dingTalkMessageTemplate || '') + ' ' + tag
}

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
    ElMessage.success('连接成功，InfluxDB 引擎响应正常')
  } else {
    ElMessage.error('连接失败：InfluxDB 服务未响应，请检查服务地址和网络')
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
      ElMessage.success('配置已成功保存并即时生效')
    } else {
      ElMessage.warning(res.error || '配置已保存，但连接测试未通过')
    }
    form.value = await getConfig()
  } finally {
    saving.value = false
  }
}
</script>

<style scoped>
.config-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.config-card {
  height: 100%;
}

.card-header-flex {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.header-title {
  display: flex;
  align-items: center;
  gap: 8px;
  font-weight: 600;
  font-size: 14px;
  color: #0f172a;
}

.header-icon {
  font-size: 16px;
}

.header-icon.primary { color: #2563eb; }
.header-icon.warning { color: #f59e0b; }

.full-width-control {
  width: 100%;
}

.template-box {
  display: flex;
  flex-direction: column;
  gap: 8px;
  width: 100%;
}

.tag-chips {
  display: flex;
  align-items: center;
  gap: 6px;
  flex-wrap: wrap;
}

.chip-label {
  font-size: 11px;
  color: #64748b;
}

.tag-chip {
  cursor: pointer;
  transition: all 0.15s ease;
  font-family: 'JetBrains Mono', monospace;
  font-size: 11px;
}

.tag-chip:hover {
  background: #eff6ff;
  border-color: #2563eb;
  color: #2563eb;
}

/* 底部操作条 */
.save-bar-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 16px 24px;
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-top: 4px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.save-bar-left {
  display: flex;
  align-items: center;
  gap: 12px;
}

.save-tip {
  font-size: 12px;
  color: #94a3b8;
}

.save-btn {
  padding: 8px 24px;
  font-weight: 600;
}

@media (max-width: 768px) {
  .save-bar-card {
    flex-direction: column;
    align-items: stretch;
    gap: 12px;
    padding: 14px;
  }

  .save-bar-left {
    flex-direction: column;
    align-items: flex-start;
    gap: 4px;
  }

  .save-btn {
    width: 100%;
  }
}
</style>
