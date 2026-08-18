import React from 'react'
import { formatMoney } from '@/utils/format'

export interface MoneyDisplayProps {
  amount?: number | string | null
  currency?: string
  /** Show + / - sign prefix */
  showSign?: boolean
  /** Apply red for negative / green for positive / neutral */
  colorType?: 'auto' | 'income' | 'expense' | 'neutral'
  bold?: boolean
  style?: React.CSSProperties
}

export const MoneyDisplay: React.FC<MoneyDisplayProps> = ({
  amount,
  currency = 'VND',
  showSign = false,
  colorType = 'neutral',
  bold = false,
  style,
}) => {
  const num = Number(amount ?? 0)
  const isPositive = num > 0
  const isNegative = num < 0

  let color: string | undefined

  if (colorType === 'auto') {
    if (isPositive) color = '#52c41a'
    else if (isNegative) color = '#ff4d4f'
  } else if (colorType === 'income') {
    color = '#52c41a'
  } else if (colorType === 'expense') {
    color = '#ff4d4f'
  }

  const formattedText = formatMoney(amount, currency, { showSign })

  return (
    <span
      style={{
        fontVariantNumeric: 'tabular-nums',
        fontWeight: bold ? 600 : 'normal',
        color,
        ...style,
      }}
    >
      {formattedText}
    </span>
  )
}
