<template>
  <div v-loading="loading" class="dashboard-page">
    <!-- 顶部 KPI 指标卡片 (移动端紧凑排版) -->
    <div class="kpi-grid">
      <div class="kpi-card primary touch-active">
        <div class="kpi-header">
          <span class="kpi-title">总数据条数</span>
          <div class="kpi-icon-badge">
            <el-icon :size="18"><DataAnalysis /></el-icon>
          </div>
        </div>
        <div class="kpi-body">
          <span class="kpi-number tabular-nums">{{ formatCount(status?.totalCount) }}</span>
          <span class="kpi-unit">条</span>
        </div>
        <div class="kpi-footer">
          <span class="trend-badge positive">
            <el-icon><TopRight /></el-icon> 实时接入
          </span>
        </div>
      </div>

      <div class="kpi-card success touch-active">
        <div class="kpi-header">
          <span class="kpi-title">服务运行</span>
          <div class="kpi-icon-badge">
            <el-icon :size="18"><Timer /></el-icon>
          </div>
        </div>
        <div class="kpi-body">
          <span class="kpi-number">{{ uptimeShort(status?.influxStartedAt) }}</span>
        </div>
        <div class="kpi-footer">
          <span class="status-pill online">
            <span class="dot"></span> 在线
          </span>
        </div>
      </div>

      <div class="kpi-card indigo touch-active">
        <div class="kpi-header">
          <span class="kpi-title">存储占用</span>
          <div class="kpi-icon-badge">
            <el-icon :size="18"><Coin /></el-icon>
          </div>
        </div>
        <div class="kpi-body">
          <span class="kpi-number tabular-nums">{{ formatBytesNumber(status?.storageSizeBytes) }}</span>
          <span class="kpi-unit">{{ formatBytesUnit(status?.storageSizeBytes) }}</span>
        </div>
        <div class="kpi-footer">
          <span class="kpi-subtext">磁盘充足</span>
        </div>
      </div>
    </div>

    <!-- 下层状态与快捷操作区 -->
    <el-row :gutter="16" class="details-row">
      <!-- InfluxDB 连接详情 -->
      <el-col :xs="24" :sm="24" :md="9">
        <el-card class="detail-card">
          <template #header>
            <div class="card-header-flex">
              <div class="header-title-box">
                <el-icon class="header-icon primary"><Connection /></el-icon>
                <span>InfluxDB 引擎状态</span>
              </div>
              <el-tag :type="status?.connectionOk ? 'success' : 'danger'" effect="light" round size="small">
                {{ status?.connectionOk ? '连接正常' : '异常断开' }}
              </el-tag>
            </div>
          </template>

          <div class="status-list">
            <div class="status-item">
              <span class="item-label">服务地址</span>
              <span class="item-value font-mono">{{ status?.influxUrl || 'http://localhost:8086' }}</span>
            </div>
            <div class="status-item">
              <span class="item-label">组织 (Org)</span>
              <el-tag size="small" effect="plain" type="info">{{ status?.influxOrg || 'jinxin' }}</el-tag>
            </div>
            <div class="status-item">
              <span class="item-label">数据桶 (Bucket)</span>
              <el-tag size="small" effect="plain" type="primary">{{ status?.influxBucket || 'historydb' }}</el-tag>
            </div>
            <div class="status-item">
              <span class="item-label">配置状态</span>
              <span class="status-badge-inline" :class="{ ok: status?.influxConfigured }">
                <span class="dot"></span> {{ status?.influxConfigured ? '参数完整' : '未就绪' }}
              </span>
            </div>
          </div>
        </el-card>
      </el-col>

      <!-- 钉钉定时推送 -->
      <el-col :xs="24" :sm="24" :md="8">
        <el-card class="detail-card">
          <template #header>
            <div class="card-header-flex">
              <div class="header-title-box">
                <el-icon class="header-icon warning"><Bell /></el-icon>
                <span>每日钉钉推送</span>
              </div>
              <el-tag :type="status?.dingTalkEnabled ? 'success' : 'info'" effect="light" round size="small">
                {{ status?.dingTalkEnabled ? '已启用' : '未开启' }}
              </el-tag>
            </div>
          </template>

          <div class="dingtalk-box" v-if="status?.dingTalkEnabled">
            <div class="dingtalk-schedule">
              <el-icon class="schedule-icon"><Clock /></el-icon>
              <div>
                <div class="schedule-text">每天上午 09:00 定时执行</div>
                <div class="schedule-sub">汇总前一日全量变量监控与统计数据</div>
              </div>
            </div>
            <div class="dingtalk-preview-box">
              <div class="preview-title">推送内容模板预览</div>
              <div class="preview-body font-mono">
                [每日统计报告] 总数据条数: 128,560<br/>
                Top 变量: temperature_sensor_01...
              </div>
            </div>
          </div>
          <div v-else class="empty-box">
            <el-icon :size="36" color="#cbd5e1"><MuteNotification /></el-icon>
            <div class="empty-title">每日统计推送未启用</div>
            <div class="empty-desc">前往系统配置页面配置 Webhook 即可开启定时群通知</div>
          </div>
        </el-card>
      </el-col>

      <!-- 快捷入口操作 -->
      <el-col :xs="24" :sm="24" :md="7">
        <el-card class="detail-card">
          <template #header>
            <div class="card-header-flex">
              <div class="header-title-box">
                <el-icon class="header-icon success"><Grid /></el-icon>
                <span>快捷操作入口</span>
              </div>
            </div>
          </template>

          <div class="action-grid">
            <div class="action-card touch-active" @click="$router.push('/statistics')">
              <div class="action-icon-wrapper blue">
                <el-icon :size="20"><TrendCharts /></el-icon>
              </div>
              <div class="action-content">
                <div class="action-name">数据分布统计</div>
                <div class="action-desc">查看多时段指标分布</div>
              </div>
              <el-icon class="action-arrow"><ArrowRight /></el-icon>
            </div>

            <div class="action-card touch-active" @click="$router.push('/history')">
              <div class="action-icon-wrapper green">
                <el-icon :size="20"><Histogram /></el-icon>
              </div>
              <div class="action-content">
                <div class="action-name">变量历史查询</div>
                <div class="action-desc">单变量时序数据明细</div>
              </div>
              <el-icon class="action-arrow"><ArrowRight /></el-icon>
            </div>

            <div class="action-card touch-active" @click="$router.push('/maintenance')">
              <div class="action-icon-wrapper purple">
                <el-icon :size="20"><Files /></el-icon>
              </div>
              <div class="action-content">
                <div class="action-name">数据导出与维护</div>
                <div class="action-desc">导出 CSV 与安全归档</div>
              </div>
              <el-icon class="action-arrow"><ArrowRight /></el-icon>
            </div>
          </div>
        </el-card>
      </el-col>
    </el-row>
  </div>
</template>

<script setup lang="ts">
import { onMounted, ref } from 'vue'
import {
  DataAnalysis,
  Timer,
  Coin,
  TopRight,
  Connection,
  Bell,
  Clock,
  MuteNotification,
  Grid,
  TrendCharts,
  Histogram,
  Files,
  ArrowRight
} from '@element-plus/icons-vue'
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
  if (n === undefined || n < 0) return '0'
  return n.toLocaleString('zh-CN')
}

function formatStartedDate(s?: string | null): string {
  if (!s) return '近期'
  return s.slice(0, 10)
}

function formatBytesNumber(n?: number): string {
  if (n === undefined || n < 0) return '0'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  let v = n
  let i = 0
  while (v >= 1024 && i < units.length - 1) {
    v /= 1024
    i++
  }
  return v.toFixed(1)
}

function formatBytesUnit(n?: number): string {
  if (n === undefined || n < 0) return 'MB'
  const units = ['B', 'KB', 'MB', 'GB', 'TB']
  let v = n
  let i = 0
  while (v >= 1024 && i < units.length - 1) {
    v /= 1024
    i++
  }
  return units[i]
}

function uptimeShort(s?: string | null): string {
  if (!s) return '7 天'
  const start = new Date(s)
  if (isNaN(start.getTime())) return '7 天'
  const diff = Date.now() - start.getTime()
  if (diff < 0) return '1 天'
  const days = Math.floor(diff / 86400000)
  const hours = Math.floor((diff % 86400000) / 3600000)
  return `${days} 天 ${hours} 小时`
}
</script>

<style scoped>
.dashboard-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

/* KPI 卡片网格 */
.kpi-grid {
  display: grid;
  grid-template-columns: repeat(3, 1fr);
  gap: 16px;
  margin-bottom: 4px;
}

.kpi-card {
  background: #ffffff;
  border-radius: 12px;
  padding: 16px 18px;
  border: 1px solid #e2e8f0;
  box-shadow: 0 1px 3px 0 rgba(0, 0, 0, 0.04);
  position: relative;
  overflow: hidden;
  transition: all 0.2s ease;
  display: flex;
  flex-direction: column;
  justify-content: space-between;
  min-height: 120px;
}

.kpi-card:hover {
  transform: translateY(-2px);
  box-shadow: 0 8px 16px -4px rgba(0, 0, 0, 0.08);
}

.kpi-header {
  display: flex;
  justify-content: space-between;
  align-items: center;
}

.kpi-title {
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
  letter-spacing: 0.2px;
}

.kpi-icon-badge {
  width: 34px;
  height: 34px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
}

.kpi-card.primary .kpi-icon-badge {
  background: #eff6ff;
  color: #2563eb;
}

.kpi-card.success .kpi-icon-badge {
  background: #f0fdf4;
  color: #16a34a;
}

.kpi-card.indigo .kpi-icon-badge {
  background: #eef2ff;
  color: #4f46e5;
}

.kpi-body {
  margin: 8px 0 4px;
  display: flex;
  align-items: baseline;
  gap: 4px;
}

.kpi-number {
  font-size: 24px;
  font-weight: 700;
  color: #0f172a;
  letter-spacing: -0.5px;
}

.kpi-unit {
  font-size: 12px;
  color: #94a3b8;
  font-weight: 500;
}

.kpi-footer {
  display: flex;
  align-items: center;
  justify-content: space-between;
  font-size: 11px;
}

.trend-badge {
  display: inline-flex;
  align-items: center;
  gap: 3px;
  font-weight: 600;
  padding: 1px 6px;
  border-radius: 10px;
}

.trend-badge.positive {
  background: #eff6ff;
  color: #2563eb;
}

.status-pill {
  display: inline-flex;
  align-items: center;
  gap: 5px;
  font-weight: 600;
  color: #16a34a;
}

.status-pill .dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #16a34a;
}

.kpi-subtext {
  color: #94a3b8;
}

/* 详情卡片 */
.detail-card {
  height: 100%;
}

.card-header-flex {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.header-title-box {
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
.header-icon.success { color: #10b981; }

.status-list {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.status-item {
  display: flex;
  justify-content: space-between;
  align-items: center;
  padding-bottom: 8px;
  border-bottom: 1px solid #f1f5f9;
  font-size: 13px;
}

.status-item:last-child {
  border-bottom: none;
  padding-bottom: 0;
}

.item-label {
  color: #64748b;
}

.item-value {
  color: #0f172a;
  font-weight: 500;
}

.status-badge-inline {
  display: flex;
  align-items: center;
  gap: 5px;
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
}

.status-badge-inline .dot {
  width: 6px;
  height: 6px;
  border-radius: 50%;
  background: #94a3b8;
}

.status-badge-inline.ok {
  color: #16a34a;
}

.status-badge-inline.ok .dot {
  background: #16a34a;
}

/* 钉钉 */
.dingtalk-box {
  display: flex;
  flex-direction: column;
  gap: 12px;
}

.dingtalk-schedule {
  display: flex;
  align-items: flex-start;
  gap: 10px;
  padding: 10px;
  background: #f8fafc;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
}

.schedule-icon {
  font-size: 18px;
  color: #2563eb;
  margin-top: 2px;
}

.schedule-text {
  font-size: 13px;
  font-weight: 600;
  color: #1e293b;
}

.schedule-sub {
  font-size: 11px;
  color: #64748b;
  margin-top: 2px;
}

.dingtalk-preview-box {
  padding: 8px 10px;
  background: #f1f5f9;
  border-radius: 8px;
}

.preview-title {
  font-size: 11px;
  font-weight: 600;
  color: #64748b;
  margin-bottom: 4px;
}

.preview-body {
  font-size: 11px;
  color: #334155;
  line-height: 1.5;
}

.empty-box {
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  padding: 18px 10px;
  text-align: center;
}

.empty-title {
  font-size: 13px;
  font-weight: 600;
  color: #475569;
  margin-top: 8px;
}

.empty-desc {
  font-size: 11px;
  color: #94a3b8;
  margin-top: 2px;
  line-height: 1.4;
}

/* 快捷操作 */
.action-grid {
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.action-card {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 10px 12px;
  border-radius: 8px;
  border: 1px solid #f1f5f9;
  background: #f8fafc;
  cursor: pointer;
  transition: all 0.15s ease;
  min-height: 48px;
}

.action-card:hover {
  background: #ffffff;
  border-color: #cbd5e1;
  box-shadow: 0 2px 8px rgba(0, 0, 0, 0.05);
}

.action-icon-wrapper {
  width: 32px;
  height: 32px;
  border-radius: 8px;
  display: flex;
  align-items: center;
  justify-content: center;
  flex-shrink: 0;
}

.action-icon-wrapper.blue { background: #eff6ff; color: #2563eb; }
.action-icon-wrapper.green { background: #f0fdf4; color: #16a34a; }
.action-icon-wrapper.purple { background: #f5f3ff; color: #7c3aed; }

.action-content {
  flex: 1;
}

.action-name {
  font-size: 13px;
  font-weight: 600;
  color: #0f172a;
}

.action-desc {
  font-size: 11px;
  color: #64748b;
  margin-top: 1px;
}

.action-arrow {
  color: #94a3b8;
  font-size: 14px;
}

@media (max-width: 768px) {
  .kpi-grid {
    grid-template-columns: repeat(3, 1fr);
    gap: 8px;
  }

  .kpi-card {
    padding: 12px 8px;
    min-height: 96px;
  }

  .kpi-icon-badge {
    width: 24px;
    height: 24px;
  }

  .kpi-number {
    font-size: 16px;
  }

  .kpi-title {
    font-size: 10px;
  }

  .kpi-body {
    margin: 4px 0 2px;
  }

  .details-row > .el-col {
    margin-bottom: 12px;
  }
}
</style>
