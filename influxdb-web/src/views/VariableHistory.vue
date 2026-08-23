<template>
  <el-card v-loading="loading">
    <template #header>
      <div class="toolbar">
        <el-autocomplete
          v-model="variableName"
          :fetch-suggestions="querySearch"
          placeholder="输入变量名（支持联想）"
          clearable
          class="var-input"
          @select="onSearch"
          @keyup.enter="onSearch"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-autocomplete>

        <!-- 桌面端：日期时间范围一行搞定 -->
        <el-date-picker
          v-if="!isMobile"
          v-model="timeRange"
          type="datetimerange"
          range-separator="至"
          start-placeholder="开始时间"
          end-placeholder="结束时间"
          value-format="YYYY-MM-DDTHH:mm:ss"
        />
        <!-- 移动端：拆为两个独立日期时间选择器，避免双日历在窄屏难选 -->
        <template v-else>
          <el-date-picker
            v-model="startTime"
            type="datetime"
            placeholder="开始时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            class="dt-full"
            :popper-class="datePopperClass"
          />
          <el-date-picker
            v-model="endTime"
            type="datetime"
            placeholder="结束时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            class="dt-full"
            :popper-class="datePopperClass"
          />
        </template>

        <el-button type="primary" :icon="Search" @click="onSearch">查询</el-button>
      </div>
    </template>

    <template v-if="result">
      <el-alert type="info" :closable="false" class="range-alert">
        <template #title>
          变量 <b>{{ result.variableName }}</b> 共
          <b class="total-count">{{ result.result.total }}</b> 条记录
          <el-divider direction="vertical" />
          {{ formatTime(result.startTime) }} ~ {{ formatTime(result.endTime) }}
        </template>
      </el-alert>

      <!-- 桌面端：表格 -->
      <el-table v-if="!isMobile" :data="result.result.items" stripe border height="480">
        <el-table-column type="index" label="#" :index="indexBase" width="80" />
        <el-table-column prop="variableName" label="变量名" min-width="180" show-overflow-tooltip />
        <el-table-column label="值" min-width="200">
          <template #default="{ row }">{{ formatValue(row.value) }}</template>
        </el-table-column>
        <el-table-column label="时间" width="200">
          <template #default="{ row }">{{ formatTime(row.time) }}</template>
        </el-table-column>
      </el-table>

      <!-- 移动端：卡片列表 -->
      <div v-else class="record-cards">
        <div v-for="(row, i) in result.result.items" :key="i" class="record-card">
          <div class="record-head">
            <span class="record-var">{{ row.variableName }}</span>
            <span class="record-index">#{{ indexBase(i) }}</span>
          </div>
          <div class="record-value">{{ formatValue(row.value) }}</div>
          <div class="record-time">{{ formatTime(row.time) }}</div>
        </div>
        <el-empty v-if="!result.result.items.length" description="暂无记录" />
      </div>

      <div class="pager">
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
    </template>

    <el-empty v-else-if="!loading" description="输入变量名后点击查询" />
  </el-card>
</template>

<script setup lang="ts">
import { computed, onBeforeUnmount, onMounted, ref } from 'vue'
import { Search } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { getHistory, type HistoryResult } from '@/api/statistics'
import { getVariableSuggestions } from '@/api/variables'
import { useIsMobile } from '@/composables/useIsMobile'
import { registerPullRefresh } from '@/composables/pullRefresh'

const { isMobile } = useIsMobile()

const variableName = ref('')
const timeRange = ref<[string, string] | null>(null)
const page = ref(1)

// 移动端将范围拆成两个独立选择器
const datePopperClass = computed(() => (isMobile.value ? 'mobile-date-popper' : ''))
const startTime = computed<string>({
  get: () => timeRange.value?.[0] ?? '',
  set: (v) => {
    const cur = timeRange.value ?? ['', '']
    timeRange.value = [v, cur[1]]
  }
})
const endTime = computed<string>({
  get: () => timeRange.value?.[1] ?? '',
  set: (v) => {
    const cur = timeRange.value ?? ['', '']
    timeRange.value = [cur[0], v]
  }
})
const pageSize = ref(50)
const loading = ref(false)
const result = ref<HistoryResult | null>(null)

const indexBase = (i: number) => (page.value - 1) * pageSize.value + i + 1

let unregisterPr: (() => void) | null = null

onMounted(() => {
  unregisterPr = registerPullRefresh(load)
})

onBeforeUnmount(() => {
  unregisterPr?.()
})

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
  } finally {
    loading.value = false
  }
}

function formatValue(v: unknown) {
  if (v === null || v === undefined) return '-'
  if (typeof v === 'object') return JSON.stringify(v)
  return String(v)
}

function formatTime(iso: string) {
  return iso ? iso.replace('T', ' ').replace('Z', '').slice(0, 19) : '-'
}
</script>

<style scoped>
.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.var-input {
  width: 300px;
}

.range-alert {
  margin-bottom: 16px;
}

.total-count {
  font-size: 18px;
  color: var(--el-color-primary);
}

/* 移动端记录卡片列表 */
.record-cards {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.record-card {
  background: #fff;
  border: 1px solid var(--el-border-color-lighter);
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
  font-size: 13px;
  color: var(--el-text-color-secondary);
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.record-index {
  font-size: 12px;
  color: var(--el-text-color-placeholder);
}

.record-value {
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  word-break: break-all;
  margin-bottom: 4px;
}

.record-time {
  font-size: 12px;
  color: var(--el-text-color-secondary);
}

.pager {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}

@media (max-width: 768px) {
  .toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  .var-input {
    width: 100%;
  }

  .dt-full {
    width: 100%;
  }

  /* 查询按钮全宽 */
  .toolbar > .el-button {
    width: 100%;
  }

  .pager {
    justify-content: center;
  }
}
</style>

<!-- 日期面板 teleport 到 body，需全局样式约束窄屏不溢出视口 -->
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
