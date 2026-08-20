export interface AuditLogItem {
  id: string
  correlationId: string
  action: string
  actor: string
  ipAddress: string
  resource: string
  status: 'SUCCESS' | 'FAILURE' | string
  details: string
  timestamp: string
}

export interface AuditLogFilterParams {
  keyword?: string
  page?: number
  pageSize?: number
}
