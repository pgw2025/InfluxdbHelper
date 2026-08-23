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

        <el-button :icon="Refresh" circle @click="load" />
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
        <el-col :span="10">
          <h4 class="section-title">变量数据分布（Top 15）</h4>
          <el-table :data="topVariables" height="420" stripe>
            <el-table-column type="index" label="#" width="50" />
            <el-table-column prop="variableName" label="变量名" min-width="180" show-overflow-tooltip />
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
        </el-col>

        <el-col :span="14">
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
import * as echarts from 'echarts'
import { Refresh } from '@element-plus/icons-vue'
import { getSummary, type StatisticsSummary } from '@/api/statistics'

const period = ref('day')
const customRange = ref<[string, string] | null>(null)
const summary = ref<StatisticsSummary | null>(null)
const loading = ref(false)

const chartRef = ref<HTMLElement>()
let chart: echarts.ECharts | null = null

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

async function load() {
  if (period.value === 'custom') {
    if (!customRange.value) return
  }
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
    grid: { left: 160, right: 40, top: 20, bottom: 30 },
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

function onResize() {
  chart?.resize()
}

onMounted(() => {
  load()
  window.addEventListener('resize', onResize)
})

onBeforeUnmount(() => {
  window.removeEventListener('resize', onResize)
  chart?.dispose()
  chart = null
})
</script>

<style scoped>
.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
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
</style>
