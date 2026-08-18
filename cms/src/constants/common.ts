export const STORAGE_KEYS = {
  ACCESS_TOKEN: 'mini_banking_access_token',
  REFRESH_TOKEN: 'mini_banking_refresh_token',
  USER_INFO: 'mini_banking_user_info',
  THEME_MODE: 'mini_banking_theme_mode',
  SIDEBAR_COLLAPSED: 'mini_banking_sidebar_collapsed',
} as const

export const DEFAULT_PAGINATION = {
  PAGE: 1,
  PAGE_SIZE: 10,
  PAGE_SIZE_OPTIONS: ['10', '20', '50', '100'],
} as const

export const DATE_FORMATS = {
  DATE: 'YYYY-MM-DD',
  DATETIME: 'YYYY-MM-DD HH:mm:ss',
  TIME: 'HH:mm:ss',
  DISPLAY_DATE: 'DD/MM/YYYY',
  DISPLAY_DATETIME: 'DD/MM/YYYY HH:mm:ss',
} as const

export const APP_CONFIG = {
  NAME: 'Mini Banking Admin',
  DEFAULT_CURRENCY: 'VND',
  SYSTEM_VERSION: '1.0.0',
} as const
