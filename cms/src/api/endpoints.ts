export const API_ENDPOINTS = {
  DASHBOARD: {
    STATS: '/admin/dashboard/stats',
  },
  SYSTEM: {
    INFO: '/system/info',
    HEALTH: '/health',
  },
  AUTH: {
    LOGIN: '/admin/auth/login',
    LOGOUT: '/admin/auth/logout',
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
    UPDATE: (id: string) => `/admin/merchants/${id}`,
    DELETE: (id: string) => `/admin/merchants/${id}`,
    REGENERATE_KEYS: (id: string) => `/admin/merchants/${id}/regenerate-keys`,
  },
  AUDIT: {
    LOGS: '/admin/audit-logs',
  },
  DEMO: {
    SEED_STATUS: '/demo/seed-status',
  },
} as const
