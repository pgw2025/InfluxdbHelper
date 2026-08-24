<template>
  <div class="statistics-page" v-loading="loading">
    <!-- 顶部控制栏 -->
    <el-card class="filter-card">
      <div class="filter-toolbar">
        <div class="filter-left">
          <PeriodSegmented v-model="period" @change="load" />
        </div>

        <div class="filter-right">
          <el-button-group class="chart-type-group">
            <el-button
              :type="chartType === 'bar' ? 'primary' : 'default'"
              size="small"
              @click="switchChartType('bar')"
            >
              柱状图
            </el-button>
            <el-button
              :type="chartType === 'pie' ? 'primary' : 'default'"
              size="small"
              @click="switchChartType('pie')"
            >
              饼图
            </el-button>
          </el-button-group>

          <el-button :icon="Refresh" circle class="refresh-action-btn" title="刷新统计" @click="load" />
        </div>

        <!-- 自定义时间范围（抽屉/弹窗响应式选择） -->
        <div v-if="period === 'custom'" class="custom-range-row">
          <CustomDateRangePicker
            v-model="customRange"
            placeholder="点击选择自定义起止时间..."
            @change="load"
          />
        </div>
      </div>
    </el-card>

    <template v-if="summary">
      <!-- 汇总信息条 (移动端响应式网格) -->
      <div class="summary-hero-bar">
        <div class="summary-item">
          <div class="summary-label">数据采集总计</div>
          <div class="summary-val-wrap">
            <span class="summary-num tabular-nums">{{ summary.total.toLocaleString() }}</span>
            <span class="summary-unit">条</span>
          </div>
        </div>
        <div class="summary-divider"></div>
        <div class="summary-item">
          <div class="summary-label">已监控变量</div>
          <div class="summary-val-wrap">
            <span class="summary-num tabular-nums">{{ summary.variables?.length || 0 }}</span>
            <span class="summary-unit">个</span>
          </div>
        </div>
        <div class="summary-divider"></div>
        <div class="summary-item range-item">
          <div class="summary-label">统计时间范围</div>
          <div class="summary-range-text font-mono">
            {{ formatTime(summary.startTime) }} ~ {{ formatTime(summary.endTime) }}
          </div>
        </div>
      </div>

      <!-- 分布图表与排行榜 -->
      <el-row :gutter="16">
        <!-- 排行榜 -->
        <el-col :xs="24" :sm="24" :md="10">
          <el-card class="content-card">
            <template #header>
              <div class="card-title-flex">
                <span>变量数据排名 Top 15</span>
                <span class="sub-tag">点击查看趋势</span>
              </div>
            </template>

            <!-- 桌面端表格 -->
            <el-table
              v-if="!isMobile"
              class="rank-table"
              :data="topVariables"
              height="450"
              stripe
              @row-click="onRowClick"
            >
              <el-table-column label="排名" width="68" align="center">
                <template #default="{ $index }">
                  <span class="rank-badge" :class="'rank-' + ($index + 1)">{{ $index + 1 }}</span>
                </template>
              </el-table-column>
              
              <el-table-column label="变量名称" min-width="170" show-overflow-tooltip>
                <template #default="{ row }">
                  <span class="var-name-link font-mono">{{ row.variableName }}</span>
                </template>
              </el-table-column>

              <el-table-column prop="count" label="条数" width="105" sortable align="right">
                <template #default="{ row }">
                  <span class="count-num tabular-nums font-mono">{{ row.count.toLocaleString() }}</span>
                </template>
              </el-table-column>

              <el-table-column label="占比" width="130">
                <template #default="{ row }">
                  <div class="pct-bar-wrap">
                    <el-progress
                      :percentage="percentage(row.count)"
                      :stroke-width="6"
                      :show-text="false"
                      :color="getBarColor(percentage(row.count))"
                    />
                    <span class="pct-num tabular-nums">{{ percentage(row.count).toFixed(1) }}%</span>
                  </div>
                </template>
              </el-table-column>
            </el-table>

            <!-- 移动端列表 -->
            <div v-else class="var-cards">
              <div
                v-for="(row, idx) in topVariables"
                :key="row.variableName"
                class="var-card touch-active"
                role="button"
                tabindex="0"
                @click="goVariable(row.variableName)"
              >
                <div class="var-card-top">
                  <div class="rank-left">
                    <span class="rank-badge" :class="'rank-' + (idx + 1)">{{ idx + 1 }}</span>
                    <span class="var-name font-mono">{{ row.variableName }}</span>
                  </div>
                  <div class="rank-right">
                    <span class="var-count tabular-nums font-mono">{{ row.count.toLocaleString() }}</span>
                    <el-icon class="var-arrow"><ArrowRight /></el-icon>
                  </div>
                </div>
                <div class="var-progress-box">
                  <el-progress :percentage="percentage(row.count)" :stroke-width="5" :show-text="false" />
                  <span class="var-pct">{{ percentage(row.count).toFixed(1) }}%</span>
                </div>
              </div>
            </div>
          </el-card>
        </el-col>

        <!-- ECharts 可视化图表 -->
        <el-col :xs="24" :sm="24" :md="14">
          <el-card class="content-card">
            <template #header>
              <div class="card-title-flex">
                <span>{{ chartType === 'bar' ? '数据量柱状分布图' : '变量占比饼图' }}</span>
                <span class="sub-tag">时序可视化</span>
              </div>
            </template>
            <div ref="chartRef" class="chart-container"></div>
          </el-card>
        </el-col>
      </el-row>
    </template>

    <el-empty v-else-if="!loading" description="当前区间暂无统计数据" />
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import * as echarts from 'echarts'
import { ArrowRight, Refresh } from '@element-plus/icons-vue'
import { getSummary, type StatisticsSummary } from '@/api/statistics'
import { useIsMobile } from '@/composables/useIsMobile'
import { registerPullRefresh } from '@/composables/pullRefresh'
import PeriodSegmented from '@/components/PeriodSegmented.vue'
import CustomDateRangePicker from '@/components/CustomDateRangePicker.vue'

const { isMobile } = useIsMobile()
const router = useRouter()

const period = ref('day')
const chartType = ref<'bar' | 'pie'>('bar')
const customRange = ref<[string, string] | null>(null)
const summary = ref<StatisticsSummary | null>(null)
const loading = ref(false)

const chartRef = ref<HTMLElement>()
let chart: echarts.ECharts | null = null
let ro: ResizeObserver | null = null
let unregisterPr: (() => void) | null = null

const topVariables = computed(() =>
  [...(summary.value?.variables ?? [])]
    .sort((a, b) => b.count - a.count)
    .slice(0, 15)
)

function percentage(count: number) {
  const total = summary.value?.total || 0
  return total > 0 ? (count / total) * 100 : 0
}

function getBarColor(pct: number) {
  if (pct > 30) return '#2563eb'
  if (pct > 15) return '#3b82f6'
  return '#60a5fa'
}

function formatTime(iso: string) {
  return iso ? iso.replace('T', ' ').slice(0, 19) : '-'
}

function goVariable(name: string) {
  router.push({ name: 'history', query: { variable: name } })
}

function onRowClick(row: { variableName: string }) {
  goVariable(row.variableName)
}

function switchChartType(type: 'bar' | 'pie') {
  chartType.value = type
  renderChart()
}

async function load() {
  if (period.value === 'custom' && !customRange.value) return
  loading.value = true
  try {
    const [start, end] = customRange.value ?? []
    summary.value = await getSummary(period.value, start, end)
    await nextTick()
    renderChart()
  } finally {
    loading.value = false
  }
}

function renderChart() {
  if (!chartRef.value) return
  if (!chart) chart = echarts.init(chartRef.value)

  if (chartType.value === 'pie') {
    const pieData = topVariables.value.map(v => ({
      name: v.variableName,
      value: v.count
    }))

    chart.setOption({
      tooltip: {
        trigger: 'item',
        confine: true,
        formatter: '{b}: <br/><b>{c} 条</b> ({d}%)'
      },
      legend: {
        type: 'scroll',
        orient: isMobile.value ? 'horizontal' : 'vertical',
        right: isMobile.value ? 'center' : 10,
        bottom: isMobile.value ? 0 : 20,
        top: isMobile.value ? undefined : 20,
        textStyle: { color: '#475569', fontSize: 11 }
      },
      series: [
        {
          name: '数据占比',
          type: 'pie',
          radius: isMobile.value ? ['38%', '65%'] : ['45%', '75%'],
          center: isMobile.value ? ['50%', '42%'] : ['40%', '50%'],
          avoidLabelOverlap: false,
          itemStyle: {
            borderRadius: 6,
            borderColor: '#fff',
            borderWidth: 2
          },
          label: { show: false },
          emphasis: {
            label: {
              show: true,
              fontSize: 12,
              fontWeight: 'bold'
            }
          },
          data: pieData
        }
      ]
    }, true)
  } else {
    // 柱状图
    const vars = topVariables.value.slice().reverse()
    chart.setOption({
      tooltip: {
        trigger: 'axis',
        confine: true,
        axisPointer: { type: 'shadow' },
        formatter: (params: any) => {
          const item = params[0]
          return `<div style="font-weight:600;margin-bottom:4px;">${item.name}</div>
                  <div style="display:flex;align-items:center;gap:6px;">
                    <span style="display:inline-block;width:8px;height:8px;border-radius:50%;background:#2563eb;"></span>
                    <span>数据量: <b>${item.value.toLocaleString()} 条</b></span>
                  </div>`
        }
      },
      grid: {
        left: isMobile.value ? 8 : 12,
        right: isMobile.value ? 24 : 36,
        top: 20,
        bottom: 20,
        containLabel: true
      },
      xAxis: {
        type: 'value',
        name: isMobile.value ? '' : '条数',
        axisLine: { lineStyle: { color: '#cbd5e1' } },
        splitLine: { lineStyle: { color: '#f1f5f9' } },
        axisLabel: { fontSize: 10 }
      },
      yAxis: {
        type: 'category',
        data: vars.map(v => v.variableName),
        axisLabel: {
          width: isMobile.value ? 90 : 140,
          overflow: 'truncate',
          color: '#475569',
          fontFamily: 'JetBrains Mono, monospace',
          fontSize: isMobile.value ? 10 : 12
        },
        axisLine: { lineStyle: { color: '#cbd5e1' } }
      },
      series: [
        {
          name: '数据条数',
          type: 'bar',
          data: vars.map(v => v.count),
          itemStyle: {
            color: new echarts.graphic.LinearGradient(0, 0, 1, 0, [
              { offset: 0, color: '#3b82f6' },
              { offset: 1, color: '#2563eb' }
            ]),
            borderRadius: [0, 6, 6, 0]
          },
          label: {
            show: true,
            position: 'right',
            color: '#64748b',
            fontFamily: 'JetBrains Mono, monospace',
            fontSize: 10
          }
        }
      ]
    }, true)
  }
}

function onResize() {
  chart?.resize()
}

onMounted(() => {
  load()
  window.addEventListener('resize', onResize)
  if (chartRef.value && 'ResizeObserver' in window) {
    ro = new ResizeObserver(() => onResize())
    ro.observe(chartRef.value)
  }
  unregisterPr = registerPullRefresh(load)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  ro?.disconnect()
  ro = null
  chart?.dispose()
  chart = null
  unregisterPr?.()
})
</script>

<style scoped>
.statistics-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.filter-card {
  margin-bottom: 0;
}

.filter-toolbar {
  display: flex;
  justify-content: space-between;
  align-items: center;
  flex-wrap: wrap;
  gap: 12px;
}

.filter-left {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.period-pills {
  background: #f1f5f9;
  border-radius: 8px;
  padding: 3px;
  gap: 2px;
  border: 1px solid #e2e8f0;
}

.period-pills :deep(.el-radio-button__inner) {
  border: none !important;
  background: transparent !important;
  box-shadow: none !important;
  border-radius: 6px !important;
  color: #64748b;
  font-weight: 500;
  font-size: 13px;
  padding: 6px 14px;
}

.period-pills :deep(.el-radio-button.is-active .el-radio-button__inner) {
  background: #ffffff !important;
  color: #2563eb !important;
  font-weight: 600;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08) !important;
}

.filter-right {
  display: flex;
  align-items: center;
  gap: 10px;
}

.refresh-action-btn {
  transition: all 0.2s ease;
}

.refresh-action-btn:hover {
  transform: rotate(180deg);
}

/* 汇总信息条 */
.summary-hero-bar {
  display: flex;
  align-items: center;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 16px 24px;
  gap: 24px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
}

.summary-item {
  display: flex;
  flex-direction: column;
}

.summary-label {
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
  margin-bottom: 4px;
}

.summary-val-wrap {
  display: flex;
  align-items: baseline;
  gap: 6px;
}

.summary-num {
  font-size: 22px;
  font-weight: 700;
  color: #2563eb;
}

.summary-unit {
  font-size: 12px;
  color: #94a3b8;
}

.summary-divider {
  width: 1px;
  height: 36px;
  background: #e2e8f0;
}

.range-item {
  flex: 1;
}

.summary-range-text {
  font-size: 13px;
  color: #334155;
  font-weight: 500;
}

/* 内容卡片 */
.content-card {
  height: 100%;
}

.card-title-flex {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 14px;
  font-weight: 600;
  color: #0f172a;
}

.sub-tag {
  font-size: 12px;
  font-weight: 400;
  color: #94a3b8;
}

/* 排名徽章 */
.rank-badge {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  width: 22px;
  height: 22px;
  border-radius: 6px;
  font-size: 11px;
  font-weight: 700;
  background: #f1f5f9;
  color: #64748b;
}

.rank-badge.rank-1 {
  background: #fef3c7;
  color: #d97706;
}

.rank-badge.rank-2 {
  background: #f1f5f9;
  color: #475569;
}

.rank-badge.rank-3 {
  background: #ffedd5;
  color: #c2410c;
}

.var-name-link {
  color: #2563eb;
  cursor: pointer;
  font-weight: 500;
  font-size: 13px;
}

.var-name-link:hover {
  text-decoration: underline;
}

.count-num {
  font-size: 13px;
  color: #0f172a;
  font-weight: 600;
}

.pct-bar-wrap {
  display: flex;
  align-items: center;
  gap: 8px;
}

.pct-num {
  font-size: 11px;
  color: #64748b;
  min-width: 38px;
}

.rank-table :deep(.el-table__row) {
  cursor: pointer;
  transition: background-color 0.15s ease;
}

.chart-container {
  height: 450px;
  width: 100%;
}

/* 移动端卡片列表 */
.var-cards {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.var-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 12px;
  cursor: pointer;
  transition: all 0.15s ease;
}

.var-card:active {
  background: #f8fafc;
  transform: scale(0.99);
}

.var-card-top {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.rank-left {
  display: flex;
  align-items: center;
  gap: 8px;
  overflow: hidden;
}

.rank-right {
  display: flex;
  align-items: center;
  gap: 4px;
}

.var-name {
  font-size: 13px;
  font-weight: 600;
  color: #0f172a;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.var-count {
  font-size: 13px;
  font-weight: 700;
  color: #2563eb;
}

.var-arrow {
  font-size: 14px;
  color: #94a3b8;
}

.var-progress-box {
  display: flex;
  align-items: center;
  gap: 10px;
}

.var-pct {
  font-size: 11px;
  color: #64748b;
  font-weight: 500;
}

.custom-range-row {
  width: 100%;
}

.custom-range-picker {
  width: 100% !important;
}

@media (max-width: 768px) {
  .filter-toolbar {
    flex-direction: column;
    align-items: stretch;
    gap: 8px;
  }

  .filter-left {
    width: 100%;
  }

  .filter-right {
    justify-content: space-between;
    width: 100%;
  }

  .summary-hero-bar {
    display: grid;
    grid-template-columns: 1fr 1fr;
    gap: 12px;
    padding: 12px 14px;
  }

  .summary-divider {
    display: none;
  }

  .range-item {
    grid-column: span 2;
    padding-top: 8px;
    border-top: 1px solid #f1f5f9;
    min-width: 0;
    overflow: hidden;
  }

  .summary-num {
    font-size: 18px;
  }

  .summary-range-text {
    font-size: 11px;
    word-break: break-all;
    white-space: normal;
    line-height: 1.4;
    color: #475569;
  }

  .chart-container {
    height: 300px;
  }
}
</style>
