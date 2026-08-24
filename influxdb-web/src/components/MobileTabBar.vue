<template>
  <nav class="mobile-tabbar">
    <router-link
      v-for="t in tabs"
      :key="t.path"
      :to="t.path"
      class="tab-item"
      :class="{ active: route.path === t.path }"
    >
      <div class="tab-icon-wrapper">
        <el-icon :size="20"><component :is="t.icon" /></el-icon>
      </div>
      <span class="tab-label">{{ t.title }}</span>
    </router-link>
  </nav>
</template>

<script setup lang="ts">
import { useRoute } from 'vue-router'
import {
  Odometer,
  DataAnalysis,
  Clock,
  Files,
  Setting
} from '@element-plus/icons-vue'

const route = useRoute()

const tabs = [
  { path: '/dashboard', title: '概览', icon: Odometer },
  { path: '/statistics', title: '统计', icon: DataAnalysis },
  { path: '/history', title: '历史', icon: Clock },
  { path: '/maintenance', title: '维护', icon: Files },
  { path: '/config', title: '配置', icon: Setting }
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
  padding-bottom: env(safe-area-inset-bottom, 0px);
  background: rgba(255, 255, 255, 0.92);
  backdrop-filter: blur(16px);
  -webkit-backdrop-filter: blur(16px);
  border-top: 1px solid rgba(226, 232, 240, 0.8);
  z-index: 2000;
  box-shadow: 0 -2px 10px rgba(0, 0, 0, 0.04);
  user-select: none;
  -webkit-user-select: none;
}

.tab-item {
  flex: 1;
  display: flex;
  flex-direction: column;
  align-items: center;
  justify-content: center;
  gap: 2px;
  color: #64748b;
  text-decoration: none;
  min-height: 50px;
  transition: all 0.2s cubic-bezier(0.4, 0, 0.2, 1);
  position: relative;
  touch-action: manipulation;
}

.tab-item:active {
  transform: scale(0.92);
}

.tab-icon-wrapper {
  display: flex;
  align-items: center;
  justify-content: center;
  width: 36px;
  height: 26px;
  border-radius: 14px;
  transition: all 0.2s ease;
}

.tab-item.active {
  color: #2563eb;
}

.tab-item.active .tab-icon-wrapper {
  background: #eff6ff;
  color: #2563eb;
}

.tab-label {
  font-size: 11px;
  font-weight: 500;
  line-height: 1.2;
  letter-spacing: -0.1px;
}

.tab-item.active .tab-label {
  font-weight: 600;
}
</style>
