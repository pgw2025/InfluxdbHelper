import { ref } from 'vue'

// 移动端断点：最大宽度 768px 视为移动设备（平板横屏仍走桌面布局）
const MOBILE_QUERY = '(max-width: 768px)'

// 单例响应式状态，所有组件共享同一份，避免重复监听
const isMobile = ref(false)
let initialized = false

function setup() {
  if (initialized || typeof window === 'undefined') return
  initialized = true
  const mql = window.matchMedia(MOBILE_QUERY)
  isMobile.value = mql.matches
  mql.addEventListener('change', e => {
    isMobile.value = e.matches
  })
}

/**
 * 判断当前是否为移动端视图。
 * 返回共享的响应式 ref，在 setup 阶段同步初始化（SPA 环境 window 必定存在），无首屏闪烁。
 */
export function useIsMobile() {
  setup()
  return { isMobile }
}
