import { fileURLToPath, URL } from 'node:url'
import { defineConfig } from 'vite'
import vue from '@vitejs/plugin-vue'

export default defineConfig({
  plugins: [vue()],
  resolve: {
    alias: {
      '@': fileURLToPath(new URL('./src', import.meta.url))
    }
  },
  server: {
    host: true,
    port: 5173,
    // 开发期把 /api 转发到后端 Kestrel（生产环境由 Nginx 反代，无需此配置）
    // host: true 使 Vite 开发服务器也监听 0.0.0.0，便于局域网内其他设备访问前端
    proxy: {
      '/api': {
        target: 'http://0.0.0.0:5100',
        changeOrigin: true
      }
    }
  },
  build: {
    chunkSizeWarningLimit: 2048
  }
})
