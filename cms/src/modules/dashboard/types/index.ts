export interface RecentPaymentItem {
  id: string
  orderId: string
  merchantName: string
  customerName: string
  amount: number
  currency: string
  status: string
  createdAt: string
}

export interface DashboardStats {
  totalBalance: number
  dailyPayments: number
  totalVolume?: number
  totalPayments?: number
  successRate: number
  activeWallets?: number
  activeMerchants: number
  recentPayments: RecentPaymentItem[]
}

export interface HealthCheckEntry {
  name: string
  status: 'Healthy' | 'Degraded' | 'Unhealthy' | string
  duration?: number
  exception?: string | null
}

export interface SystemHealthReport {
  status: 'Healthy' | 'Degraded' | 'Unhealthy' | string
  totalDuration?: number
  checks: HealthCheckEntry[]
}
