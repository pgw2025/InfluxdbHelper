import type { Directive } from 'vue'
import { hasPullRefresh, triggerPullRefresh } from '@/composables/pullRefresh'

/**
 * v-pull-refresh：在滚动容器的顶部下拉时触发刷新（仅触摸设备、且滚动到顶时生效）。
 * 是否真正刷新由 pullRefresh 总线中是否有注册页决定，非列表页不会误触发。
 */
export const vPullRefresh: Directive<HTMLElement> = {
  mounted(el) {
    el.style.position = el.style.position || 'relative'

    const indicator = document.createElement('div')
    indicator.className = 'ptr-indicator'
    indicator.textContent = '下拉刷新'
    Object.assign(indicator.style, {
      position: 'absolute',
      top: '0',
      left: '0',
      right: '0',
      textAlign: 'center',
      fontSize: '13px',
      color: '#909399',
      padding: '8px 0',
      transform: 'translateY(-100%)',
      transition: 'transform 0.2s',
      zIndex: '10',
      pointerEvents: 'none',
      background: '#f5f7fa'
    })
    el.appendChild(indicator)

    const THRESHOLD = 60
    let startY = 0
    let pulling = false
    let distance = 0
    let refreshing = false

    const setTransform = (t: string) => {
      indicator.style.transform = t
    }

    const onTouchStart = (e: TouchEvent) => {
      if (refreshing || !hasPullRefresh()) return
      if (el.scrollTop <= 0) {
        startY = e.touches[0].clientY
        pulling = true
        indicator.style.transition = 'none'
      }
    }

    const onTouchMove = (e: TouchEvent) => {
      if (!pulling) return
      const dy = e.touches[0].clientY - startY
      if (dy <= 0) {
        distance = 0
        setTransform('translateY(-100%)')
        return
      }
      distance = Math.min(dy * 0.5, 120)
      setTransform(`translateY(${distance - 100}%)`)
      indicator.textContent = distance >= THRESHOLD ? '释放立即刷新' : '下拉刷新'
      // 已到顶部且继续下拉时阻止页面整体滚动
      if (el.scrollTop <= 0) e.preventDefault()
    }

    const onTouchEnd = async () => {
      if (!pulling) return
      pulling = false
      indicator.style.transition = 'transform 0.2s'
      if (distance >= THRESHOLD) {
        refreshing = true
        setTransform('translateY(0)')
        indicator.textContent = '刷新中…'
        try {
          await triggerPullRefresh()
        } finally {
          refreshing = false
          setTransform('translateY(-100%)')
          indicator.textContent = '下拉刷新'
        }
      } else {
        setTransform('translateY(-100%)')
      }
      distance = 0
    }

    ;(el as any)._ptr = { onTouchStart, onTouchMove, onTouchEnd }
    el.addEventListener('touchstart', onTouchStart, { passive: true })
    el.addEventListener('touchmove', onTouchMove, { passive: false })
    el.addEventListener('touchend', onTouchEnd)
    el.addEventListener('touchcancel', onTouchEnd)
  },
  unmounted(el) {
    const h = (el as any)._ptr
    if (h) {
      el.removeEventListener('touchstart', h.onTouchStart)
      el.removeEventListener('touchmove', h.onTouchMove)
      el.removeEventListener('touchend', h.onTouchEnd)
      el.removeEventListener('touchcancel', h.onTouchEnd)
      delete (el as any)._ptr
    }
  }
}
