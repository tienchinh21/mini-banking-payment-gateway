import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type {
  MerchantItem,
  MerchantFilterParams,
  CreateMerchantFormData,
  RegenerateKeyResult,
} from '../types'

export const merchantService = {
  async getMerchants(params?: MerchantFilterParams): Promise<PaginatedResult<MerchantItem>> {
    const response = await http.get<PaginatedResult<MerchantItem>>(
      API_ENDPOINTS.MERCHANTS.LIST,
      params
    )
    return response.data
  },

  async getMerchantDetail(id: string): Promise<MerchantItem> {
    const response = await http.get<MerchantItem>(API_ENDPOINTS.MERCHANTS.DETAIL(id))
    return response.data
  },

  async createMerchant(data: CreateMerchantFormData) {
    const response = await http.post(API_ENDPOINTS.MERCHANTS.CREATE, data)
    return response.data
  },

  async regenerateKeys(id: string): Promise<RegenerateKeyResult> {
    const response = await http.post<RegenerateKeyResult>(
      API_ENDPOINTS.MERCHANTS.REGENERATE_KEYS(id)
    )
    return response.data
  },
}
