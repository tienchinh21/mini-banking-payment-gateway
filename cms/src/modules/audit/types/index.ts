export interface AuditLogItem {
  id: string
  correlationId: string
  action: string
  actor: string
  ipAddress: string
  resource: string
  status: 'SUCCESS' | 'FAILURE' | string
  details?: string
  method?: string
  path?: string
  responseStatusCode?: number
  requestBody?: string | null
  timestamp: string
  createdAt?: string
}

export interface AuditLogFilterParams {
  keyword?: string
  action?: string
  actor?: string
  correlationId?: string
  page?: number
  pageSize?: number
}
