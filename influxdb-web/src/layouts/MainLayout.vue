<template>
  <el-container class="layout">
    <el-aside v-show="!isMobile" :width="isCollapse ? '64px' : '210px'" class="layout-aside">
      <div class="logo">
        <el-icon :size="22" color="#409eff"><DataLine /></el-icon>
        <span v-show="!isCollapse" class="logo-text">InfluxDB 助手</span>
      </div>
      <el-menu
        :default-active="route.path"
        :collapse="isCollapse"
        :collapse-transition="false"
        router
        class="layout-menu"
      >
        <el-menu-item v-for="item in menus" :key="item.path" :index="item.path">
          <el-icon><component :is="item.icon" /></el-icon>
          <template #title>{{ item.title }}</template>
        </el-menu-item>
      </el-menu>
    </el-aside>

    <el-container>
      <el-header class="layout-header">
        <div class="header-left">
          <el-icon
            v-show="!isMobile"
            class="collapse-btn"
            :size="18"
            @click="isCollapse = !isCollapse"
          >
            <Expand v-if="isCollapse" />
            <Fold v-else />
          </el-icon>
          <!-- 移动端显示当前页标题，桌面端显示面包屑 -->
          <span v-if="isMobile" class="mobile-title">{{ route.meta.title }}</span>
          <el-breadcrumb v-else separator="/">
            <el-breadcrumb-item :to="{ path: '/' }">首页</el-breadcrumb-item>
            <el-breadcrumb-item v-if="route.meta.title">{{ route.meta.title }}</el-breadcrumb-item>
          </el-breadcrumb>
        </div>
        <div class="header-right">
          <el-dropdown @command="onCommand">
            <span class="user-info">
              <el-icon><User /></el-icon>
              {{ auth.displayName || 'admin' }}
              <el-icon><ArrowDown /></el-icon>
            </span>
            <template #dropdown>
              <el-dropdown-menu>
                <el-dropdown-item command="logout">退出登录</el-dropdown-item>
              </el-dropdown-menu>
            </template>
          </el-dropdown>
        </div>
      </el-header>

      <el-main class="layout-main" v-pull-refresh>
        <router-view />
      </el-main>
    </el-container>

    <!-- 移动端底部导航 Tab 栏 -->
    <MobileTabBar v-if="isMobile" />
  </el-container>
</template>

<script setup lang="ts">
import { computed, ref } from 'vue'
import { useRoute, useRouter } from 'vue-router'
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
  }
}
</script>

<style scoped>
.layout {
  height: 100vh;
}

.layout-aside {
  display: flex;
  flex-direction: column;
  border-right: 1px solid var(--el-border-color-light);
  background: #fff;
  transition: width 0.2s;
}

.logo {
  display: flex;
  align-items: center;
  justify-content: center;
  gap: 8px;
  height: 56px;
  border-bottom: 1px solid var(--el-border-color-light);
}

.logo-text {
  font-size: 16px;
  font-weight: 600;
  color: var(--el-text-color-primary);
  white-space: nowrap;
}

.layout-menu {
  border-right: none;
  flex: 1;
}

.layout-header {
  display: flex;
  align-items: center;
  justify-content: space-between;
  height: 56px;
  border-bottom: 1px solid var(--el-border-color-light);
  background: #fff;
}

.header-left {
  display: flex;
  align-items: center;
  gap: 16px;
}

.mobile-title {
  font-size: 17px;
  font-weight: 600;
  color: var(--el-text-color-primary);
}

.collapse-btn {
  cursor: pointer;
  color: var(--el-text-color-secondary);
}

.collapse-btn:hover {
  color: var(--el-color-primary);
}

.header-right .user-info {
  display: flex;
  align-items: center;
  gap: 6px;
  cursor: pointer;
  color: var(--el-text-color-regular);
  font-size: 14px;
}

.layout-main {
  background: #f5f7fa;
  padding: 16px;
  overflow-y: auto;
}

/* 移动端：顶栏更紧凑，内容区底部留出 Tab 栏 + 安全区空间 */
@media (max-width: 768px) {
  .layout {
    height: 100vh;
    height: 100dvh;
  }

  .layout-header {
    height: 50px;
    padding: 0 12px;
  }

  .header-left {
    gap: 8px;
  }

  .layout-main {
    padding: 12px;
    /* 56px Tab 栏 + 安全区 + 12px 间距 */
    padding-bottom: calc(56px + var(--safe-bottom) + 12px);
  }
}
</style>
