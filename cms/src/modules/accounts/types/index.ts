export interface WalletAccountItem {
  id: string
  accountNumber: string
  customerName: string
  email: string
  phone: string
  currency: string
  availableBalance: number
  ledgerBalance: number
  status: 'ACTIVE' | 'FROZEN' | 'CLOSED'
  createdAt: string
  updatedAt?: string
}

export interface AccountFilterParams {
  keyword?: string
  status?: string
  currency?: string
  page?: number
  pageSize?: number
}

export interface TopUpFormData {
  accountNumber: string
  amount: number
  description: string
}
