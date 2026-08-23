import { createRouter, createWebHistory } from 'vue-router'
import { useAuthStore } from '@/stores/auth'

const router = createRouter({
  history: createWebHistory(),
  routes: [
    {
      path: '/login',
      name: 'login',
      component: () => import('@/views/Login.vue'),
      meta: { title: '登录' }
    },
    {
      path: '/',
      component: () => import('@/layouts/MainLayout.vue'),
      redirect: '/dashboard',
      children: [
        {
          path: 'dashboard',
          name: 'dashboard',
          component: () => import('@/views/Dashboard.vue'),
          meta: { title: '系统状态', icon: 'Odometer' }
        },
        {
          path: 'statistics',
          name: 'statistics',
          component: () => import('@/views/Statistics.vue'),
          meta: { title: '数据统计', icon: 'DataAnalysis' }
        },
        {
          path: 'history',
          name: 'history',
          component: () => import('@/views/VariableHistory.vue'),
          meta: { title: '变量历史', icon: 'Clock' }
        },
        {
          path: 'config',
          name: 'config',
          component: () => import('@/views/Config.vue'),
          meta: { title: '系统配置', icon: 'Setting' }
        }
      ]
    },
    { path: '/:pathMatch(.*)*', redirect: '/' }
  ]
})

router.beforeEach(to => {
  const auth = useAuthStore()
  if (to.path !== '/login' && !auth.isLoggedIn) {
    return { path: '/login', query: { redirect: to.fullPath } }
  }
  if (to.path === '/login' && auth.isLoggedIn) {
    return { path: '/' }
  }
  document.title = `${to.meta.title ?? ''} - InfluxDB 助手`
})

export default router
