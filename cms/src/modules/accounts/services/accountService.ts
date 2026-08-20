import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type { WalletAccountItem, AccountFilterParams, TopUpFormData } from '../types'

export const accountService = {
  async getAccounts(params?: AccountFilterParams): Promise<PaginatedResult<WalletAccountItem>> {
    const response = await http.get<PaginatedResult<WalletAccountItem>>(
      API_ENDPOINTS.ACCOUNTS.LIST,
      params
    )
    return response.data
  },

  async getAccountDetail(id: string) {
    const response = await http.get(API_ENDPOINTS.ACCOUNTS.DETAIL(id))
    return response.data
  },

  async getAccountBalance(accountNumber: string) {
    const response = await http.get(API_ENDPOINTS.ACCOUNTS.BALANCE(accountNumber))
    return response.data
  },

  async getAccountLedger(accountNumber: string) {
    const response = await http.get(API_ENDPOINTS.ACCOUNTS.LEDGER(accountNumber))
    return response.data
  },

  async topUp(data: TopUpFormData) {
    const response = await http.post(API_ENDPOINTS.ACCOUNTS.TOP_UP, data)
    return response.data
  },

  async toggleFreeze(id: string, status: 'ACTIVE' | 'FROZEN') {
    const response = await http.post(API_ENDPOINTS.ACCOUNTS.FREEZE(id), { status })
    return response.data
  },
}
