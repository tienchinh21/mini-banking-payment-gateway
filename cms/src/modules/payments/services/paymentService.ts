import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type { PaymentItem, PaymentFilterParams, RefundFormData } from '../types'

export const paymentService = {
  async getPayments(params?: PaymentFilterParams): Promise<PaginatedResult<PaymentItem>> {
    const response = await http.get<PaginatedResult<PaymentItem>>(
      API_ENDPOINTS.PAYMENTS.LIST,
      params
    )
    return response.data
  },

  async getPaymentDetail(id: string) {
    const response = await http.get(API_ENDPOINTS.PAYMENTS.DETAIL(id))
    return response.data
  },

  async refund(data: RefundFormData) {
    const response = await http.post(API_ENDPOINTS.PAYMENTS.REFUND(data.paymentId), data)
    return response.data
  },

  async createSettlement(data: { merchantId: string; amount: number; currency: string }) {
    const response = await http.post(API_ENDPOINTS.PAYMENTS.SETTLEMENT, data)
    return response.data
  },
}
