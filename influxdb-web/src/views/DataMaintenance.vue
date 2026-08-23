<template>
  <div class="maintenance">
    <!-- 导出区 -->
    <el-card class="block" v-loading="exporting">
      <template #header>
        <span class="block-title">导出数据</span>
      </template>

      <div class="toolbar">
        <el-radio-group v-model="period" class="period-group" @change="onPeriodChange">
          <el-radio-button value="day">今日</el-radio-button>
          <el-radio-button value="yesterday">昨日</el-radio-button>
          <el-radio-button value="daybefore">前日</el-radio-button>
          <el-radio-button value="week">本周</el-radio-button>
          <el-radio-button value="month">本月</el-radio-button>
          <el-radio-button value="custom">自定义</el-radio-button>
        </el-radio-group>

        <el-autocomplete
          v-model="variableName"
          :fetch-suggestions="querySearch"
          placeholder="变量名（留空导出全部）"
          clearable
          class="var-input"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-autocomplete>

        <template v-if="period === 'custom'">
          <el-date-picker
            v-if="!isMobile"
            v-model="timeRange"
            type="datetimerange"
            range-separator="至"
            start-placeholder="开始时间"
            end-placeholder="结束时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
          />
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
        </template>

        <el-button type="primary" :icon="Download" :loading="exporting" @click="onExport">导出 CSV</el-button>
      </div>

      <el-alert type="info" :closable="false" class="hint">
        导出当前所选范围（及变量）的数据为 CSV 文件，可直接用 Excel 打开归档。
      </el-alert>
    </el-card>

    <!-- 删除区 -->
    <el-card class="block" v-loading="deleting">
      <template #header>
        <span class="block-title danger">删除数据</span>
      </template>

      <el-alert type="warning" :closable="false" class="hint">
        <template #title>
          删除为<strong>不可逆</strong>操作。系统会在删除前<strong>自动导出 CSV 并打包为 zip 备份</strong>到服务器备份目录，仍请谨慎。
        </template>
      </el-alert>

      <div class="toolbar">
        <el-autocomplete
          v-model="delVariableName"
          :fetch-suggestions="querySearch"
          placeholder="变量名（必填）"
          clearable
          class="var-input"
        >
          <template #prefix>
            <el-icon><Search /></el-icon>
          </template>
        </el-autocomplete>

        <el-date-picker
          v-if="!isMobile"
          v-model="delRange"
          type="datetimerange"
          range-separator="至"
          start-placeholder="开始时间"
          end-placeholder="结束时间"
          value-format="YYYY-MM-DDTHH:mm:ss"
        />
        <template v-else>
          <el-date-picker
            v-model="delStart"
            type="datetime"
            placeholder="开始时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            class="dt-full"
            :popper-class="datePopperClass"
          />
          <el-date-picker
            v-model="delEnd"
            type="datetime"
            placeholder="结束时间"
            value-format="YYYY-MM-DDTHH:mm:ss"
            class="dt-full"
            :popper-class="datePopperClass"
          />
        </template>

        <el-button type="danger" :icon="Delete" :loading="deleting" @click="onDelete">删除（将先自动备份）</el-button>
      </div>
    </el-card>

    <!-- 删除前预览与二次确认 -->
    <el-dialog
      v-model="previewVisible"
      title="删除前核对"
      width="720px"
      :close-on-click-modal="false"
      append-to-body
    >
      <div v-loading="previewLoading">
        <el-alert type="warning" :closable="false" class="hint">
          请确认以下正是你要删除的<strong>变量与数据</strong>。删除后系统会先自动备份，但仍不可恢复。
        </el-alert>

        <el-descriptions :column="isMobile ? 1 : 2" border class="preview-meta">
          <el-descriptions-item label="变量名">{{ preview?.dataName }}</el-descriptions-item>
          <el-descriptions-item label="时间范围">{{ delStartText }} ~ {{ delEndText }}</el-descriptions-item>
          <el-descriptions-item label="数据点总数">{{ preview?.pointCount }}</el-descriptions-item>
          <el-descriptions-item label="数据起止">
            {{ preview?.firstTime || '—' }} ~ {{ preview?.lastTime || '—' }}
          </el-descriptions-item>
        </el-descriptions>

        <div class="sample-toolbar">
          <span class="sample-title">抽样数据（共 {{ preview?.pointCount }} 条，显示前 {{ sortableSamples.length }} 条）</span>
          <el-radio-group v-model="sampleLimit" size="small" @change="onSampleLimitChange">
            <el-radio-button :value="20">20</el-radio-button>
            <el-radio-button :value="50">50</el-radio-button>
            <el-radio-button :value="100">100</el-radio-button>
          </el-radio-group>
        </div>

        <el-table :data="sortableSamples" border stripe class="sample-table" max-height="320">
          <el-table-column
            label="时间"
            prop="time"
            sortable
            :sort-method="sortByTime"
            min-width="180"
          />
          <el-table-column label="值" prop="value" min-width="120" />
        </el-table>

        <el-checkbox v-model="confirmChecked" class="confirm-check">
          我确认以上就是要删除的变量与数据
        </el-checkbox>
      </div>

      <template #footer>
        <el-button @click="previewVisible = false">取消</el-button>
        <el-button
          type="danger"
          :icon="Delete"
          :loading="deleting"
          :disabled="!confirmChecked"
          @click="onConfirmDelete"
        >
          确认删除
        </el-button>
      </template>
    </el-dialog>
  </div>
</template>

<script setup lang="ts">
import { computed, onMounted, ref } from 'vue'
import { Delete, Download, Search } from '@element-plus/icons-vue'
import { ElMessage } from 'element-plus'
import { useIsMobile } from '@/composables/useIsMobile'
import { getVariableSuggestions } from '@/api/variables'
import { exportCsv, deleteData, previewDelete, type VariablePreview } from '@/api/maintenance'

const { isMobile } = useIsMobile()

// 导出区
const period = ref('month')
const variableName = ref('')
const timeRange = ref<[string, string] | null>(null)
const startTime = ref('')
const endTime = ref('')
const exporting = ref(false)

// 删除区
const delVariableName = ref('')
const delRange = ref<[string, string] | null>(null)
const delStart = ref('')
const delEnd = ref('')
const deleting = ref(false)

// 删除前预览
const previewVisible = ref(false)
const previewLoading = ref(false)
const preview = ref<VariablePreview | null>(null)
const confirmChecked = ref(false)
const sampleLimit = ref(20)
const delStartText = ref('')
const delEndText = ref('')

const datePopperClass = computed(() => (isMobile.value ? 'mobile-date-popper' : ''))

function querySearch(query: string, cb: (items: { value: string }[]) => void) {
  getVariableSuggestions(query)
    .then(list => cb(list.map(v => ({ value: v }))))
    .catch(() => cb([]))
}

// 时段预设：计算本地时间起止
function computeRange(p: string): [string, string] {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  const fmt = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
  const startOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate(), 0, 0, 0)
  const endOfDay = (d: Date) => new Date(d.getFullYear(), d.getMonth(), d.getDate(), 23, 59, 59)

  switch (p) {
    case 'day': {
      const s = startOfDay(now)
      return [fmt(s), fmt(now)]
    }
    case 'yesterday': {
      const y = new Date(now); y.setDate(now.getDate() - 1)
      return [fmt(startOfDay(y)), fmt(endOfDay(y))]
    }
    case 'daybefore': {
      const y = new Date(now); y.setDate(now.getDate() - 2)
      return [fmt(startOfDay(y)), fmt(endOfDay(y))]
    }
    case 'week': {
      const day = now.getDay() || 7
      const mon = new Date(now); mon.setDate(now.getDate() - day + 1); mon.setHours(0, 0, 0, 0)
      return [fmt(mon), fmt(now)]
    }
    case 'month': {
      const m = new Date(now.getFullYear(), now.getMonth(), 1, 0, 0, 0)
      return [fmt(m), fmt(now)]
    }
    default:
      return [fmt(startOfDay(now)), fmt(now)]
  }
}

function applyPeriod() {
  if (period.value === 'custom') return
  const [s, e] = computeRange(period.value)
  timeRange.value = [s, e]
  startTime.value = s
  endTime.value = e
}

function onPeriodChange() {
  applyPeriod()
}

// 取导出/删除的起止（自定义优先用单独控件，其余用 timeRange）
function resolveRange(): [string, string] {
  if (period.value === 'custom') {
    if (isMobile.value) return [startTime.value, endTime.value]
    return timeRange.value ?? ['', '']
  }
  return timeRange.value ?? ['', '']
}

async function onExport() {
  const [s, e] = resolveRange()
  if (!s || !e) {
    ElMessage.warning('请选择时间范围')
    return
  }
  exporting.value = true
  try {
    await exportCsv({ start: s, stop: e, dataName: variableName.value || undefined })
    ElMessage.success('导出成功，文件已下载')
  } catch (err) {
    // 错误已由拦截器提示
  } finally {
    exporting.value = false
  }
}

// 抽样表格：后端按时间升序返回，这里支持按时间列排序（前端本地排序）
const sortableSamples = computed(() =>
  (preview.value?.samples ?? []).map(s => ({
    time: s.time ?? null,
    value: s.value ?? null
  }))
)

function sortByTime(a: { time: string | null }, b: { time: string | null }) {
  const ta = a.time ? new Date(a.time).getTime() : 0
  const tb = b.time ? new Date(b.time).getTime() : 0
  return ta - tb
}

async function onDelete() {
  // 1) 必填校验
  if (!delVariableName.value || !delVariableName.value.trim()) {
    ElMessage.warning('请指定要删除的变量名（不支持留空删除全部）')
    return
  }
  const [s, e] = isMobile.value ? [delStart.value, delEnd.value] : (delRange.value ?? ['', ''])
  if (!s || !e) {
    ElMessage.warning('请选择要删除的时间范围')
    return
  }

  // 2) 查询预览
  delStartText.value = s
  delEndText.value = e
  previewVisible.value = true
  previewLoading.value = true
  confirmChecked.value = false
  preview.value = null
  try {
    preview.value = await previewDelete({
      start: s,
      stop: e,
      dataName: delVariableName.value.trim(),
      sampleLimit: sampleLimit.value
    })
  } catch {
    previewVisible.value = false
    // 错误已由拦截器提示
  } finally {
    previewLoading.value = false
  }
}

// 调整抽样行数后重新查询预览
async function onSampleLimitChange() {
  if (!previewVisible.value || !delVariableName.value.trim()) return
  const [s, e] = isMobile.value ? [delStart.value, delEnd.value] : (delRange.value ?? ['', ''])
  if (!s || !e) return
  previewLoading.value = true
  try {
    preview.value = await previewDelete({
      start: s,
      stop: e,
      dataName: delVariableName.value.trim(),
      sampleLimit: sampleLimit.value
    })
  } catch {
    // 错误已由拦截器提示
  } finally {
    previewLoading.value = false
  }
}

// 3) 二次确认后真正删除
async function onConfirmDelete() {
  if (!confirmChecked.value) return
  const [s, e] = isMobile.value ? [delStart.value, delEnd.value] : (delRange.value ?? ['', ''])
  if (!s || !e || !delVariableName.value.trim()) return

  deleting.value = true
  try {
    const res = await deleteData({ start: s, stop: e, dataName: delVariableName.value.trim(), confirm: true })
    const backup = (res as { backupFile?: string })?.backupFile
    ElMessage.success(`删除完成，已备份至 ${backup ?? '服务器'}`)
    previewVisible.value = false
    confirmChecked.value = false
  } catch {
    // 错误已由拦截器提示
  } finally {
    deleting.value = false
  }
}

onMounted(() => {
  applyPeriod()
})
</script>

<style scoped>
.maintenance {
  display: flex;
  flex-direction: column;
  gap: 16px;
}

.block-title {
  font-weight: 600;
  font-size: 15px;
}

.block-title.danger {
  color: var(--el-color-danger);
}

.toolbar {
  display: flex;
  align-items: center;
  gap: 12px;
  flex-wrap: wrap;
}

.var-input {
  width: 280px;
}

.hint {
  margin-top: 12px;
}

/* 与统计页一致的分段控件外观 */
.period-group {
  display: inline-flex;
  padding: 3px;
  background: var(--el-fill-color-light);
  border-radius: 10px;
  gap: 2px;
}

.period-group :deep(.el-radio-button__inner) {
  border: none !important;
  box-shadow: none !important;
  background: transparent;
  border-radius: 8px;
  color: var(--el-text-color-secondary);
  transition: all 0.2s ease;
}

.period-group :deep(.el-radio-button.is-active .el-radio-button__inner) {
  background: #fff;
  color: var(--el-color-primary);
  font-weight: 600;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.12);
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

  .period-group {
    width: 100%;
  }

  .period-group :deep(.el-radio-button) {
    flex: 1 1 30%;
  }

  .period-group :deep(.el-radio-button__inner) {
    width: 100%;
    padding: 8px 4px;
  }

  .toolbar :deep(.el-button) {
    width: 100%;
  }
}

.preview-meta {
  margin-top: 12px;
}

.sample-toolbar {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin: 16px 0 8px;
  flex-wrap: wrap;
}

.sample-title {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

.sample-table {
  width: 100%;
}

.confirm-check {
  margin-top: 16px;
}

.confirm-check :deep(.el-checkbox__label) {
  font-weight: 600;
  color: var(--el-color-danger);
}
</style>
