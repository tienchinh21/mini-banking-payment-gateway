export interface LedgerEntryItem {
  id: string
  transactionId: string
  transactionType: string
  accountId: string
  accountName: string
  accountType: 'USER_WALLET' | 'MERCHANT_SETTLEMENT' | 'PLATFORM_CLEARING' | 'PLATFORM_FEE' | string
  entryType: 'DEBIT' | 'CREDIT'
  amount: number
  currency: string
  createdAt: string
}

export interface LedgerTransactionItem {
  id: string
  referenceId: string
  type: string | number
  description: string
  status: string | number
  createdAt: string
  entries?: LedgerEntryItem[]
}

export interface LedgerReconcileResult {
  status: 'BALANCED' | 'DISCREPANCY' | string
  isBalanced: boolean
  totalDebit: number
  totalCredit: number
  totalAccountsChecked: number
  totalEntriesChecked: number
  checkedAt: string
}

export interface LedgerFilterParams {
  keyword?: string
  accountType?: string
  entryType?: string
  page?: number
  pageSize?: number
}
