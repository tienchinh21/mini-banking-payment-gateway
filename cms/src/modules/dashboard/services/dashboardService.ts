import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { DashboardStats, SystemHealthReport } from '../types'

export const dashboardService = {
  async getStats(): Promise<DashboardStats> {
    const response = await http.get<DashboardStats>(API_ENDPOINTS.DASHBOARD.STATS)
    return response.data
  },

  async getHealth(): Promise<SystemHealthReport> {
    const response = await http.get<SystemHealthReport>(API_ENDPOINTS.SYSTEM.HEALTH)
    return response.data
  },
}
