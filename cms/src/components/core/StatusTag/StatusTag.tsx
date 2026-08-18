import React from 'react'
import { Tag, Badge } from 'antd'
import type { StatusConfig } from '@/constants/status'
import { PAYMENT_STATUS, LEDGER_TRANSACTION_TYPE, ACCOUNT_STATUS, MERCHANT_STATUS } from '@/constants/status'

export interface StatusTagProps {
  status?: string | number | null
  config?: Record<string, StatusConfig>
  customLabel?: string
  color?: string
  useBadge?: boolean
}

export const StatusTag: React.FC<StatusTagProps> = ({
  status,
  config,
  customLabel,
  color,
  useBadge = false,
}) => {
  if (status === undefined || status === null) {
    return <Tag>-</Tag>
  }

  const strStatus = String(status).toUpperCase()

  // Find status config across common dictionaries if not explicitly provided
  const resolvedConfig: StatusConfig | undefined =
    config?.[strStatus] ||
    PAYMENT_STATUS[strStatus] ||
    LEDGER_TRANSACTION_TYPE[strStatus] ||
    ACCOUNT_STATUS[strStatus] ||
    MERCHANT_STATUS[strStatus]

  const label = customLabel || resolvedConfig?.label || String(status)
  const tagColor = color || resolvedConfig?.color || 'default'

  if (useBadge) {
    const badgeStatus =
      resolvedConfig?.type === 'success'
        ? 'success'
        : resolvedConfig?.type === 'error'
        ? 'error'
        : resolvedConfig?.type === 'processing'
        ? 'processing'
        : resolvedConfig?.type === 'warning'
        ? 'warning'
        : 'default'

    return <Badge status={badgeStatus} text={label} />
  }

  return (
    <Tag color={tagColor} style={{ fontWeight: 500, borderRadius: 4 }}>
      {label}
    </Tag>
  )
}
