import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type {
  PaymentItem,
  PaymentDetail,
  PaymentFilterParams,
  RefundFormData,
  RefundResult,
} from '../types'

export const paymentService = {
  async getPayments(params?: PaymentFilterParams): Promise<PaginatedResult<PaymentItem>> {
    const response = await http.get<PaginatedResult<PaymentItem>>(
      API_ENDPOINTS.PAYMENTS.LIST,
      params
    )
    return response.data
  },

  async getPaymentById(id: string): Promise<PaymentDetail> {
    const response = await http.get<PaymentDetail>(
      API_ENDPOINTS.PAYMENTS.DETAIL(id)
    )
    return response.data
  },

  async getPaymentDetail(id: string): Promise<PaymentDetail> {
    return this.getPaymentById(id)
  },

  async refund(data: RefundFormData): Promise<RefundResult> {
    const response = await http.post<RefundResult>(
      API_ENDPOINTS.PAYMENTS.REFUND(data.paymentId),
      {
        amount: data.amount,
        reason: data.reason,
      }
    )
    return response.data
  },

  async createSettlement(data: { merchantId: string; amount: number; currency: string }) {
    const response = await http.post(API_ENDPOINTS.PAYMENTS.SETTLEMENT, data)
    return response.data
  },
}
