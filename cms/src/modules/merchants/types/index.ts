export type MerchantStatus = 'ACTIVE' | 'SUSPENDED'

export interface MerchantItem {
  id: string
  code: string
  name: string
  contactEmail: string
  apiKey: string
  status: MerchantStatus | string
  webhookUrl: string
  createdAt: string
}

export interface MerchantFilterParams {
  keyword?: string
  status?: MerchantStatus
  page?: number
  pageSize?: number
}

export interface CreateMerchantDto {
  merchantId: string
  name: string
  webhookUrl?: string
}

export interface CreateMerchantFormData {
  code?: string
  merchantId?: string
  name: string
  contactEmail?: string
  webhookUrl?: string
}

export interface UpdateMerchantDto {
  name: string
  webhookUrl?: string
  isActive: boolean
}

export interface CreateMerchantResult {
  id: string
  code: string
  name: string
  apiKey: string
  secret: string
  webhookUrl?: string
  status: string
  createdAt: string
}

export interface RegenerateKeyResult {
  id: string
  code?: string
  apiKey: string
  secret: string
}

export interface RegenerateKeysResponse {
  id: string
  apiKey: string
  secret: string
}
