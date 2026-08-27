import axios from 'axios'
import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import { storage } from '@/utils/storage'
import { APP_CONFIG } from '@/constants/common'
import type {
  SystemInfo,
  SystemHealthReport,
  SystemSettingsConfig,
} from '../types'

const SETTINGS_STORAGE_KEY = 'mini_banking_system_settings'

export const DEFAULT_SETTINGS: SystemSettingsConfig = {
  general: {
    systemName: APP_CONFIG.NAME,
    defaultCurrency: 'VND',
    maxDebitPerTxn: 50000000,
    idempotencyTtlSeconds: 86400,
    autoSettlementHour: 23,
  },
  security: {
    enableHmacValidation: true,
    hmacAlgorithm: 'HmacSHA256',
    jwtExpiresInMinutes: 60,
    requireNonceCheck: true,
    maxTimestampDriftSeconds: 300,
  },
  worker: {
    outboxBatchSize: 50,
    outboxIntervalMs: 500,
    webhookMaxRetries: 5,
    webhookTimeoutSeconds: 15,
    deadLetterQueueEnabled: true,
  },
  rateLimit: {
    enableRateLimiter: true,
    rateLimitPerMinute: 120,
    rateLimitBurst: 30,
    rateLimitStorage: 'redis',
  },
}

export const settingsService = {
  /**
   * Fetch live system info from GET /api/v1/system/info
   */
  async getSystemInfo(): Promise<SystemInfo> {
    try {
      const response = await http.get<SystemInfo>(API_ENDPOINTS.SYSTEM.INFO)
      const data = response.data || {}
      return {
        name: (data as any).name || (data as any).Name || 'Mini Banking API',
        version: (data as any).version || (data as any).Version || '1.0.0',
        framework: (data as any).framework || (data as any).Framework || '.NET 8',
        environment: (data as any).environment || (data as any).Environment || 'Production',
      }
    } catch {
      return {
        name: 'Mini Banking API',
        version: '1.0.0',
        framework: '.NET 8 ASP.NET Core',
        environment: 'Development / Local',
      }
    }
  },

  /**
   * Fetch live health checks (PostgreSQL, Redis, RabbitMQ)
   */
  async getHealth(): Promise<SystemHealthReport> {
    try {
      const res = await axios.get<any>('/health', { timeout: 5000 })
      const rawData = res.data || {}
      const checks = (rawData.checks || rawData.Checks || []).map((c: any) => ({
        name: c.name || c.Name || '',
        status: c.status || c.Status || 'Unhealthy',
        duration: c.duration || c.Duration || 0,
        exception: c.exception || c.Exception || null,
      }))
      return {
        status: rawData.status || rawData.Status || 'Healthy',
        totalDuration: rawData.totalDuration || rawData.TotalDuration || 0,
        checks,
      }
    } catch {
      try {
        const response = await http.get<any>(API_ENDPOINTS.SYSTEM.HEALTH)
        const rawData = response.data || {}
        const checks = (rawData.checks || rawData.Checks || []).map((c: any) => ({
          name: c.name || c.Name || '',
          status: c.status || c.Status || 'Unhealthy',
          duration: c.duration || c.Duration || 0,
          exception: c.exception || c.Exception || null,
        }))
        return {
          status: rawData.status || rawData.Status || 'Healthy',
          totalDuration: rawData.totalDuration || rawData.TotalDuration || 0,
          checks,
        }
      } catch {
        return {
          status: 'Unhealthy',
          totalDuration: 0,
          checks: [
            { name: 'postgresql', status: 'Unhealthy', duration: 0, exception: 'Cannot connect to database' },
            { name: 'redis', status: 'Unhealthy', duration: 0, exception: 'Cannot connect to cache' },
            { name: 'rabbitmq', status: 'Unhealthy', duration: 0, exception: 'Cannot connect to broker' },
          ],
        }
      }
    }
  },

  /**
   * Load settings from localStorage or default
   */
  getSettings(): SystemSettingsConfig {
    const saved = storage.get<SystemSettingsConfig>(SETTINGS_STORAGE_KEY)
    if (!saved) {
      return DEFAULT_SETTINGS
    }
    return {
      general: { ...DEFAULT_SETTINGS.general, ...(saved.general || {}) },
      security: { ...DEFAULT_SETTINGS.security, ...(saved.security || {}) },
      worker: { ...DEFAULT_SETTINGS.worker, ...(saved.worker || {}) },
      rateLimit: { ...DEFAULT_SETTINGS.rateLimit, ...(saved.rateLimit || {}) },
    }
  },

  /**
   * Save system settings to localStorage
   */
  saveSettings(newSettings: SystemSettingsConfig): void {
    storage.set(SETTINGS_STORAGE_KEY, newSettings)
  },

  /**
   * Reset settings to default configuration
   */
  resetSettings(): SystemSettingsConfig {
    storage.set(SETTINGS_STORAGE_KEY, DEFAULT_SETTINGS)
    return DEFAULT_SETTINGS
  },
}
