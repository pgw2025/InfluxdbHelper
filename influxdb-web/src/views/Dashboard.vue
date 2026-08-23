<template>
  <div v-loading="loading">
    <el-row :gutter="16">
      <el-col :span="8">
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

      <el-col :span="8">
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

      <el-col :span="8">
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
</script>

<style scoped>
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
</style>
