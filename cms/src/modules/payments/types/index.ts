export interface PaymentItem {
  id: string
  merchantId: string
  merchantName: string
  orderId: string
  payerWalletNumber: string
  payerName: string
  amount: number
  currency: string
  status: 'PENDING' | 'SUCCEEDED' | 'FAILED' | 'REFUNDED' | 'PARTIALLY_REFUNDED' | string
  description?: string
  failureCode?: string | null
  errorMessage?: string | null
  idempotencyKey: string
  ledgerTransactionId?: string | null
  createdAt: string
  updatedAt?: string
}

export interface PaymentLedgerEntry {
  id: string
  accountId: string
  accountType: string
  amount: number
  currency: string
  isDebit: boolean
  createdAt: string
}

export interface PaymentDetail extends PaymentItem {
  callbackUrl?: string | null
  ledgerEntries?: PaymentLedgerEntry[]
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

export interface RefundResult {
  refundId: string
  paymentId: string
  amount: number
  currency: string
  reason?: string
  status: string
  createdAt: string
}
