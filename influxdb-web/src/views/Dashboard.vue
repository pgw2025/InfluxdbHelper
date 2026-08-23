<template>
  <div v-loading="loading">
    <!-- InfluxDB 数据概览：总条数 / 启动时间 / 占用空间 -->
    <el-row :gutter="16" class="overview-row">
      <el-col :xs="24">
        <el-card>
          <template #header>
            <span>InfluxDB 数据概览</span>
          </template>
          <div class="metric-grid">
            <div class="metric">
              <div class="metric-label">总数据条数</div>
              <div class="metric-value">{{ formatCount(status?.totalCount) }}</div>
            </div>
            <div class="metric">
              <div class="metric-label">启动时间</div>
              <div class="metric-value">{{ formatStarted(status?.influxStartedAt) }}</div>
              <div v-if="status?.influxStartedAt" class="metric-sub">{{ uptimeText(status.influxStartedAt) }}</div>
            </div>
            <div class="metric">
              <div class="metric-label">占用空间</div>
              <div class="metric-value">{{ formatBytes(status?.storageSizeBytes) }}</div>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>

    <el-row :gutter="16">
      <el-col :xs="24" :sm="24" :md="8">
        <el-card>
          <template #header>
            <span>InfluxDB 连接</span>
          </template>
          <div class="status-line">
            <span>配置状态</span>
            <el-tag :type="status?.influxConfigured ? 'success' : 'danger'">
              {{ status?.influxConfigured ? '已配置' : '未配置' }}
            </el-tag>
          </div>
          <div class="status-line">
            <span>连接状态</span>
            <el-tag :type="status?.connectionOk ? 'success' : 'danger'">
              {{ status?.connectionOk ? '正常' : '异常' }}
            </el-tag>
          </div>
          <div class="status-line">
            <span>服务地址</span>
            <span class="status-value">{{ status?.influxUrl || '-' }}</span>
          </div>
          <div class="status-line">
            <span>组织</span>
            <span class="status-value">{{ status?.influxOrg || '-' }}</span>
          </div>
          <div class="status-line">
            <span>Bucket</span>
            <span class="status-value">{{ status?.influxBucket || '-' }}</span>
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="24" :md="8">
        <el-card>
          <template #header>
            <span>钉钉推送</span>
          </template>
          <div class="status-line">
            <span>推送开关</span>
            <el-tag :type="status?.dingTalkEnabled ? 'success' : 'info'">
              {{ status?.dingTalkEnabled ? '已开启' : '已关闭' }}
            </el-tag>
          </div>
          <el-empty v-if="!status?.dingTalkEnabled" description="每日统计推送未启用" :image-size="70" />
          <div v-else class="status-tip">
            后台服务将按配置时间推送每日统计报告到钉钉群。
          </div>
        </el-card>
      </el-col>

      <el-col :xs="24" :sm="24" :md="8">
        <el-card>
          <template #header>
            <span>快捷入口</span>
          </template>
          <div class="quick-links">
            <el-button type="primary" plain @click="$router.push('/statistics')">
              <el-icon><DataAnalysis /></el-icon> 数据统计
            </el-button>
            <el-button type="primary" plain @click="$router.push('/history')">
              <el-icon><Clock /></el-icon> 变量历史
            </el-button>
            <el-button type="primary" plain @click="$router.push('/config')">
              <el-icon><Setting /></el-icon> 系统配置
            </el-button>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import { getSystemStatus, type SystemStatus } from '@/api/system'

const status = ref<SystemStatus | null>(null)
const loading = ref(false)

onMounted(async () => {
  loading.value = true
  try {
    status.value = await getSystemStatus()
  } finally {
    loading.value = false
  }
})

function formatCount(n?: number): string {
  if (n === undefined || n < 0) return '-'
  return n.toLocaleString('zh-CN')
}

function formatStarted(s?: string | null): string {
  if (!s) return '未知'
  return s.replace('T', ' ').slice(0, 19)
}

function formatBytes(n?: number): string {
  if (n === undefined || n < 0) return '未配置'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  let v = n
  let i = 0
  while (v >= 1024 && i < units.length - 1) {
    v /= 1024
    i++
  }
  return `${v.toFixed(2)} ${units[i]}`
}

function uptimeText(s: string): string {
  const start = new Date(s)
  if (isNaN(start.getTime())) return ''
  const diff = Date.now() - start.getTime()
  if (diff < 0) return ''
  const days = Math.floor(diff / 86400000)
  const hours = Math.floor((diff % 86400000) / 3600000)
  const mins = Math.floor((diff % 3600000) / 60000)
  return `已运行 ${days} 天 ${hours} 小时 ${mins} 分`
}
</script>

<style scoped>
.overview-row {
  margin-bottom: 16px;
}

.metric-grid {
  display: flex;
  flex-wrap: wrap;
  gap: 0 16px;
}

.metric {
  flex: 1 1 200px;
  min-width: 0;
  padding: 6px 16px 6px 0;
  border-right: 1px solid var(--el-border-color-lighter);
}

.metric:last-child {
  border-right: none;
  padding-right: 0;
}

.metric-label {
  font-size: 13px;
  color: var(--el-text-color-secondary);
  margin-bottom: 6px;
}

.metric-value {
  font-size: 22px;
  font-weight: 700;
  color: var(--el-color-primary);
  word-break: break-all;
  line-height: 1.3;
}

.metric-sub {
  font-size: 12px;
  color: var(--el-text-color-secondary);
  margin-top: 4px;
}

.status-line {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding: 8px 0;
  font-size: 14px;
  color: var(--el-text-color-regular);
}

.status-line + .status-line {
  border-top: 1px dashed var(--el-border-color-lighter);
}

.status-value {
  color: var(--el-text-color-primary);
  font-weight: 500;
}

.status-tip {
  margin-top: 12px;
  font-size: 13px;
  color: var(--el-text-color-secondary);
  line-height: 1.6;
}

.quick-links {
  display: flex;
  flex-direction: column;
  gap: 12px;
  padding: 8px 0;
}

.quick-links .el-button {
  width: 100%;
  justify-content: flex-start;
  gap: 6px;
}

@media (max-width: 768px) {
  .metric {
    flex: 1 1 100%;
    border-right: none;
    border-bottom: 1px dashed var(--el-border-color-lighter);
    padding: 10px 0;
  }

  .metric:last-child {
    border-bottom: none;
  }

  .metric-value {
    font-size: 20px;
  }
}
</style>
