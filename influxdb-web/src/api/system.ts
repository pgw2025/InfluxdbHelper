import { get } from './request'

export interface SystemStatus {
  influxConfigured: boolean
  influxUrl: string
  influxOrg: string
  influxBucket: string
  dingTalkEnabled: boolean
  connectionOk: boolean
}

export function getSystemStatus() {
  return get<SystemStatus>('/system/status')
}
