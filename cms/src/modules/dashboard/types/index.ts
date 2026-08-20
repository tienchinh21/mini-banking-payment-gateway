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
  successRate: number
  activeMerchants: number
  recentPayments: RecentPaymentItem[]
}
