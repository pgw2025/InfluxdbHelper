import request from '@/api/request'
import { get } from '@/api/request'

export interface DeletePayload {
  start: string
  stop: string
  dataName: string
  confirm: boolean
}

export interface VariablePreviewSample {
  time: string | null
  value: unknown | null
}

export interface VariablePreview {
  dataName: string
  pointCount: number
  firstTime: string | null
  lastTime: string | null
  samples: VariablePreviewSample[]
}

// 导出 CSV：直接触发浏览器下载（后端返回 text/csv 文件流）
export async function exportCsv(params: { start: string; stop: string; dataName?: string }) {
  const query = new URLSearchParams({
    start: params.start,
    stop: params.stop
  })
  if (params.dataName) query.append('dataName', params.dataName)

  const token = localStorage.getItem('token') || ''
  const res = await fetch(`/api/maintenance/export?${query.toString()}`, {
    headers: { Authorization: `Bearer ${token}` }
  })
  if (!res.ok) {
    const err = await res.json().catch(() => null)
    throw new Error(err?.message || `导出失败 (${res.status})`)
  }
  const blob = await res.blob()
  const disposition = res.headers.get('content-disposition') || ''
  const match = disposition.match(/filename\*?=(?:UTF-8'')?["']?([^"';]+)/i)
  const fileName = match ? decodeURIComponent(match[1]) : `influx-export-${Date.now()}.csv`
  const url = window.URL.createObjectURL(blob)
  const a = document.createElement('a')
  a.href = url
  a.download = fileName
  document.body.appendChild(a)
  a.click()
  document.body.removeChild(a)
  window.URL.revokeObjectURL(url)
}

// 删除前预览：查询指定变量在所选时间范围的数据概览与抽样
export function previewDelete(params: { start: string; stop: string; dataName: string; sampleLimit?: number }) {
  return get<VariablePreview>('/maintenance/preview', params)
}

// 删除（删除前后端会先导出备份）
export function deleteData(payload: DeletePayload) {
  return request.post('/api/maintenance/delete', payload)
}
