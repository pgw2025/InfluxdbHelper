<template>
  <el-card v-loading="loading">
    <template #header>
      <div class="toolbar">
        <el-radio-group v-model="period" @change="load">
          <el-radio-button value="day">今日</el-radio-button>
          <el-radio-button value="yesterday">昨日</el-radio-button>
          <el-radio-button value="daybefore">前日</el-radio-button>
          <el-radio-button value="week">本周</el-radio-button>
          <el-radio-button value="month">本月</el-radio-button>
          <el-radio-button value="custom">自定义</el-radio-button>
        </el-radio-group>

        <template v-if="period === 'custom'">
          <el-date-picker
            v-model="customRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            @change="load"
          />
        </template>

        <el-button :icon="Refresh" circle class="refresh-btn" title="刷新" @click="load" />
      </div>
    </template>

    <template v-if="summary">
      <el-alert type="info" :closable="false" class="range-alert">
        <template #title>
          统计区间：{{ formatTime(summary.startTime) }} ~ {{ formatTime(summary.endTime) }}
          <el-divider direction="vertical" />
          总数据条数：
          <b class="total-count">{{ summary.total }}</b>
        </template>
      </el-alert>

      <el-row :gutter="16">
        <el-col :xs="24" :sm="24" :md="10">
          <h4 class="section-title">变量数据分布（Top 15）</h4>
          <!-- 桌面端：表格 -->
          <el-table v-if="!isMobile" class="stat-table" :data="topVariables" height="420" stripe @row-click="onRowClick">
            <el-table-column type="index" label="#" width="50" />
            <el-table-column label="变量名" min-width="180" show-overflow-tooltip>
            <template #default="{ row }">
              <span class="var-link">{{ row.variableName }}</span>
            </template>
          </el-table-column>
            <el-table-column prop="count" label="数据条数" width="120" sortable />
            <el-table-column label="占比" width="160">
              <template #default="{ row }">
                <el-progress
                  :percentage="percentage(row.count)"
                  :stroke-width="10"
                  :show-text="false"
                />
                <span class="pct-text">{{ percentage(row.count).toFixed(1) }}%</span>
              </template>
            </el-table-column>
          </el-table>
          <!-- 移动端：卡片列表 -->
          <div v-else class="var-cards">
            <div
              v-for="row in topVariables"
              :key="row.variableName"
              class="var-card"
              role="button"
              tabindex="0"
              @click="goVariable(row.variableName)"
              @keyup.enter="goVariable(row.variableName)"
            >
              <div class="var-card-head">
                <span class="var-name">{{ row.variableName }}</span>
                <span class="var-count">{{ row.count }}</span>
                <el-icon class="var-go"><ArrowRight /></el-icon>
              </div>
              <el-progress :percentage="percentage(row.count)" :stroke-width="8" :show-text="false" />
              <span class="var-pct">{{ percentage(row.count).toFixed(1) }}%</span>
            </div>
            <el-empty v-if="!topVariables.length" description="暂无数据" />
          </div>
        </el-col>

        <el-col :xs="24" :sm="24" :md="14">
          <h4 class="section-title">分布图表</h4>
          <div ref="chartRef" class="chart"></div>
        </el-col>
      </el-row>
    </template>

    <el-empty v-else-if="!loading" description="暂无数据" />
  </el-card>
</template>

<script setup lang="ts">
import { computed, nextTick, onBeforeUnmount, onMounted, ref } from 'vue'
import { useRouter } from 'vue-router'
import * as echarts from 'echarts'
import { ArrowRight, Refresh } from '@element-plus/icons-vue'
import { getSummary, type StatisticsSummary } from '@/api/statistics'
import { useIsMobile } from '@/composables/useIsMobile'
import { registerPullRefresh } from '@/composables/pullRefresh'

const { isMobile } = useIsMobile()

const router = useRouter()
function goVariable(name: string) {
  router.push({ name: 'history', query: { variable: name } })
}

// 桌面端点击整行跳转（行点击事件的 row 参数由 el-table 注入）
function onRowClick(row: { variableName: string }) {
  goVariable(row.variableName)
}

const period = ref('day')
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

function formatTime(iso: string) {
  return iso ? iso.replace('T', ' ').slice(0, 19) : '-'
}

// 图表左侧留白由 ECharts 根据标签宽度自适应（containLabel），避免窄屏裁掉变量名

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

  const vars = topVariables.value.slice().reverse()
  chart.setOption({
    tooltip: { trigger: 'axis', axisPointer: { type: 'shadow' } },
    grid: { left: 8, right: 16, top: 20, bottom: 20, containLabel: true },
    xAxis: { type: 'value', name: '条数' },
    yAxis: {
      type: 'category',
      data: vars.map(v => v.variableName),
      axisLabel: { width: 150, overflow: 'truncate' }
    },
    series: [
      {
        name: '数据条数',
        type: 'bar',
        data: vars.map(v => v.count),
        itemStyle: { color: '#409eff', borderRadius: [0, 4, 4, 0] },
        label: { show: true, position: 'right' }
      }
    ]
  })
}

// 容器尺寸变化时重绘（含 grid.left 自适应）
function onResize() {
  chart?.resize()
  renderChart()
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
.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

/* iOS 风格分段控制器：浅灰轨道 + 选中白底高亮 */
.toolbar :deep(.el-radio-group) {
  background: var(--el-fill-color-light);
  border-radius: 10px;
  padding: 3px;
  gap: 2px;
}

.toolbar :deep(.el-radio-button) {
  flex: 0 1 auto;
}

.toolbar :deep(.el-radio-button__inner) {
  border: none !important;
  background: transparent !important;
  box-shadow: none !important;
  border-radius: 8px !important;
  color: var(--el-text-color-regular);
  font-weight: 500;
  padding: 8px 16px;
  transition: background-color 0.2s, color 0.2s, box-shadow 0.2s;
}

.toolbar :deep(.el-radio-button__inner:hover) {
  color: var(--el-color-primary);
}

.toolbar :deep(.el-radio-button.is-active .el-radio-button__inner) {
  background: #fff !important;
  color: var(--el-color-primary) !important;
  box-shadow: 0 1px 4px rgba(0, 0, 0, 0.12) !important;
  border-radius: 8px !important;
}

/* 刷新按钮：圆形图标 + 触摸反馈 */
.refresh-btn {
  transition: transform 0.1s ease, box-shadow 0.2s ease;
}

.refresh-btn:active {
  transform: scale(0.92);
}

.range-alert {
  margin-bottom: 16px;
}

.total-count {
  font-size: 18px;
  color: var(--el-color-primary);
}

.section-title {
  margin: 4px 0 12px;
  font-size: 15px;
  color: var(--el-text-color-primary);
}

.chart {
  height: 420px;
  width: 100%;
}

.pct-text {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

/* 移动端卡片列表（替代横向滚动的表格） */
.var-cards {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.var-card {
  background: #fff;
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 10px 12px;
  cursor: pointer;
  transition: background-color 0.15s ease, box-shadow 0.15s ease, transform 0.05s ease;
}

.var-card:hover {
  box-shadow: 0 2px 10px rgba(0, 0, 0, 0.08);
}

.var-card:active {
  background: var(--el-fill-color-light);
  transform: scale(0.99);
}

.var-card-head {
  display: flex;
  justify-content: space-between;
  align-items: center;
  margin-bottom: 8px;
}

.var-name {
  font-size: 14px;
  color: var(--el-text-color-primary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

/* 变量名主色提示（桌面表格 / 移动卡片通用） */
.var-link {
  color: var(--el-color-primary);
}

.var-count {
  font-size: 14px;
  font-weight: 600;
  color: var(--el-color-primary);
}

/* 卡片右侧箭头，提示可点击跳转 */
.var-go {
  color: var(--el-text-color-placeholder);
  font-size: 16px;
  margin-left: 6px;
}

/* 桌面端整行可点击 */
.stat-table :deep(.el-table__row) {
  cursor: pointer;
}

.var-pct {
  display: block;
  margin-top: 4px;
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

@media (max-width: 768px) {
  .chart {
    height: 320px;
  }

  .section-title {
    margin-top: 12px;
  }

  .toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  /* 单选按钮组内部换行并均分，避免 6 个挤成一行溢出 */
  .toolbar :deep(.el-radio-group) {
    display: flex;
    flex-wrap: wrap;
    width: 100%;
  }

  .toolbar :deep(.el-radio-button) {
    flex: 1 1 30%;
    min-width: 0;
  }

  .toolbar :deep(.el-radio-button__inner) {
    width: 100%;
    padding-left: 8px;
    padding-right: 8px;
  }

  /* 自定义日期范围选择器全宽 */
  .toolbar :deep(.el-date-editor) {
    width: 100%;
  }

  .refresh-btn {
    align-self: flex-end;
    width: auto;
  }
}
</style>
