import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type {
  LedgerEntryItem,
  LedgerTransactionItem,
  LedgerReconcileResult,
  LedgerFilterParams,
} from '../types'

export const ledgerService = {
  async getEntries(params?: LedgerFilterParams): Promise<PaginatedResult<LedgerEntryItem>> {
    const response = await http.get<PaginatedResult<LedgerEntryItem>>(
      API_ENDPOINTS.LEDGER.ENTRIES,
      params
    )
    return response.data
  },

  async getTransactions(params?: { keyword?: string; page?: number; pageSize?: number }): Promise<PaginatedResult<LedgerTransactionItem>> {
    const response = await http.get<PaginatedResult<LedgerTransactionItem>>(
      API_ENDPOINTS.LEDGER.TRANSACTIONS,
      params
    )
    return response.data
  },

  async reconcile(): Promise<LedgerReconcileResult> {
    const response = await http.post<LedgerReconcileResult>(
      API_ENDPOINTS.LEDGER.RECONCILE
    )
    return response.data
  },
}
