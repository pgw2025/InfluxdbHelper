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

        <el-date-picker
          v-model="timeRange"
          type="datetimerange"
          range-separator="至"
          start-placeholder="开始时间"
          end-placeholder="结束时间"
          value-format="YYYY-MM-DDTHH:mm:ss"
        />

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

      <el-table :data="result.result.items" stripe border height="480">
        <el-table-column type="index" label="#" :index="indexBase" width="80" />
        <el-table-column prop="variableName" label="变量名" min-width="180" show-overflow-tooltip />
        <el-table-column label="值" min-width="200">
          <template #default="{ row }">{{ formatValue(row.value) }}</template>
        </el-table-column>
        <el-table-column label="时间" width="200">
          <template #default="{ row }">{{ formatTime(row.time) }}</template>
        </el-table-column>
      </el-table>

      <div class="pager">
        <el-pagination
          v-model:current-page="page"
          v-model:page-size="pageSize"
          :total="result.result.total"
          :page-sizes="[20, 50, 100, 200]"
          layout="total, sizes, prev, pager, next, jumper"
          @current-change="load"
          @size-change="onSearch"
        />
      </div>
    </template>

    <el-empty v-else-if="!loading" description="输入变量名后点击查询" />
  </el-card>
</template>

<script setup lang="ts">
import { ref } from 'vue'
import { Search } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { getHistory, type HistoryResult } from '@/api/statistics'
import { getVariableSuggestions } from '@/api/variables'

const variableName = ref('')
const timeRange = ref<[string, string] | null>(null)
const page = ref(1)
const pageSize = ref(50)
const loading = ref(false)
const result = ref<HistoryResult | null>(null)

const indexBase = (i: number) => (page.value - 1) * pageSize.value + i + 1

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

.pager {
  display: flex;
  justify-content: flex-end;
  margin-top: 16px;
}
</style>
