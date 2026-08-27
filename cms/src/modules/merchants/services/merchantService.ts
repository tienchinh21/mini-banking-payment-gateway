import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type {
  MerchantItem,
  MerchantFilterParams,
  CreateMerchantDto,
  CreateMerchantResult,
  UpdateMerchantDto,
  RegenerateKeysResponse,
} from '../types'

export const merchantService = {
  async getMerchants(params?: MerchantFilterParams): Promise<PaginatedResult<MerchantItem>> {
    const response = await http.get<PaginatedResult<MerchantItem>>(
      API_ENDPOINTS.MERCHANTS.LIST,
      params
    )
    return response.data
  },

  async getMerchantDetail(id: string) {
    const response = await http.get(API_ENDPOINTS.MERCHANTS.DETAIL(id))
    return response.data
  },

  async createMerchant(data: CreateMerchantDto): Promise<CreateMerchantResult> {
    const response = await http.post<CreateMerchantResult>(
      API_ENDPOINTS.MERCHANTS.CREATE,
      data
    )
    return response.data
  },

  async updateMerchant(id: string, data: UpdateMerchantDto): Promise<MerchantItem> {
    const response = await http.put<MerchantItem>(
      API_ENDPOINTS.MERCHANTS.UPDATE(id),
      data
    )
    return response.data
  },

  async deleteMerchant(id: string): Promise<void> {
    await http.delete(API_ENDPOINTS.MERCHANTS.DELETE(id))
  },

  async regenerateKeys(id: string): Promise<RegenerateKeysResponse> {
    const response = await http.post<RegenerateKeysResponse>(
      API_ENDPOINTS.MERCHANTS.REGENERATE_KEYS(id)
    )
    return response.data
  },
}
