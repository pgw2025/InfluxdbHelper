<template>
  <div class="period-segmented-control">
    <div class="segmented-track">
      <!-- 1. 小时维度快捷触发 -->
      <el-dropdown trigger="click" @command="handleCommand">
        <button
          type="button"
          class="seg-item"
          :class="{ active: isHourActive }"
        >
          <span class="seg-text">{{ currentHourLabel }}</span>
          <el-icon class="seg-arrow"><ArrowDown /></el-icon>
        </button>
        <template #dropdown>
          <el-dropdown-menu class="period-dropdown-menu">
            <el-dropdown-item
              v-for="opt in hourOptions"
              :key="opt.value"
              :command="opt.value"
              :class="{ 'is-selected': modelValue === opt.value }"
            >
              <div class="menu-item-content">
                <span>{{ opt.label }}</span>
                <el-icon v-if="modelValue === opt.value" class="check-icon"><Check /></el-icon>
              </div>
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>

      <!-- 2. 单日/天数维度快捷触发 -->
      <el-dropdown trigger="click" @command="handleCommand">
        <button
          type="button"
          class="seg-item"
          :class="{ active: isDayActive }"
        >
          <span class="seg-text">{{ currentDayLabel }}</span>
          <el-icon class="seg-arrow"><ArrowDown /></el-icon>
        </button>
        <template #dropdown>
          <el-dropdown-menu class="period-dropdown-menu">
            <el-dropdown-item
              v-for="opt in dayOptions"
              :key="opt.value"
              :command="opt.value"
              :class="{ 'is-selected': modelValue === opt.value }"
            >
              <div class="menu-item-content">
                <span>{{ opt.label }}</span>
                <el-icon v-if="modelValue === opt.value" class="check-icon"><Check /></el-icon>
              </div>
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>

      <!-- 3. 宏观周期维度快捷触发 -->
      <el-dropdown trigger="click" @command="handleCommand">
        <button
          type="button"
          class="seg-item"
          :class="{ active: isCycleActive }"
        >
          <span class="seg-text">{{ currentCycleLabel }}</span>
          <el-icon class="seg-arrow"><ArrowDown /></el-icon>
        </button>
        <template #dropdown>
          <el-dropdown-menu class="period-dropdown-menu">
            <el-dropdown-item
              v-for="opt in cycleOptions"
              :key="opt.value"
              :command="opt.value"
              :class="{ 'is-selected': modelValue === opt.value }"
            >
              <div class="menu-item-content">
                <span>{{ opt.label }}</span>
                <el-icon v-if="modelValue === opt.value" class="check-icon"><Check /></el-icon>
              </div>
            </el-dropdown-item>
          </el-dropdown-menu>
        </template>
      </el-dropdown>

      <!-- 4. 自定义时间范围快捷触发 -->
      <button
        type="button"
        class="seg-item custom-seg-btn"
        :class="{ active: modelValue === 'custom' }"
        @click="handleCustomClick"
      >
        <el-icon class="cal-icon"><Calendar /></el-icon>
        <span class="seg-text">自定义</span>
      </button>
    </div>
  </div>
</template>

<script setup lang="ts">
import { computed } from 'vue'
import { ArrowDown, Check, Calendar } from '@element-plus/icons-vue'

interface OptionItem {
  label: string
  shortLabel: string
  value: string
}

const props = withDefaults(
  defineProps<{
    modelValue: string
    showAllOption?: boolean
    showYearOption?: boolean
  }>(),
  {
    showAllOption: true,
    showYearOption: true
  }
)

const emit = defineEmits<{
  (e: 'update:modelValue', val: string): void
  (e: 'change', val: string): void
}>()

// 1. 小时维度选项（1小时、6小时、12小时、24小时）
const hourOptions: OptionItem[] = [
  { label: '近 1 小时', shortLabel: '1小时', value: '1h' },
  { label: '近 6 小时', shortLabel: '6小时', value: '6h' },
  { label: '近 12 小时', shortLabel: '12小时', value: '12h' },
  { label: '近 24 小时', shortLabel: '24小时', value: '24h' }
]

// 2. 单日/天数维度选项（今日、昨日、前日、3天、7天、30天）
const dayOptions: OptionItem[] = [
  { label: '今日 (Today)', shortLabel: '今日', value: 'day' },
  { label: '昨日 (Yesterday)', shortLabel: '昨日', value: 'yesterday' },
  { label: '前日 (Day Before)', shortLabel: '前日', value: 'daybefore' },
  { label: '近 3 天 (3 Days)', shortLabel: '近3天', value: '3d' },
  { label: '近 7 天 (7 Days)', shortLabel: '近7天', value: '7d' },
  { label: '近 30 天 (30 Days)', shortLabel: '近30天', value: '30d' }
]

// 3. 宏观周期维度选项（本周、本月、今年、全部）
const cycleOptions = computed<OptionItem[]>(() => {
  const list: OptionItem[] = [
    { label: '本周 (Week)', shortLabel: '本周', value: 'week' },
    { label: '本月 (Month)', shortLabel: '本月', value: 'month' }
  ]
  if (props.showYearOption) {
    list.push({ label: '今年 (Year)', shortLabel: '今年', value: 'year' })
  }
  if (props.showAllOption) {
    list.push({ label: '全部 (All)', shortLabel: '全部', value: 'all' })
  }
  return list
})

const isHourActive = computed(() => {
  return hourOptions.some(h => h.value === props.modelValue)
})

const isDayActive = computed(() => {
  return dayOptions.some(d => d.value === props.modelValue)
})

const isCycleActive = computed(() => {
  return cycleOptions.value.some(c => c.value === props.modelValue)
})

const currentHourLabel = computed(() => {
  const match = hourOptions.find(h => h.value === props.modelValue)
  return match ? match.shortLabel : '小时'
})

const currentDayLabel = computed(() => {
  const match = dayOptions.find(d => d.value === props.modelValue)
  return match ? match.shortLabel : '天数'
})

const currentCycleLabel = computed(() => {
  const match = cycleOptions.value.find(c => c.value === props.modelValue)
  return match ? match.shortLabel : '周期'
})

function handleCommand(val: string) {
  emit('update:modelValue', val)
  emit('change', val)
}

function handleCustomClick() {
  emit('update:modelValue', 'custom')
  emit('change', 'custom')
}
</script>

<style scoped>
.period-segmented-control {
  display: inline-flex;
  user-select: none;
  -webkit-user-select: none;
  max-width: 100%;
}

.segmented-track {
  display: flex;
  align-items: center;
  background: #f1f5f9;
  padding: 3px;
  border-radius: 9px;
  border: 1px solid #e2e8f0;
  gap: 2px;
  width: 100%;
}

.seg-item {
  display: inline-flex;
  align-items: center;
  justify-content: center;
  gap: 3px;
  padding: 6px 10px;
  border-radius: 7px;
  border: none;
  background: transparent;
  color: #64748b;
  font-size: 13px;
  font-weight: 500;
  cursor: pointer;
  outline: none;
  transition: all 0.15s ease;
  min-height: 32px;
  white-space: nowrap;
}

.seg-item:hover {
  color: #1e293b;
}

.seg-item:active {
  transform: scale(0.96);
}

.seg-item.active {
  background: #ffffff;
  color: #2563eb;
  font-weight: 600;
  box-shadow: 0 1px 3px rgba(0, 0, 0, 0.08);
}

.seg-arrow {
  font-size: 10px;
  transition: transform 0.2s ease;
}

.seg-item.active .seg-arrow {
  color: #2563eb;
}

.cal-icon {
  font-size: 13px;
}

.custom-seg-btn {
  padding: 6px 10px;
}

.menu-item-content {
  display: flex;
  align-items: center;
  justify-content: space-between;
  width: 100%;
  min-width: 110px;
  gap: 16px;
  font-size: 13px;
}

.check-icon {
  color: #2563eb;
  font-weight: bold;
}

:deep(.is-selected) {
  color: #2563eb !important;
  background-color: #eff6ff !important;
  font-weight: 600;
}

@media (max-width: 768px) {
  .period-segmented-control {
    width: 100%;
  }

  .segmented-track {
    width: 100%;
    display: grid;
    grid-template-columns: repeat(4, 1fr);
    padding: 3px;
    gap: 2px;
  }

  .seg-item {
    width: 100%;
    padding: 6px 2px;
    font-size: 12px;
    min-height: 34px;
    gap: 2px;
  }

  .seg-text {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
  }
}
</style>
