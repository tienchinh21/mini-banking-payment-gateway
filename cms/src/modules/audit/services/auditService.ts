import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type { AuditLogItem, AuditLogFilterParams } from '../types'

export const auditService = {
  async getAuditLogs(params?: AuditLogFilterParams): Promise<PaginatedResult<AuditLogItem>> {
    const response = await http.get<PaginatedResult<AuditLogItem>>(
      API_ENDPOINTS.AUDIT.LOGS,
      params
    )
    return response.data
  },
}
