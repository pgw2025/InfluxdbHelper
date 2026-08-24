export function padZero(n: number): string {
  return String(n).padStart(2, '0')
}

export function formatToIsoLocal(d: Date): string {
  return `${d.getFullYear()}-${padZero(d.getMonth() + 1)}-${padZero(d.getDate())}T${padZero(d.getHours())}:${padZero(d.getMinutes())}:${padZero(d.getSeconds())}`
}

export function computePeriodRange(p: string): [string, string] {
  const now = new Date()

  const startOfDay = (d: Date) => {
    const x = new Date(d)
    x.setHours(0, 0, 0, 0)
    return x
  }

  const endOfDay = (d: Date) => {
    const x = new Date(d)
    x.setHours(23, 59, 59, 999)
    return x
  }

  switch (p) {
    case '1h': {
      const s = new Date(now.getTime() - 1 * 3600 * 1000)
      return [formatToIsoLocal(s), formatToIsoLocal(now)]
    }
    case '6h': {
      const s = new Date(now.getTime() - 6 * 3600 * 1000)
      return [formatToIsoLocal(s), formatToIsoLocal(now)]
    }
    case '12h': {
      const s = new Date(now.getTime() - 12 * 3600 * 1000)
      return [formatToIsoLocal(s), formatToIsoLocal(now)]
    }
    case '24h': {
      const s = new Date(now.getTime() - 24 * 3600 * 1000)
      return [formatToIsoLocal(s), formatToIsoLocal(now)]
    }
    case 'yesterday': {
      const y = new Date(now)
      y.setDate(now.getDate() - 1)
      return [formatToIsoLocal(startOfDay(y)), formatToIsoLocal(endOfDay(y))]
    }
    case 'daybefore': {
      const y = new Date(now)
      y.setDate(now.getDate() - 2)
      return [formatToIsoLocal(startOfDay(y)), formatToIsoLocal(endOfDay(y))]
    }
    case '3d': {
      const s = new Date(now.getTime() - 3 * 24 * 3600 * 1000)
      return [formatToIsoLocal(s), formatToIsoLocal(now)]
    }
    case '7d': {
      const s = new Date(now.getTime() - 7 * 24 * 3600 * 1000)
      return [formatToIsoLocal(s), formatToIsoLocal(now)]
    }
    case '30d': {
      const s = new Date(now.getTime() - 30 * 24 * 3600 * 1000)
      return [formatToIsoLocal(s), formatToIsoLocal(now)]
    }
    case 'week': {
      const d = new Date(now)
      const day = d.getDay() || 7
      d.setDate(d.getDate() - day + 1)
      return [formatToIsoLocal(startOfDay(d)), formatToIsoLocal(now)]
    }
    case 'month': {
      const d = new Date(now.getFullYear(), now.getMonth(), 1, 0, 0, 0)
      return [formatToIsoLocal(d), formatToIsoLocal(now)]
    }
    case 'year': {
      const d = new Date(now.getFullYear(), 0, 1, 0, 0, 0)
      return [formatToIsoLocal(d), formatToIsoLocal(now)]
    }
    case 'all': {
      const d = new Date('2020-01-01T00:00:00')
      return [formatToIsoLocal(d), formatToIsoLocal(now)]
    }
    case 'day':
    default: {
      return [formatToIsoLocal(startOfDay(now)), formatToIsoLocal(now)]
    }
  }
}
