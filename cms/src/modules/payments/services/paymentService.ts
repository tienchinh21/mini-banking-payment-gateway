import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type { PaymentItem, PaymentFilterParams, RefundFormData } from '../types'

const mockPayments: PaymentItem[] = [
  {
    id: 'PAY-20260815-001',
    merchantId: 'MCH-01',
    merchantName: 'E-commerce Shop Alpha',
    orderId: 'ORD-99881',
    payerWalletNumber: 'WA-8801928371',
    payerName: 'Nguyễn Văn An',
    amount: 250000,
    currency: 'VND',
    status: 'SUCCEEDED',
    idempotencyKey: 'idem-ord-99881-20260815',
    createdAt: '2026-08-15T10:00:00Z',
  },
  {
    id: 'PAY-20260815-002',
    merchantId: 'MCH-01',
    merchantName: 'E-commerce Shop Alpha',
    orderId: 'ORD-99882',
    payerWalletNumber: 'WA-8801928372',
    payerName: 'Trần Thị Bình',
    amount: 1200000,
    currency: 'VND',
    status: 'SUCCEEDED',
    idempotencyKey: 'idem-ord-99882-20260815',
    createdAt: '2026-08-15T11:20:00Z',
  },
  {
    id: 'PAY-20260815-003',
    merchantId: 'MCH-02',
    merchantName: 'Tech Store Beta',
    orderId: 'ORD-55410',
    payerWalletNumber: 'WA-8801928373',
    payerName: 'Lê Hoàng Cường',
    amount: 3500000,
    currency: 'VND',
    status: 'FAILED',
    errorMessage: 'Tài khoản không đủ số dư',
    idempotencyKey: 'idem-ord-55410-20260815',
    createdAt: '2026-08-15T13:45:00Z',
  },
  {
    id: 'PAY-20260815-004',
    merchantId: 'MCH-02',
    merchantName: 'Tech Store Beta',
    orderId: 'ORD-55411',
    payerWalletNumber: 'WA-8801928374',
    payerName: 'Phạm Thu Dung',
    amount: 750000,
    currency: 'VND',
    status: 'REFUNDED',
    idempotencyKey: 'idem-ord-55411-20260815',
    createdAt: '2026-08-15T14:10:00Z',
  },
  {
    id: 'PAY-20260815-005',
    merchantId: 'MCH-03',
    merchantName: 'Fashion Hub',
    orderId: 'ORD-12345',
    payerWalletNumber: 'WA-8801928375',
    payerName: 'Vũ Đức Em',
    amount: 450000,
    currency: 'VND',
    status: 'SUCCEEDED',
    idempotencyKey: 'idem-ord-12345-20260815',
    createdAt: '2026-08-15T15:30:00Z',
  },
]

export const paymentService = {
  async getPayments(params?: PaymentFilterParams): Promise<PaginatedResult<PaymentItem>> {
    try {
      const response = await http.get<PaginatedResult<PaymentItem>>(
        API_ENDPOINTS.PAYMENTS.LIST,
        params
      )
      return response.data
    } catch {
      let list = [...mockPayments]
      if (params?.keyword) {
        const kw = params.keyword.toLowerCase()
        list = list.filter(
          (p) =>
            p.id.toLowerCase().includes(kw) ||
            p.orderId.toLowerCase().includes(kw) ||
            p.merchantName.toLowerCase().includes(kw) ||
            p.payerName.toLowerCase().includes(kw) ||
            p.payerWalletNumber.toLowerCase().includes(kw)
        )
      }
      if (params?.status) {
        list = list.filter((p) => p.status === params.status)
      }
      if (params?.merchantId) {
        list = list.filter((p) => p.merchantId === params.merchantId)
      }

      const page = params?.page || 1
      const pageSize = params?.pageSize || 10
      const start = (page - 1) * pageSize
      const items = list.slice(start, start + pageSize)

      return {
        items,
        meta: {
          currentPage: page,
          pageSize,
          totalItems: list.length,
          totalPages: Math.ceil(list.length / pageSize),
          hasNext: start + pageSize < list.length,
          hasPrevious: page > 1,
        },
      }
    }
  },

  async refund(data: RefundFormData) {
    try {
      return await http.post(API_ENDPOINTS.PAYMENTS.REFUND(data.paymentId), data)
    } catch {
      const payment = mockPayments.find((p) => p.id === data.paymentId)
      if (payment) {
        payment.status = payment.amount === data.amount ? 'REFUNDED' : 'PARTIALLY_REFUNDED'
      }
      return { success: true, message: 'Hoàn tiền thành công (Demo)' }
    }
  },
}
