<template>
  <nav class="mobile-tabbar">
    <router-link
      v-for="t in tabs"
      :key="t.path"
      :to="t.path"
      class="tab-item"
      :class="{ active: route.path === t.path }"
    >
      <el-icon :size="22"><component :is="t.icon" /></el-icon>
      <span class="tab-label">{{ t.title }}</span>
    </router-link>
  </nav>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router'

const route = useRoute()

const tabs = [
  { path: '/dashboard', title: '状态', icon: 'Odometer' },
  { path: '/statistics', title: '统计', icon: 'DataAnalysis' },
  { path: '/history', title: '历史', icon: 'Clock' },
  { path: '/config', title: '配置', icon: 'Setting' },
  { path: '/maintenance', title: '维护', icon: 'Files' }
]
</script>

<style scoped>
.mobile-tabbar {
  position: fixed;
  left: 0;
  right: 0;
  bottom: 0;
  display: flex;
  height: 56px;
  /* 适配 iPhone 底部安全区 */
  padding-bottom: var(--safe-bottom);
  background: #fff;
  border-top: 1px solid var(--el-border-color-light);
  z-index: 2000;
  /* 防止被内容遮挡 */
  box-shadow: 0 -2px 8px rgba(0, 0, 0, 0.04);
}

.tab-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  color: var(--el-text-color-secondary);
  text-decoration: none;
  /* 触摸目标高度达标 */
  min-height: 44px;
}

.tab-item.active {
  color: var(--el-color-primary);
}

.tab-label {
  font-size: 12px;
  line-height: 1;
}
</style>
