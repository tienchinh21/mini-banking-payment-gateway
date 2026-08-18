export interface PaymentItem {
  id: string
  merchantId: string
  merchantName: string
  orderId: string
  payerWalletNumber: string
  payerName: string
  amount: number
  currency: string
  status: 'PENDING' | 'SUCCEEDED' | 'FAILED' | 'REFUNDED' | 'PARTIALLY_REFUNDED'
  idempotencyKey: string
  errorMessage?: string
  createdAt: string
  updatedAt?: string
}

export interface PaymentFilterParams {
  keyword?: string
  merchantId?: string
  status?: string
  fromDate?: string
  toDate?: string
  page?: number
  pageSize?: number
}

export interface RefundFormData {
  paymentId: string
  amount: number
  reason: string
}
