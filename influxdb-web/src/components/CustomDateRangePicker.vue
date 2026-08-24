<template>
  <div class="custom-date-range-picker">
    <!-- 触发按钮卡片/输入展示框 -->
    <div
      class="range-trigger-btn"
      :class="{ 'has-value': hasValue }"
      role="button"
      tabindex="0"
      @click="openPicker"
    >
      <el-icon class="trigger-icon"><Calendar /></el-icon>
      <span class="trigger-label">
        <template v-if="hasValue">
          {{ displayRangeText }}
        </template>
        <template v-else>
          {{ placeholder || '选择自定义起止时间...' }}
        </template>
      </span>
      <el-icon v-if="hasValue" class="clear-icon" @click.stop="handleClear"><CircleClose /></el-icon>
      <el-icon v-else class="arrow-icon"><ArrowRight /></el-icon>
    </div>

    <!-- 移动端 / 桌面端 抽屉/弹窗 -->
    <el-drawer
      v-model="visible"
      :direction="isMobile ? 'btt' : 'rtl'"
      :size="isMobile ? 'auto' : '380px'"
      :with-header="false"
      class="custom-range-drawer"
      destroy-on-close
    >
      <div class="drawer-inner">
        <!-- 头部导航 -->
        <div class="drawer-header">
          <div class="header-left">
            <span class="drawer-title">设置时间范围</span>
            <span class="drawer-subtitle">选择自定义精确起止时间</span>
          </div>
          <el-button circle :icon="Close" size="small" class="close-btn" @click="visible = false" />
        </div>

        <!-- 精确时间选择 -->
        <div class="exact-section">
          <div class="section-title">
            <el-icon><Clock /></el-icon>
            <span>精确起止时间</span>
          </div>

          <div class="time-card start-card">
            <div class="time-card-head">
              <span class="time-tag start">开始时间</span>
              <button type="button" class="quick-link-btn" @click="setStartTodayBegin">今日 00:00</button>
            </div>
            <input
              type="datetime-local"
              class="native-datetime-input"
              :value="tempStartLocal"
              @input="onStartInput"
            />
          </div>

          <div class="time-card end-card">
            <div class="time-card-head">
              <span class="time-tag end">结束时间</span>
              <button type="button" class="quick-link-btn" @click="setEndNow">设为此时此刻</button>
            </div>
            <input
              type="datetime-local"
              class="native-datetime-input"
              :value="tempEndLocal"
              @input="onEndInput"
            />
          </div>
        </div>

        <!-- 底部确定/重置条 -->
        <div class="drawer-footer">
          <el-button class="reset-btn" @click="handleReset">重置</el-button>
          <el-button type="primary" class="confirm-btn" :disabled="!isRangeValid" @click="handleConfirm">
            确定应用
          </el-button>
        </div>
      </div>
    </el-drawer>
  </div>
</template>

<script setup lang="ts">
import { computed, ref, watch } from 'vue'
import { Calendar, CircleClose, ArrowRight, Close, Clock } from '@element-plus/icons-vue'
import { useIsMobile } from '@/composables/useIsMobile'

const props = defineProps<{
  modelValue?: [string, string] | null
  placeholder?: string
}>()

const emit = defineEmits<{
  (e: 'update:modelValue', val: [string, string] | null): void
  (e: 'change', val: [string, string] | null): void
}>()

const { isMobile } = useIsMobile()
const visible = ref(false)

const tempStart = ref<string>('')
const tempEnd = ref<string>('')

// 辅助时间格式化函数
function formatToIsoLocal(d: Date): string {
  const pad = (n: number) => String(n).padStart(2, '0')
  return `${d.getFullYear()}-${pad(d.getMonth() + 1)}-${pad(d.getDate())}T${pad(d.getHours())}:${pad(d.getMinutes())}:${pad(d.getSeconds())}`
}

function formatToInputVal(isoStr: string): string {
  if (!isoStr) return ''
  // html5 datetime-local 需要 YYYY-MM-DDTHH:mm
  return isoStr.substring(0, 16)
}

function parseInputVal(inputVal: string): string {
  if (!inputVal) return ''
  if (inputVal.length === 16) {
    return `${inputVal}:00`
  }
  return inputVal
}

const tempStartLocal = computed(() => formatToInputVal(tempStart.value))
const tempEndLocal = computed(() => formatToInputVal(tempEnd.value))

const hasValue = computed(() => {
  return Array.isArray(props.modelValue) && props.modelValue.length === 2 && Boolean(props.modelValue[0]) && Boolean(props.modelValue[1])
})

const displayRangeText = computed(() => {
  if (!hasValue.value || !props.modelValue) return ''
  const [s, e] = props.modelValue
  const formatShort = (str: string) => {
    if (str.length >= 16) {
      return `${str.substring(5, 10)} ${str.substring(11, 16)}`
    }
    return str
  }
  return `${formatShort(s)} ~ ${formatShort(e)}`
})

const isRangeValid = computed(() => {
  if (!tempStart.value || !tempEnd.value) return false
  return new Date(tempStart.value).getTime() <= new Date(tempEnd.value).getTime()
})

function openPicker() {
  if (hasValue.value && props.modelValue) {
    tempStart.value = props.modelValue[0]
    tempEnd.value = props.modelValue[1]
  } else {
    // 默认近24小时
    const now = new Date()
    const start = new Date(now.getTime() - 24 * 3600 * 1000)
    tempStart.value = formatToIsoLocal(start)
    tempEnd.value = formatToIsoLocal(now)
  }
  visible.value = true
}

function onStartInput(e: Event) {
  const val = (e.target as HTMLInputElement).value
  tempStart.value = parseInputVal(val)
}

function onEndInput(e: Event) {
  const val = (e.target as HTMLInputElement).value
  tempEnd.value = parseInputVal(val)
}

function setStartTodayBegin() {
  const now = new Date()
  const todayBegin = new Date(now.getFullYear(), now.getMonth(), now.getDate(), 0, 0, 0)
  tempStart.value = formatToIsoLocal(todayBegin)
}

function setEndNow() {
  tempEnd.value = formatToIsoLocal(new Date())
}

function handleReset() {
  const now = new Date()
  const start = new Date(now.getTime() - 24 * 3600 * 1000)
  tempStart.value = formatToIsoLocal(start)
  tempEnd.value = formatToIsoLocal(now)
}

function handleConfirm() {
  if (!isRangeValid.value) return
  const result: [string, string] = [tempStart.value, tempEnd.value]
  emit('update:modelValue', result)
  emit('change', result)
  visible.value = false
}

function handleClear() {
  emit('update:modelValue', null)
  emit('change', null)
}

watch(
  () => props.modelValue,
  val => {
    if (val && val.length === 2) {
      tempStart.value = val[0]
      tempEnd.value = val[1]
    }
  },
  { immediate: true }
)
</script>

<style scoped>
.custom-date-range-picker {
  width: 100%;
  max-width: 100%;
  box-sizing: border-box;
}

.range-trigger-btn {
  display: flex;
  align-items: center;
  gap: 8px;
  width: 100%;
  max-width: 100%;
  box-sizing: border-box;
  padding: 6px 12px;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 8px;
  color: #64748b;
  font-size: 13px;
  cursor: pointer;
  transition: all 0.2s ease;
  min-height: 38px;
  user-select: none;
  overflow: hidden;
}

.range-trigger-btn:hover {
  border-color: #94a3b8;
  background: #f8fafc;
}

.range-trigger-btn.has-value {
  border-color: #3b82f6;
  background: #eff6ff;
  color: #1e40af;
  font-weight: 500;
}

.trigger-icon {
  font-size: 15px;
  color: #3b82f6;
  flex-shrink: 0;
}

.trigger-label {
  flex: 1;
  min-width: 0;
  overflow: hidden;
  text-overflow: ellipsis;
  white-space: nowrap;
}

.clear-icon {
  font-size: 14px;
  color: #94a3b8;
  flex-shrink: 0;
  transition: color 0.15s ease;
}

.clear-icon:hover {
  color: #ef4444;
}

.arrow-icon {
  font-size: 13px;
  color: #94a3b8;
  flex-shrink: 0;
}

/* 抽屉样式 */
:deep(.custom-range-drawer) {
  border-top-left-radius: 18px !important;
  border-top-right-radius: 18px !important;
  overflow: hidden;
  box-shadow: 0 -8px 30px rgba(0, 0, 0, 0.15);
}

.drawer-inner {
  display: flex;
  flex-direction: column;
  height: 100%;
  padding: 16px 20px 24px;
  gap: 16px;
  background: #ffffff;
  overflow-y: auto;
}

.drawer-header {
  display: flex;
  align-items: flex-start;
  justify-content: space-between;
  padding-bottom: 12px;
  border-bottom: 1px solid #f1f5f9;
}

.header-left {
  display: flex;
  flex-direction: column;
  gap: 2px;
}

.drawer-title {
  font-size: 16px;
  font-weight: 700;
  color: #0f172a;
}

.drawer-subtitle {
  font-size: 12px;
  color: #64748b;
}

.close-btn {
  border: none;
  background: #f1f5f9;
}

/* 精确起止时间 */
.exact-section {
  display: flex;
  flex-direction: column;
  gap: 10px;
}

.section-title {
  display: flex;
  align-items: center;
  gap: 6px;
  font-size: 13px;
  font-weight: 600;
  color: #334155;
  margin-bottom: 2px;
}

.time-card {
  padding: 12px 14px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  border-radius: 10px;
  display: flex;
  flex-direction: column;
  gap: 8px;
}

.time-card-head {
  display: flex;
  align-items: center;
  justify-content: space-between;
}

.time-tag {
  font-size: 11px;
  font-weight: 700;
  padding: 2px 8px;
  border-radius: 6px;
  text-transform: uppercase;
}

.time-tag.start {
  background: #dbeafe;
  color: #1d4ed8;
}

.time-tag.end {
  background: #fef3c7;
  color: #b45309;
}

.quick-link-btn {
  border: none;
  background: transparent;
  color: #2563eb;
  font-size: 12px;
  font-weight: 500;
  cursor: pointer;
  padding: 2px 6px;
  border-radius: 4px;
}

.quick-link-btn:hover {
  background: #eff6ff;
}

.native-datetime-input {
  width: 100%;
  padding: 8px 10px;
  font-size: 14px;
  font-family: 'JetBrains Mono', -apple-system, BlinkMacSystemFont, monospace;
  color: #0f172a;
  background: #ffffff;
  border: 1px solid #cbd5e1;
  border-radius: 6px;
  outline: none;
  transition: border-color 0.15s ease;
  min-height: 38px;
}

.native-datetime-input:focus {
  border-color: #2563eb;
  box-shadow: 0 0 0 2px rgba(37, 99, 235, 0.15);
}

/* 底部操作 */
.drawer-footer {
  margin-top: auto;
  display: flex;
  align-items: center;
  gap: 12px;
  padding-top: 14px;
  border-top: 1px solid #f1f5f9;
}

.reset-btn {
  flex: 1;
  min-height: 42px;
}

.confirm-btn {
  flex: 2;
  min-height: 42px;
  font-weight: 600;
}
</style>
