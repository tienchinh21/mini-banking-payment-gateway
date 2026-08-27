import type { SystemHealthReport } from '@/modules/dashboard/types'

export type { SystemHealthReport }

export interface SystemInfo {
  name: string
  version: string
  framework: string
  environment: string
}

export interface SecuritySettings {
  enableHmacValidation: boolean
  hmacAlgorithm: string
  jwtExpiresInMinutes: number
  requireNonceCheck: boolean
  maxTimestampDriftSeconds: number
}

export interface WorkerSettings {
  outboxBatchSize: number
  outboxIntervalMs: number
  webhookMaxRetries: number
  webhookTimeoutSeconds: number
  deadLetterQueueEnabled: boolean
}

export interface RateLimitSettings {
  enableRateLimiter: boolean
  rateLimitPerMinute: number
  rateLimitBurst: number
  rateLimitStorage: 'redis' | 'memory'
}

export interface GeneralSettings {
  systemName: string
  defaultCurrency: string
  maxDebitPerTxn: number
  idempotencyTtlSeconds: number
  autoSettlementHour: number
}

export interface SystemSettingsConfig {
  general: GeneralSettings
  security: SecuritySettings
  worker: WorkerSettings
  rateLimit: RateLimitSettings
}
