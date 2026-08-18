import dayjs from 'dayjs'
import { DATE_FORMATS } from '@/constants/common'

/**
 * Format money with currency code (e.g. 100,000 VND)
 */
export function formatMoney(
  amount?: number | string | null,
  currency = 'VND',
  options?: {
    showSign?: boolean
    compact?: boolean
  }
): string {
  if (amount === undefined || amount === null || isNaN(Number(amount))) {
    return `0 ${currency}`
  }

  const num = Number(amount)
  const sign = options?.showSign && num > 0 ? '+' : ''

  if (currency === 'VND') {
    const formatted = new Intl.NumberFormat('vi-VN').format(num)
    return `${sign}${formatted} ₫`
  }

  const formatted = new Intl.NumberFormat('en-US', {
    style: 'currency',
    currency,
  }).format(num)

  return `${sign}${formatted}`
}

/**
 * Format standard date
 */
export function formatDate(
  date?: string | Date | number | null,
  format = DATE_FORMATS.DISPLAY_DATETIME
): string {
  if (!date) return '-'
  const d = dayjs(date)
  return d.isValid() ? d.format(format) : '-'
}

/**
 * Format number with thousand separators
 */
export function formatNumber(value?: number | string | null): string {
  if (value === undefined || value === null || isNaN(Number(value))) {
    return '0'
  }
  return new Intl.NumberFormat('vi-VN').format(Number(value))
}

/**
 * Mask sensitive account number (e.g. WA-1234****89)
 */
export function maskAccountNumber(accNo?: string): string {
  if (!accNo || accNo.length < 6) return accNo || '-'
  const prefix = accNo.slice(0, 4)
  const suffix = accNo.slice(-4)
  return `${prefix}****${suffix}`
}
