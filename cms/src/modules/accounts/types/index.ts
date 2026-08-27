export type WalletAccountStatus = 'ACTIVE' | 'FROZEN' | 'CLOSED'

export interface WalletAccountItem {
  id: string
  accountNumber: string
  customerName: string
  email: string
  phone: string
  currency: string
  availableBalance: number
  ledgerBalance: number
  status: WalletAccountStatus
  createdAt: string
  updatedAt?: string
}

export interface AccountFilterParams {
  keyword?: string
  status?: string
  currency?: string
  page?: number
  pageSize?: number
}

export interface TopUpFormData {
  accountNumber: string
  amount: number
  description?: string
}

export interface TopUpResult {
  id: string
  accountNumber: string
  customerName: string
  availableBalance: number
  ledgerBalance: number
  transactionId: string
}

export interface FreezeWalletPayload {
  status: 'ACTIVE' | 'FROZEN'
}

export interface FreezeWalletResult {
  id: string
  accountNumber: string
  status: 'ACTIVE' | 'FROZEN'
}

export interface WalletBalanceInfo {
  id: string
  accountNumber: string
  currency: string
  customerName: string
  availableBalance: number
  ledgerBalance: number
}

export interface WalletLedgerEntry {
  id: string
  ledgerTransactionId: string
  accountType: string
  amount: number
  currency: string
  isDebit: boolean
  entryType: 'DEBIT' | 'CREDIT'
  createdAt: string
}
