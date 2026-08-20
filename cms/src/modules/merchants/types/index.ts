export interface MerchantItem {
  id: string
  code: string
  name: string
  contactEmail: string
  apiKey: string
  status: 'ACTIVE' | 'SUSPENDED'
  webhookUrl: string
  createdAt: string
}

export interface MerchantFilterParams {
  keyword?: string
  page?: number
  pageSize?: number
}

export interface CreateMerchantFormData {
  code: string
  name: string
  contactEmail?: string
  webhookUrl?: string
}

export interface RegenerateKeyResult {
  id: string
  code: string
  apiKey: string
  secret: string
}
