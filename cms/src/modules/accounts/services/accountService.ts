import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type {
  WalletAccountItem,
  AccountFilterParams,
  TopUpFormData,
  TopUpResult,
  FreezeWalletPayload,
  FreezeWalletResult,
  WalletBalanceInfo,
  WalletLedgerEntry,
} from '../types'

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

  async getBalance(accountNumber: string): Promise<WalletBalanceInfo> {
    const response = await http.get<WalletBalanceInfo>(
      API_ENDPOINTS.ACCOUNTS.BALANCE(accountNumber)
    )
    return response.data
  },

  async getAccountBalance(accountNumber: string): Promise<WalletBalanceInfo> {
    return this.getBalance(accountNumber)
  },

  async getLedger(accountNumber: string): Promise<WalletLedgerEntry[]> {
    const response = await http.get<WalletLedgerEntry[]>(
      API_ENDPOINTS.ACCOUNTS.LEDGER(accountNumber)
    )
    return response.data
  },

  async getAccountLedger(accountNumber: string): Promise<WalletLedgerEntry[]> {
    return this.getLedger(accountNumber)
  },

  async topUp(data: TopUpFormData): Promise<TopUpResult> {
    const response = await http.post<TopUpResult>(
      API_ENDPOINTS.ACCOUNTS.TOP_UP,
      data
    )
    return response.data
  },

  async toggleFreeze(id: string, status: FreezeWalletPayload['status']): Promise<FreezeWalletResult> {
    const response = await http.post<FreezeWalletResult>(
      API_ENDPOINTS.ACCOUNTS.FREEZE(id),
      { status }
    )
    return response.data
  },
}
