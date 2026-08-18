import type { StatusTagType } from '@/types/common'

export interface StatusConfig {
  label: string
  color: string
  type: StatusTagType
  description?: string
}

export const PAYMENT_STATUS: Record<string, StatusConfig> = {
  PENDING: {
    label: 'Chờ xử lý',
    color: 'processing',
    type: 'processing',
    description: 'Giao dịch đang được xử lý',
  },
  SUCCEEDED: {
    label: 'Thành công',
    color: 'success',
    type: 'success',
    description: 'Thanh toán thành công',
  },
  FAILED: {
    label: 'Thất bại',
    color: 'error',
    type: 'error',
    description: 'Giao dịch bị từ chối hoặc lỗi',
  },
  REFUNDED: {
    label: 'Đã hoàn tiền',
    color: 'purple',
    type: 'default',
    description: 'Toàn bộ số tiền đã được hoàn lại',
  },
  PARTIALLY_REFUNDED: {
    label: 'Hoàn tiền 1 phần',
    color: 'warning',
    type: 'warning',
    description: 'Một phần số tiền đã hoàn lại',
  },
}

export const LEDGER_TRANSACTION_TYPE: Record<string, StatusConfig> = {
  TOP_UP: {
    label: 'Nạp tiền ví',
    color: 'green',
    type: 'success',
  },
  PAYMENT_DIRECT_DEBIT: {
    label: 'Thanh toán đơn hàng',
    color: 'blue',
    type: 'processing',
  },
  REFUND: {
    label: 'Hoàn tiền',
    color: 'orange',
    type: 'warning',
  },
  SETTLEMENT: {
    label: 'Quyết toán merchant',
    color: 'cyan',
    type: 'default',
  },
  ADJUSTMENT: {
    label: 'Điều chỉnh số dư',
    color: 'geekblue',
    type: 'default',
  },
}

export const ACCOUNT_STATUS: Record<string, StatusConfig> = {
  ACTIVE: {
    label: 'Hoạt động',
    color: 'success',
    type: 'success',
  },
  FROZEN: {
    label: 'Tạm khóa',
    color: 'warning',
    type: 'warning',
  },
  CLOSED: {
    label: 'Đã đóng',
    color: 'error',
    type: 'error',
  },
}

export const MERCHANT_STATUS: Record<string, StatusConfig> = {
  ACTIVE: {
    label: 'Hoạt động',
    color: 'success',
    type: 'success',
  },
  SUSPENDED: {
    label: 'Tạm ngưng',
    color: 'error',
    type: 'error',
  },
}
