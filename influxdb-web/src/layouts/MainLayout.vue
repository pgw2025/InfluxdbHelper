<template>
  <el-container class="layout">
    <!-- 桌面端侧边栏 -->
    <el-aside v-show="!isMobile" :width="isCollapse ? '72px' : '230px'" class="layout-aside">
      <div class="logo">
        <div class="logo-icon-box">
          <el-icon :size="20" color="#ffffff"><DataLine /></el-icon>
        </div>
        <div v-show="!isCollapse" class="logo-info">
          <span class="logo-text">InfluxDB Helper</span>
          <span class="logo-sub">时序数据统计平台</span>
        </div>
      </div>

      <el-menu
        :default-active="route.path"
        :collapse="isCollapse"
        :collapse-transition="false"
        router
        class="layout-menu"
      >
        <el-menu-item v-for="item in menus" :key="item.path" :index="item.path">
          <el-icon class="menu-icon"><component :is="item.icon" /></el-icon>
          <template #title>
            <span class="menu-title">{{ item.title }}</span>
          </template>
        </el-menu-item>
      </el-menu>

      <!-- 侧边栏底部状态卡片 -->
      <div v-show="!isCollapse" class="aside-footer">
        <div class="server-status-badge">
          <span class="status-dot online"></span>
          <div class="status-info">
            <span class="status-title">InfluxDB 引擎</span>
            <span class="status-state">运行正常</span>
          </div>
        </div>
      </div>
    </el-aside>

    <el-container class="layout-body">
      <el-header class="layout-header">
        <div class="header-left">
          <button
            v-if="!isMobile"
            class="collapse-btn-box"
            :title="isCollapse ? '展开菜单' : '折叠菜单'"
            @click="isCollapse = !isCollapse"
          >
            <el-icon :size="18">
              <Expand v-if="isCollapse" />
              <Fold v-else />
            </el-icon>
          </button>
          
          <div class="page-title-group">
            <h1 class="page-title">{{ route.meta.title || '控制台' }}</h1>
          </div>
        </div>

        <div class="header-right">
          <div class="header-badge-tag" v-if="!isMobile">
            <span class="status-dot online"></span>
            <span>已连接 v2.7</span>
          </div>

          <el-dropdown @command="onCommand">
            <div class="user-pill">
              <div class="user-avatar">
                <el-icon :size="15"><User /></el-icon>
              </div>
              <span class="user-name">{{ auth.displayName || '管理员' }}</span>
              <el-icon class="user-arrow"><ArrowDown /></el-icon>
            </div>
            <template #dropdown>
              <el-dropdown-menu class="custom-dropdown">
                <el-dropdown-item command="config">
                  <el-icon><Setting /></el-icon> 系统配置
                </el-dropdown-item>
                <el-dropdown-item divided command="logout">
                  <el-icon><SwitchButton /></el-icon> 退出登录
                </el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <el-main class="layout-main" v-pull-refresh>
        <div class="main-content-wrapper">
          <router-view v-slot="{ Component }">
            <transition name="fade-transform" mode="out-in">
              <component :is="Component" />
            </transition>
          </router-view>
        </div>
      </el-main>
    </el-container>

    <!-- 移动端底部导航 Tab 栏 -->
    <MobileTabBar v-if="isMobile" />
  </el-container>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import {
  DataLine,
  Fold,
  Expand,
  User,
  ArrowDown,
  Setting,
  SwitchButton
} from '@element-plus/icons-vue'
import { useAuthStore } from '@/stores/auth'
import { useIsMobile } from '@/composables/useIsMobile'
import MobileTabBar from '@/components/MobileTabBar.vue'
import { vPullRefresh } from '@/directives/pullRefresh'

const route = useRoute()
const router = useRouter()
const auth = useAuthStore()
const { isMobile } = useIsMobile()
const isCollapse = ref(false)

const menus = computed(() =>
  router
    .getRoutes()
    .filter(r => r.meta?.title && r.path !== '/login' && r.path.split('/').length <= 2)
    .map(r => ({ path: r.path, title: r.meta.title as string, icon: r.meta.icon as string }))
)

function onCommand(cmd: string) {
  if (cmd === 'logout') {
    auth.logout()
    router.push('/login')
  } else if (cmd === 'config') {
    router.push('/config')
  }
}
</script>

<style scoped>
.layout {
  height: 100vh;
  display: flex;
}

.layout-aside {
  display: flex;
  flex-direction: column;
  background: #0f172a;
  color: #f8fafc;
  border-right: 1px solid #1e293b;
  transition: width 0.25s cubic-bezier(0.4, 0, 0.2, 1);
  overflow: hidden;
  z-index: 100;
}

.logo {
  display: flex;
  align-items: center;
  gap: 12px;
  height: 64px;
  padding: 0 18px;
  background: #0f172a;
  border-bottom: 1px solid #1e293b;
}

.logo-icon-box {
  width: 36px;
  height: 36px;
  border-radius: 10px;
  background: linear-gradient(135deg, #2563eb 0%, #1d4ed8 100%);
  display: flex;
  align-items: center;
  justify-content: center;
  box-shadow: 0 4px 10px rgba(37, 99, 235, 0.3);
  flex-shrink: 0;
}

.logo-info {
  display: flex;
  flex-direction: column;
  overflow: hidden;
}

.logo-text {
  font-size: 15px;
  font-weight: 700;
  color: #ffffff;
  letter-spacing: -0.2px;
  white-space: nowrap;
}

.logo-sub {
  font-size: 11px;
  color: #94a3b8;
  white-space: nowrap;
}

.layout-menu {
  border-right: none;
  background: transparent;
  flex: 1;
  padding: 12px 10px;
}

.layout-menu :deep(.el-menu-item) {
  height: 44px;
  line-height: 44px;
  margin-bottom: 4px;
  border-radius: 8px;
  color: #94a3b8;
  font-weight: 500;
  font-size: 14px;
  transition: all 0.15s ease;
}

.layout-menu :deep(.el-menu-item:hover) {
  color: #ffffff;
  background: #1e293b;
}

.layout-menu :deep(.el-menu-item.is-active) {
  color: #ffffff;
  background: #2563eb;
  font-weight: 600;
  box-shadow: 0 4px 12px rgba(37, 99, 235, 0.25);
}

.menu-icon {
  font-size: 18px;
  margin-right: 10px;
}

.aside-footer {
  padding: 14px;
  border-top: 1px solid #1e293b;
  background: #090e1a;
}

.server-status-badge {
  display: flex;
  align-items: center;
  gap: 10px;
  padding: 8px 12px;
  border-radius: 8px;
  background: #1e293b;
  border: 1px solid #334155;
}

.status-dot {
  width: 8px;
  height: 8px;
  border-radius: 50%;
  flex-shrink: 0;
}

.status-dot.online {
  background: #10b981;
  box-shadow: 0 0 8px #10b981;
}

.status-info {
  display: flex;
  flex-direction: column;
}

.status-title {
  font-size: 12px;
  font-weight: 600;
  color: #e2e8f0;
}

.status-state {
  font-size: 11px;
  color: #10b981;
}

.layout-body {
  background: var(--el-bg-color-page);
  display: flex;
  flex-direction: column;
}

.layout-header {
  height: 64px;
  display: flex;
  align-items: center;
  justify-content: space-between;
  padding: 0 24px;
  background: #ffffff;
  border-bottom: 1px solid #e2e8f0;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 14px;
}

.collapse-btn-box {
  width: 34px;
  height: 34px;
  border-radius: 8px;
  border: 1px solid #e2e8f0;
  background: #f8fafc;
  color: #64748b;
  display: flex;
  align-items: center;
  justify-content: center;
  cursor: pointer;
  transition: all 0.15s ease;
}

.collapse-btn-box:hover {
  background: #e2e8f0;
  color: #0f172a;
}

.page-title {
  font-size: 17px;
  font-weight: 700;
  color: #0f172a;
  margin: 0;
  letter-spacing: -0.3px;
}

.header-right {
  display: flex;
  align-items: center;
  gap: 14px;
}

.header-badge-tag {
  display: flex;
  align-items: center;
  gap: 6px;
  padding: 4px 10px;
  border-radius: 20px;
  background: #f0fdf4;
  border: 1px solid #bbf7d0;
  color: #15803d;
  font-size: 12px;
  font-weight: 600;
}

.user-pill {
  display: flex;
  align-items: center;
  gap: 8px;
  padding: 4px 10px 4px 4px;
  border-radius: 24px;
  background: #f8fafc;
  border: 1px solid #e2e8f0;
  cursor: pointer;
  transition: all 0.15s ease;
}

.user-pill:hover {
  background: #f1f5f9;
  border-color: #cbd5e1;
}

.user-avatar {
  width: 28px;
  height: 28px;
  border-radius: 50%;
  background: #2563eb;
  color: #ffffff;
  display: flex;
  align-items: center;
  justify-content: center;
}

.user-name {
  font-size: 13px;
  font-weight: 600;
  color: #1e293b;
}

.user-arrow {
  font-size: 12px;
  color: #94a3b8;
}

.layout-main {
  padding: 24px;
  overflow-y: auto;
  flex: 1;
}

.main-content-wrapper {
  max-width: 1380px;
  margin: 0 auto;
}

/* Page Transition */
.fade-transform-enter-active,
.fade-transform-leave-active {
  transition: opacity 0.2s ease, transform 0.2s ease;
}

.fade-transform-enter-from {
  opacity: 0;
  transform: translateY(4px);
}

.fade-transform-leave-to {
  opacity: 0;
  transform: translateY(-4px);
}

@media (max-width: 768px) {
  .layout-header {
    height: 52px;
    padding: 0 14px;
  }

  .page-title {
    font-size: 16px;
  }

  .layout-main {
    padding: 14px;
    padding-bottom: calc(60px + var(--safe-bottom) + 14px);
  }
}
</style>
