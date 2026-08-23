import { get } from './request'

export interface SystemStatus {
  influxConfigured: boolean
  influxUrl: string
  influxOrg: string
  influxBucket: string
  dingTalkEnabled: boolean
  connectionOk: boolean
  /** 总数据点数量；-1 表示未配置/获取失败 */
  totalCount?: number
  /** InfluxDB 服务启动时间（本地时区 ISO，yyyy-MM-ddTHH:mm:ss）；null 表示未知 */
  influxStartedAt?: string | null
  /** 引擎目录占用字节数；-1 表示未配置/不可用 */
  storageSizeBytes?: number
}

export function getSystemStatus() {
  return get<SystemStatus>('/system/status')
}
