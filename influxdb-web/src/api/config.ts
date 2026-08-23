import { get, post, put } from './request'

export interface AppConfig {
  url: string
  token: string
  org: string
  bucket: string
  dingTalkWebhookUrl: string
  dingTalkSecret: string
  dingTalkEnabled: boolean
  dingTalkSendHour: number
  dingTalkSendMinute: number
  dingTalkMessageTemplate: string
}

export interface ConfigSaveResult {
  saved: boolean
  connectionOk: boolean
  error: string | null
}

export function getConfig() {
  return get<AppConfig>('/config')
}

export function saveConfig(data: AppConfig & { persist: boolean }) {
  return put<ConfigSaveResult>('/config', data)
}

export function testConnection(data: { url: string; token: string }) {
  return post<{ connectionOk: boolean }>('/config/test', data)
}
