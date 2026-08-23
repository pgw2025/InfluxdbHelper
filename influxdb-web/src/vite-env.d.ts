/// <reference types="vite/client" />

interface ImportMetaEnv {
  /** 前端调用的后端基地址；未设置时回退为 /api（由 Vite 代理 / Nginx 反代转发） */
  readonly VITE_API_BASE_URL?: string
}

interface ImportMeta {
  readonly env: ImportMetaEnv
}

declare module '*.vue' {
  import type { DefineComponent } from 'vue'
  const component: DefineComponent<{}, {}, any>
  export default component
}
