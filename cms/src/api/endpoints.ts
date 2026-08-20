export const API_ENDPOINTS = {
  SYSTEM: {
    INFO: '/system/info',
    HEALTH: '/health',
  },
  AUTH: {
    LOGIN: '/auth/login',
    LOGOUT: '/auth/logout',
    PROFILE: '/admin/auth/profile',
  },
  ACCOUNTS: {
    LIST: '/admin/wallets',
    DETAIL: (id: string) => `/admin/wallets/${id}`,
    BALANCE: (accountNumber: string) => `/admin/wallets/${accountNumber}/balance`,
    LEDGER: (accountNumber: string) => `/admin/wallets/${accountNumber}/ledger`,
    TOP_UP: '/admin/wallets/top-up',
    FREEZE: (id: string) => `/admin/wallets/${id}/freeze`,
  },
  PAYMENTS: {
    LIST: '/admin/payments',
    DETAIL: (id: string) => `/admin/payments/${id}`,
    REFUND: (id: string) => `/admin/payments/${id}/refund`,
    MERCHANT_PAY: '/merchant/payments',
    MERCHANT_REFUND: '/merchant/refunds',
    SETTLEMENT: '/admin/settlements',
  },
  LEDGER: {
    TRANSACTIONS: '/admin/ledger/transactions',
    ENTRIES: '/admin/ledger/entries',
    RECONCILE: '/admin/ledger/reconcile',
  },
  MERCHANTS: {
    LIST: '/admin/merchants',
    DETAIL: (id: string) => `/admin/merchants/${id}`,
    CREATE: '/admin/merchants',
    REGENERATE_KEYS: (id: string) => `/admin/merchants/${id}/regenerate-keys`,
  },
  DASHBOARD: {
    STATS: '/admin/dashboard/stats',
  },
  AUDIT: {
    LOGS: '/admin/audit/logs',
  },
  DEMO: {
    SEED_STATUS: '/demo/seed-status',
  },
} as const
