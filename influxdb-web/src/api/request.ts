import axios from 'axios'
import { ElMessage } from 'element-plus'
import router from '@/router'

/** 后端统一响应结构 */
export interface ApiResult<T = unknown> {
  code: number
  message: string
  data: T
}

const request = axios.create({
  baseURL: '/api',
  timeout: 60000
})

request.interceptors.request.use(config => {
  const token = localStorage.getItem('token')
  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }
  return config
})

request.interceptors.response.use(
  res => {
    const body = res.data as ApiResult
    if (body.code !== 0) {
      ElMessage.error(body.message || '请求失败')
      return Promise.reject(new Error(body.message))
    }
    return body.data as never
  },
  err => {
    if (err.response?.status === 401) {
      localStorage.removeItem('token')
      localStorage.removeItem('displayName')
      if (router.currentRoute.value.path !== '/login') {
        ElMessage.error('登录已过期，请重新登录')
        router.push('/login')
      }
    } else {
      ElMessage.error(err.response?.data?.message || err.message || '网络错误')
    }
    return Promise.reject(err)
  }
)

export async function get<T>(url: string, params?: Record<string, unknown>): Promise<T> {
  return (await request.get(url, { params })) as unknown as T
}

export async function post<T>(url: string, data?: unknown): Promise<T> {
  return (await request.post(url, data)) as unknown as T
}

export async function put<T>(url: string, data?: unknown): Promise<T> {
  return (await request.put(url, data)) as unknown as T
}

export default request
