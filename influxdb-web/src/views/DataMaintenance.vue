<template>
  <div class="maintenance">
    <!-- 导出区 -->
    <el-card class="block" v-loading="exporting">
      <template #header>
        <span class="block-title">导出数据</span>
      </template>

      <div class="toolbar">
        <PeriodSegmented
          v-model="period"
          :show-all-option="false"
          :show-year-option="false"
          @change="onPeriodChange"
        />

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
          <div class="custom-range-slot">
            <CustomDateRangePicker
              v-model="timeRange"
              placeholder="选择导出自定义起止时间..."
            />
          </div>
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

        <el-radio-group v-model="delPeriod" class="del-period mobile-scroll-pills" @change="onDelPeriodChange">
          <el-radio-button value="year">今年</el-radio-button>
          <el-radio-button value="all">全部</el-radio-button>
          <el-radio-button value="custom">自定义</el-radio-button>
        </el-radio-group>

        <template v-if="delPeriod === 'custom'">
          <div class="custom-range-slot">
            <CustomDateRangePicker
              v-model="delRange"
              placeholder="选择删除自定义起止时间..."
            />
          </div>
        </template>

        <el-button type="danger" :icon="Delete" :loading="deleting" @click="onDelete">删除（将先自动备份）</el-button>
      </div>
    </el-card>

    <!-- 导出/删除前预览与核对 -->
    <el-dialog
      v-model="previewVisible"
      :title="previewMode === 'export' ? '导出前核对' : '删除前核对'"
      :width="isMobile ? '92%' : '720px'"
      :fullscreen="isMobile"
      :close-on-click-modal="false"
      append-to-body
      class="preview-dialog"
    >
      <div v-loading="previewLoading">
        <el-alert
          :type="previewMode === 'export' ? 'info' : 'warning'"
          :closable="false"
          class="hint"
        >
          <template #title>
            <span v-if="previewMode === 'export'">
              请确认以下是要<strong>导出</strong>的变量与数据，核对无误后点击「导出 CSV」。
            </span>
            <span v-else>
              请确认以下正是你要<strong>删除</strong>的变量与数据。删除后系统会先自动备份，但仍不可恢复。
            </span>
          </template>
        </el-alert>

        <el-descriptions :column="isMobile ? 1 : 2" border class="preview-meta">
          <el-descriptions-item label="变量名">{{ preview?.dataName }}</el-descriptions-item>
          <el-descriptions-item label="时间范围">{{ previewStartText }} ~ {{ previewEndText }}</el-descriptions-item>
          <el-descriptions-item label="数据点总数">{{ preview?.pointCount }}</el-descriptions-item>
          <el-descriptions-item label="数据起止">
            {{ preview?.firstTime || '—' }} ~ {{ preview?.lastTime || '—' }}
          </el-descriptions-item>
        </el-descriptions>

        <div class="sample-toolbar">
          <span class="sample-title">抽样数据（共 {{ preview?.pointCount }} 条）</span>
          <div class="sort-controls">
            <span class="sort-label">排序</span>
            <el-radio-group :model-value="previewSortBy" size="small" @change="(v: 'time' | 'value') => changeSort(v)">
              <el-radio-button value="time">按时间</el-radio-button>
              <el-radio-button value="value">按值</el-radio-button>
            </el-radio-group>
            <el-button
              size="small"
              text
              @click="changeSort(previewSortBy)"
            >
              {{ previewSortDir === 'asc' ? '升序 ↑' : '降序 ↓' }}
            </el-button>
          </div>
        </div>

        <!-- 桌面端：表格 -->
        <el-table
          v-if="!isMobile"
          :data="sortableSamples"
          border
          stripe
          class="sample-table"
          max-height="320"
          :default-sort="sortState"
          @sort-change="onSortChange"
        >
          <el-table-column
            label="时间"
            prop="time"
            sortable="custom"
            min-width="180"
          />
          <el-table-column label="值" prop="value" sortable="custom" min-width="120" />
        </el-table>

        <!-- 移动端：卡片列表，避免横向滚动 -->
        <div v-else class="sample-list">
          <div v-for="(row, i) in sortableSamples" :key="i" class="sample-card">
            <div class="sample-row">
              <span class="sample-label">时间</span>
              <span class="sample-value">{{ row.time || '—' }}</span>
            </div>
            <div class="sample-row">
              <span class="sample-label">值</span>
              <span class="sample-value">{{ row.value ?? '—' }}</span>
            </div>
          </div>
        </div>

        <!-- 分页：浏览全部数据 -->
        <div class="preview-pager" v-if="preview && preview.totalPages > 1">
          <el-pagination
            layout="prev, pager, next"
            :current-page="previewPage"
            :page-size="previewPageSize"
            :total="preview.pointCount"
            :pager-count="5"
            @current-change="onPageChange"
          />
          <el-select v-model="previewPageSize" size="small" class="page-size" @change="onPageSizeChange">
            <el-option :value="20" label="20 条/页" />
            <el-option :value="50" label="50 条/页" />
            <el-option :value="100" label="100 条/页" />
          </el-select>
        </div>

        <el-checkbox v-if="previewMode === 'delete'" v-model="confirmChecked" class="confirm-check">
          我确认以上就是要删除的变量与数据
        </el-checkbox>
      </div>

      <template #footer>
        <div class="preview-footer">
          <el-button @click="previewVisible = false">取消</el-button>
          <el-button
            v-if="previewMode === 'export'"
            type="primary"
            :icon="Download"
            :loading="exporting"
            @click="onConfirmExport"
          >
            导出 CSV
          </el-button>
          <el-button
            v-else
            type="danger"
            :icon="Delete"
            :loading="deleting"
            :disabled="!confirmChecked"
            @click="onConfirmDelete"
          >
            确认删除
          </el-button>
        </div>
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
import PeriodSegmented from '@/components/PeriodSegmented.vue'
import CustomDateRangePicker from '@/components/CustomDateRangePicker.vue'
import { computePeriodRange } from '@/utils/dateRange'

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
const delPeriod = ref<'year' | 'all' | 'custom'>('year')
const delRange = ref<[string, string] | null>(null)
const delStart = ref('')
const delEnd = ref('')
const deleting = ref(false)

// 导出/删除前预览（共用）
const previewMode = ref<'export' | 'delete'>('delete')
const previewVisible = ref(false)
const previewLoading = ref(false)
const preview = ref<VariablePreview | null>(null)
const confirmChecked = ref(false)
const previewPage = ref(1)
const previewPageSize = ref(20)
const previewSortBy = ref<'time' | 'value'>('time')
const previewSortDir = ref<'asc' | 'desc'>('asc')
// 弹窗专用的来源副本（避免与导出/删除各自表单耦合）
const previewVarName = ref('')
const previewStart = ref('')
const previewEnd = ref('')
const previewStartText = ref('')
const previewEndText = ref('')

const datePopperClass = computed(() => (isMobile.value ? 'mobile-date-popper' : ''))

function querySearch(query: string, cb: (items: { value: string }[]) => void) {
  getVariableSuggestions(query)
    .then(list => cb(list.map(v => ({ value: v }))))
    .catch(() => cb([]))
}

function applyPeriod() {
  if (period.value === 'custom') return
  const [s, e] = computePeriodRange(period.value)
  timeRange.value = [s, e]
  startTime.value = s
  endTime.value = e
}

function onPeriodChange() {
  applyPeriod()
}

// 删除区快捷时段：今年 / 全部 / 自定义，选中后填充对应的时间范围
function applyDelPeriod() {
  const now = new Date()
  const pad = (n: number) => String(n).padStart(2, '0')
  const fmt = (d: Date) => `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
  if (delPeriod.value === 'year') {
    const s = new Date(now.getFullYear(), 0, 1, 0, 0, 0)
    delRange.value = [fmt(s), fmt(now)]
    delStart.value = fmt(s)
    delEnd.value = fmt(now)
  } else if (delPeriod.value === 'all') {
    const s = new Date(2000, 0, 1, 0, 0, 0)
    delRange.value = [fmt(s), fmt(now)]
    delStart.value = fmt(s)
    delEnd.value = fmt(now)
  }
  // custom：保留手选，不自动填充
}

function onDelPeriodChange() {
  applyDelPeriod()
}

// 取导出/删除的起止
function resolveRange(): [string, string] {
  return timeRange.value ?? ['', '']
}

async function onExport() {
  const [s, e] = resolveRange()
  if (!s || !e) {
    ElMessage.warning('请选择时间范围')
    return
  }
  // 导出允许变量名留空（导出全部）；先走预览弹窗核对
  openPreview('export', variableName.value.trim(), s, e)
}

// 当前页抽样数据（后端已排序分页，前端仅展示）
const sortableSamples = computed(() =>
  (preview.value?.samples ?? []).map(s => ({
    time: s.time ?? null,
    value: s.value ?? null
  }))
)

// 当前排序表头状态（用于桌面端 el-table 高亮显示）
const sortState = computed(() => {
  if (!preview.value) return {}
  return { prop: preview.value.sortBy, order: preview.value.sortDir === 'desc' ? 'descending' : 'ascending' }
})

// 打开预览弹窗并查询（导出/删除共用）
async function openPreview(mode: 'export' | 'delete', dataName: string, start: string, stop: string) {
  previewMode.value = mode
  previewVarName.value = dataName
  previewStart.value = start
  previewEnd.value = stop
  previewStartText.value = start
  previewEndText.value = stop
  previewPage.value = 1
  previewSortBy.value = 'time'
  previewSortDir.value = 'asc'
  previewVisible.value = true
  previewLoading.value = true
  confirmChecked.value = false
  preview.value = null
  await fetchPreview()
}

// 统一的预览查询（受控排序 + 分页），使用弹窗来源副本参数
async function fetchPreview() {
  const s = previewStart.value
  const e = previewEnd.value
  if (!s || !e) return
  previewLoading.value = true
  try {
    preview.value = await previewDelete({
      start: s,
      stop: e,
      dataName: previewVarName.value || undefined,
      page: previewPage.value,
      pageSize: previewPageSize.value,
      sortBy: previewSortBy.value,
      sortDir: previewSortDir.value
    })
  } catch {
    // 错误已由拦截器提示
  } finally {
    previewLoading.value = false
  }
}

async function onDelete() {
  // 0) 确保快捷时段（今年/全部）已填充范围
  applyDelPeriod()
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

  // 2) 查询预览（删除模式）
  openPreview('delete', delVariableName.value.trim(), s, e)
}

// 预览核对通过后执行导出
async function onConfirmExport() {
  const s = previewStart.value
  const e = previewEnd.value
  if (!s || !e) return
  exporting.value = true
  try {
    await exportCsv({ start: s, stop: e, dataName: previewVarName.value || undefined })
    ElMessage.success('导出成功，文件已下载')
    previewVisible.value = false
  } catch {
    // 错误已由拦截器提示
  } finally {
    exporting.value = false
  }
}

// 切换排序字段/方向后重新查询
function changeSort(by: 'time' | 'value') {
  if (previewSortBy.value === by) {
    previewSortDir.value = previewSortDir.value === 'asc' ? 'desc' : 'asc'
  } else {
    previewSortBy.value = by
    previewSortDir.value = 'asc'
  }
  previewPage.value = 1
  fetchPreview()
}

// 桌面端 el-table 表头点击排序（受控，触发服务端查询）
function onSortChange({ prop, order }: { prop: string; order: string | null }) {
  if (!prop) return
  previewSortBy.value = prop as 'time' | 'value'
  previewSortDir.value = order === 'descending' ? 'desc' : 'asc'
  previewPage.value = 1
  fetchPreview()
}

// 每页条数变化
function onPageSizeChange() {
  previewPage.value = 1
  fetchPreview()
}

// 翻页
function onPageChange(p: number) {
  previewPage.value = p
  fetchPreview()
}

// 3) 二次确认后真正删除
async function onConfirmDelete() {
  if (!confirmChecked.value) return
  const [s, e] = delRange.value ?? ['', '']
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
  applyDelPeriod()
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

.custom-range-slot {
  flex: 1 1 240px;
  max-width: 100%;
  min-width: 0;
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

/* 移动端：删除预览卡片列表（替代易横向滚动的表格） */
.sample-list {
  display: flex;
  flex-direction: column;
  gap: 8px;
  max-height: 50vh;
  overflow-y: auto;
  -webkit-overflow-scrolling: touch;
}

.sample-card {
  border: 1px solid var(--el-border-color-lighter);
  border-radius: 8px;
  padding: 8px 10px;
  background: var(--el-fill-color-blank);
}

.sample-row {
  display: flex;
  align-items: baseline;
  gap: 8px;
  padding: 2px 0;
}

.sample-label {
  flex: 0 0 36px;
  color: var(--el-text-color-secondary);
  font-size: 12px;
}

.sample-value {
  flex: 1 1 auto;
  word-break: break-all;
  font-size: 13px;
}

/* 弹窗全屏（移动端）时：让内容区可滚动、底部按钮更易点 */
.preview-dialog :deep(.el-dialog) {
  display: flex;
  flex-direction: column;
  max-height: 92vh;
}

.preview-dialog :deep(.el-dialog__body) {
  flex: 1 1 auto;
  overflow-y: auto;
}

.preview-dialog :deep(.el-dialog__footer) {
  padding: 12px 16px;
}

.preview-footer {
  display: flex;
  gap: 12px;
}

.preview-footer .el-button {
  flex: 1 1 0;
}

/* 排序控件 */
.sort-controls {
  display: flex;
  align-items: center;
  gap: 8px;
  flex-wrap: wrap;
}

.sort-label {
  font-size: 13px;
  color: var(--el-text-color-secondary);
}

/* 分页 */
.preview-pager {
  display: flex;
  align-items: center;
  justify-content: space-between;
  gap: 12px;
  margin-top: 12px;
  flex-wrap: wrap;
}

.preview-pager :deep(.el-pagination) {
  flex-wrap: wrap;
}

.page-size {
  width: 110px;
}

@media (max-width: 768px) {
  .sample-toolbar {
    flex-direction: column;
    align-items: stretch;
  }

  .sort-controls {
    justify-content: space-between;
  }

  .preview-pager {
    flex-direction: column;
    align-items: stretch;
  }

  .preview-pager :deep(.el-pagination) {
    justify-content: center;
  }

  .page-size {
    width: 100%;
  }
}
</style>
