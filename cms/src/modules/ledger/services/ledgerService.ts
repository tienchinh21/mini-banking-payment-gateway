import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type {
  LedgerEntryItem,
  LedgerTransactionItem,
  LedgerReconcileResult,
  LedgerFilterParams,
  TransactionLedgerSummary,
} from '../types'

export const ledgerService = {
  async getEntries(params?: LedgerFilterParams): Promise<PaginatedResult<LedgerEntryItem>> {
    const response = await http.get<PaginatedResult<LedgerEntryItem>>(
      API_ENDPOINTS.LEDGER.ENTRIES,
      params
    )
    return response.data
  },

  async getEntriesByTransactionId(transactionId: string): Promise<TransactionLedgerSummary> {
    const response = await this.getEntries({ keyword: transactionId, pageSize: 50 })
    const entries = response.items.filter(
      (e) => e.transactionId.toLowerCase() === transactionId.toLowerCase()
    )
    const totalDebit = entries
      .filter((e) => String(e.entryType).toUpperCase() === 'DEBIT')
      .reduce((sum, e) => sum + e.amount, 0)
    const totalCredit = entries
      .filter((e) => String(e.entryType).toUpperCase() === 'CREDIT')
      .reduce((sum, e) => sum + e.amount, 0)

    return {
      transactionId,
      isBalanced: totalDebit === totalCredit && entries.length > 0,
      totalDebit,
      totalCredit,
      currency: entries[0]?.currency || 'VND',
      entries,
    }
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
