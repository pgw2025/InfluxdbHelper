import { get } from './request'

export interface VariableCount {
  variableName: string
  count: number
  startTime: string
  endTime: string
  period: string
}

export interface StatisticsSummary {
  period: string
  startTime: string
  endTime: string
  total: number
  variables: VariableCount[]
}

export interface HistoryItem {
  variableName: string
  value: unknown
  time: string
}

export interface HistoryResult {
  variableName: string
  startTime: string
  endTime: string
  result: {
    items: HistoryItem[]
    total: number
    page: number
    pageSize: number
  }
}

export function getSummary(period: string, start?: string, end?: string) {
  return get<StatisticsSummary>('/statistics/summary', { period, start, end })
}

export function getHistory(params: {
  variableName: string
  start?: string
  end?: string
  page?: number
  pageSize?: number
}) {
  return get<HistoryResult>('/statistics/history', params)
}
