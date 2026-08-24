<template>
  <div class="history-page" v-loading="loading">
    <!-- 顶部过滤与搜索栏 -->
    <el-card class="filter-card">
      <div class="filter-toolbar">
        <PeriodSegmented
          v-model="period"
          :show-all-option="false"
          :show-year-option="false"
          @change="onPeriodChange"
        />

        <el-autocomplete
          v-model="variableName"
          :fetch-suggestions="querySearch"
          placeholder="输入变量名（支持智能联想）"
          clearable
          class="var-autocomplete"
          @select="onSearch"
          @keyup.enter="onSearch"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-autocomplete>

        <template v-if="period === 'custom'">
          <div class="custom-range-slot">
            <CustomDateRangePicker
              v-model="timeRange"
              placeholder="选择自定义起止时间..."
              @change="onCustomRangeChange"
            />
          </div>
        </template>

        <el-button type="primary" :icon="Search" class="search-btn" @click="onSearch">
          查询明细
        </el-button>
      </div>
    </el-card>

    <template v-if="result">
      <!-- 汇总状态栏与统计指标 -->
      <div class="stat-summary-bar">
        <div class="stat-item main-var">
          <div class="stat-label">当前查询变量</div>
          <div class="stat-value font-mono">{{ result.variableName }}</div>
        </div>

        <div class="stat-divider"></div>

        <div class="stat-item">
          <div class="stat-label">时序记录总数</div>
          <div class="stat-value text-blue tabular-nums">{{ result.result.total.toLocaleString() }} <span class="unit">条</span></div>
        </div>

        <template v-if="numericStats">
          <div class="stat-divider"></div>
          <div class="stat-item">
            <div class="stat-label">当前页最小值</div>
            <div class="stat-value tabular-nums font-mono">{{ numericStats.min }}</div>
          </div>
          <div class="stat-divider"></div>
          <div class="stat-item">
            <div class="stat-label">当前页最大值</div>
            <div class="stat-value tabular-nums font-mono">{{ numericStats.max }}</div>
          </div>
          <div class="stat-divider"></div>
          <div class="stat-item">
            <div class="stat-label">当前页均值</div>
            <div class="stat-value text-emerald tabular-nums font-mono">{{ numericStats.avg }}</div>
          </div>
        </template>

        <div class="stat-divider"></div>

        <div class="stat-item range">
          <div class="stat-label">起止区间</div>
          <div class="stat-range font-mono">{{ formatTime(result.startTime) }} ~ {{ formatTime(result.endTime) }}</div>
        </div>
      </div>

      <!-- 时序折线图（当前页或抽样点） -->
      <el-card class="chart-card" v-if="chartDataAvailable">
        <template #header>
          <div class="chart-header-flex">
            <span>时序数值波动趋势</span>
            <span class="sub-tip">动态平滑曲线与数据点</span>
          </div>
        </template>
        <div ref="lineChartRef" class="line-chart-box"></div>
      </el-card>

      <!-- 明细表格卡片 -->
      <el-card class="table-card">
        <template #header>
          <div class="table-header-flex">
            <span>记录明细数据表</span>
            <span class="sub-tip">第 {{ page }} 页 / 共 {{ Math.ceil(result.result.total / pageSize) }} 页</span>
          </div>
        </template>

        <!-- 桌面端表格 -->
        <el-table
          v-if="!isMobile"
          :data="result.result.items"
          stripe
          height="450"
          class="history-table"
        >
          <el-table-column type="index" label="#" :index="indexBase" width="70" align="center" />
          
          <el-table-column prop="variableName" label="变量标识" min-width="180" show-overflow-tooltip>
            <template #default="{ row }">
              <el-tag size="small" effect="plain" type="info" class="font-mono">{{ row.variableName }}</el-tag>
            </template>
          </el-table-column>

          <el-table-column label="采集数值" min-width="200">
            <template #default="{ row }">
              <span class="val-tag font-mono tabular-nums">{{ formatValue(row.value) }}</span>
            </template>
          </el-table-column>

          <el-table-column label="采集时间 (UTC / Local)" width="220">
            <template #default="{ row }">
              <span class="time-text font-mono">{{ formatTime(row.time) }}</span>
            </template>
          </el-table-column>
        </el-table>

        <!-- 移动端卡片列表 -->
        <div v-else class="record-cards">
          <div v-for="(row, i) in result.result.items" :key="i" class="record-card">
            <div class="record-head">
              <span class="record-var font-mono">{{ row.variableName }}</span>
              <span class="record-index">#{{ indexBase(i) }}</span>
            </div>
            <div class="record-value font-mono tabular-nums">{{ formatValue(row.value) }}</div>
            <div class="record-time font-mono">{{ formatTime(row.time) }}</div>
          </div>
          <el-empty v-if="!result.result.items.length" description="暂无记录" />
        </div>

        <div class="pager-wrapper">
          <el-pagination
            v-model:current-page="page"
            v-model:page-size="pageSize"
            :total="result.result.total"
            :page-sizes="[20, 50, 100, 200]"
            :layout="isMobile ? 'prev, pager, next' : 'total, sizes, prev, pager, next, jumper'"
            :size="isMobile ? 'small' : 'default'"
            @current-change="load"
            @size-change="onSearch"
          />
        </div>
      </el-card>
    </template>

    <el-empty v-else-if="!loading" description="在上方输入变量名并点击查询以查看时序详情" />
  </div>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRoute } from 'vue-router'
import { Search } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import * as echarts from 'echarts'
import { getHistory, type HistoryResult } from '@/api/statistics'
import { getVariableSuggestions } from '@/api/variables'
import { useIsMobile } from '@/composables/useIsMobile'
import { registerPullRefresh } from '@/composables/pullRefresh'
import PeriodSegmented from '@/components/PeriodSegmented.vue'
import CustomDateRangePicker from '@/components/CustomDateRangePicker.vue'
import { computePeriodRange } from '@/utils/dateRange'

const { isMobile } = useIsMobile()
const route = useRoute()

const period = ref('day')
const variableName = ref('')
const timeRange = ref<[string, string] | null>(null)
const page = ref(1)
const pageSize = ref(50)
const loading = ref(false)
const result = ref<HistoryResult | null>(null)

const lineChartRef = ref<HTMLElement>()
let lineChart: echarts.ECharts | null = null
let ro: ResizeObserver | null = null
let unregisterPr: (() => void) | null = null

const indexBase = (i: number) => (page.value - 1) * pageSize.value + i + 1

const numericStats = computed(() => {
  if (!result.value?.result?.items?.length) return null
  const nums = result.value.result.items
    .map(it => Number(it.value))
    .filter(n => !isNaN(n))
  if (!nums.length) return null
  const min = Math.min(...nums)
  const max = Math.max(...nums)
  const sum = nums.reduce((a, b) => a + b, 0)
  const avg = Number((sum / nums.length).toFixed(2))
  return { min, max, avg }
})

const chartDataAvailable = computed(() => {
  return result.value && result.value.result.items.length > 1
})

function fmtLocal(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

function applyPeriod(p: string) {
  if (p === 'custom') {
    if (!timeRange.value) timeRange.value = null
  } else {
    timeRange.value = computePeriodRange(p)
  }
}

function onPeriodChange(p: string) {
  applyPeriod(p)
  if (variableName.value.trim()) load()
}

function onCustomRangeChange() {
  if (variableName.value.trim()) load()
}

async function querySearch(query: string, cb: (items: { value: string }[]) => void) {
  try {
    const names = await getVariableSuggestions(query)
    cb(names.map(n => ({ value: n })))
  } catch {
    cb([])
  }
}

function onSearch() {
  if (!variableName.value.trim()) {
    ElMessage.warning('请输入变量名')
    return
  }
  page.value = 1
  load()
}

async function load() {
  if (!variableName.value.trim()) return
  loading.value = true
  try {
    const [start, end] = timeRange.value ?? []
    result.value = await getHistory({
      variableName: variableName.value.trim(),
      start,
      end,
      page: page.value,
      pageSize: pageSize.value
    })
    await nextTick()
    renderLineChart()
  } finally {
    loading.value = false
  }
}

function renderLineChart() {
  if (!lineChartRef.value || !result.value?.result?.items?.length) return
  if (!lineChart) lineChart = echarts.init(lineChartRef.value)

  // 按时间升序绘制
  const items = [...result.value.result.items].sort((a, b) => new Date(a.time).getTime() - new Date(b.time).getTime())
  const times = items.map(it => formatTime(it.time).slice(11, 19))
  const values = items.map(it => {
    const num = Number(it.value)
    return isNaN(num) ? null : num
  })

  lineChart.setOption({
    tooltip: {
      trigger: 'axis',
      confine: true,
      axisPointer: { type: 'cross' },
      formatter: (params: any) => {
        const p = params[0]
        return `<div style="font-size:12px;color:#64748b;">时间: ${p.axisValue}</div>
                <div style="font-weight:700;color:#2563eb;margin-top:2px;">采集值: ${p.data ?? '-'}</div>`
      }
    },
    grid: { left: 16, right: 24, top: 20, bottom: 24, containLabel: true },
    xAxis: {
      type: 'category',
      data: times,
      axisLabel: { color: '#64748b', fontSize: 11, fontFamily: 'JetBrains Mono, monospace' },
      axisLine: { lineStyle: { color: '#cbd5e1' } }
    },
    yAxis: {
      type: 'value',
      axisLabel: { color: '#64748b', fontSize: 11, fontFamily: 'JetBrains Mono, monospace' },
      splitLine: { lineStyle: { color: '#f1f5f9' } }
    },
    series: [
      {
        name: '数值',
        type: 'line',
        smooth: true,
        showSymbol: items.length < 30,
        symbolSize: 6,
        data: values,
        itemStyle: { color: '#2563eb' },
        lineStyle: { width: 2.5, color: '#2563eb' },
        areaStyle: {
          color: new echarts.graphic.LinearGradient(0, 0, 0, 1, [
            { offset: 0, color: 'rgba(37, 99, 235, 0.25)' },
            { offset: 1, color: 'rgba(37, 99, 235, 0.01)' }
          ])
        }
      }
    ]
  }, true)
}

function onResize() {
  lineChart?.resize()
}

function formatValue(v: unknown) {
  if (v === null || v === undefined) return '-'
  if (typeof v === 'object') return JSON.stringify(v)
  return String(v)
}

function formatTime(iso: string) {
  return iso ? iso.replace('T', ' ').replace('Z', '').slice(0, 19) : '-'
}

onMounted(() => {
  applyPeriod(period.value)
  const fromVar = route.query.variable
  if (typeof fromVar === 'string' && fromVar.trim()) {
    variableName.value = fromVar.trim()
    load()
  }
  window.addEventListener('resize', onResize)
  if (lineChartRef.value && 'ResizeObserver' in window) {
    ro = new ResizeObserver(() => onResize())
    ro.observe(lineChartRef.value)
  }
  unregisterPr = registerPullRefresh(load)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  ro?.disconnect()
  ro = null
  lineChart?.dispose()
  lineChart = null
  unregisterPr?.()
})
</script>

<style scoped>
.history-page {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.filter-card {
  margin-bottom: 0;
}

.filter-toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.custom-range-slot {
  flex: 1 1 240px;
  max-width: 100%;
  min-width: 0;
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

.var-autocomplete {
  width: 280px;
}

.search-btn {
  padding: 8px 18px;
}

/* 统计条 */
.stat-summary-bar {
  display: flex;
  align-items: center;
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 12px;
  padding: 16px 24px;
  gap: 20px;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.04);
  flex-wrap: wrap;
}

.stat-item {
  display: flex;
  flex-direction: column;
}

.stat-item.main-var {
  min-width: 140px;
}

.stat-label {
  font-size: 12px;
  font-weight: 600;
  color: #64748b;
  margin-bottom: 4px;
}

.stat-value {
  font-size: 17px;
  font-weight: 700;
  color: #0f172a;
}

.stat-value .unit {
  font-size: 12px;
  color: #94a3b8;
  font-weight: 400;
}

.text-blue { color: #2563eb; }
.text-emerald { color: #059669; }

.stat-divider {
  width: 1px;
  height: 32px;
  background: #e2e8f0;
}

.stat-item.range {
  flex: 1;
}

.stat-range {
  font-size: 13px;
  color: #475569;
}

/* 图表卡片 */
.chart-card {
  margin-bottom: 0;
}

.chart-header-flex, .table-header-flex {
  display: flex;
  justify-content: space-between;
  align-items: center;
  font-size: 14px;
  font-weight: 600;
  color: #0f172a;
}

.sub-tip {
  font-size: 12px;
  font-weight: 400;
  color: #94a3b8;
}

.line-chart-box {
  height: 260px;
  width: 100%;
}

/* 明细表格 */
.val-tag {
  font-weight: 600;
  color: #0f172a;
  background: #f8fafc;
  padding: 3px 8px;
  border-radius: 4px;
  border: 1px solid #e2e8f0;
}

.time-text {
  font-size: 13px;
  color: #475569;
}

.pager-wrapper {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

/* 移动端记录卡片 */
.record-cards {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.record-card {
  background: #ffffff;
  border: 1px solid #e2e8f0;
  border-radius: 8px;
  padding: 12px;
}

.record-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 6px;
}

.record-var {
  font-size: 12px;
  color: #64748b;
  font-weight: 500;
}

.record-index {
  font-size: 11px;
  color: #94a3b8;
}

.record-value {
  font-size: 16px;
  font-weight: 700;
  color: #2563eb;
  margin-bottom: 4px;
}

.record-time {
  font-size: 12px;
  color: #64748b;
}

@media (max-width: 768px) {
  .filter-toolbar {
    flex-direction: column;
    align-items: stretch;
    gap: 8px;
  }

  .var-autocomplete {
    width: 100%;
  }

  .dt-full {
    width: 100%;
  }

  .search-btn {
    width: 100%;
  }

  .stat-summary-bar {
    display: grid;
    grid-template-columns: repeat(2, 1fr);
    gap: 10px;
    padding: 12px 14px;
  }

  .stat-item.main-var {
    grid-column: span 2;
  }

  .stat-item.range {
    grid-column: span 2;
    padding-top: 6px;
    border-top: 1px solid #f1f5f9;
    min-width: 0;
    overflow: hidden;
  }

  .stat-divider {
    display: none;
  }

  .stat-value {
    font-size: 15px;
  }

  .stat-range {
    font-size: 11px;
    word-break: break-all;
    white-space: normal;
    line-height: 1.4;
  }

  .line-chart-box {
    height: 220px;
  }

  .pager-wrapper {
    justify-content: center;
  }
}
</style>
<style>
@media (max-width: 768px) {
  .mobile-date-popper {
    max-width: calc(100vw - 16px);
  }
  .mobile-date-popper .el-picker-panel {
    max-width: calc(100vw - 16px);
  }
}
</style>
