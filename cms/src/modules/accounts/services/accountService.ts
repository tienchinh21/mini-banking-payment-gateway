import { http } from '@/api/client'
import { API_ENDPOINTS } from '@/api/endpoints'
import type { PaginatedResult } from '@/types/api'
import type { WalletAccountItem, AccountFilterParams, TopUpFormData } from '../types'

// Mock initial data if API server is not started
const mockAccounts: WalletAccountItem[] = [
  {
    id: 'wa-001',
    accountNumber: 'WA-8801928371',
    customerName: 'Nguyễn Văn An',
    email: 'an.nguyen@example.com',
    phone: '0912345678',
    currency: 'VND',
    availableBalance: 2500000,
    ledgerBalance: 2500000,
    status: 'ACTIVE',
    createdAt: '2026-08-01T08:30:00Z',
  },
  {
    id: 'wa-002',
    accountNumber: 'WA-8801928372',
    customerName: 'Trần Thị Bình',
    email: 'binh.tran@example.com',
    phone: '0987654321',
    currency: 'VND',
    availableBalance: 500000,
    ledgerBalance: 500000,
    status: 'ACTIVE',
    createdAt: '2026-08-05T10:15:00Z',
  },
  {
    id: 'wa-003',
    accountNumber: 'WA-8801928373',
    customerName: 'Lê Hoàng Cường',
    email: 'cuong.le@example.com',
    phone: '0903112233',
    currency: 'VND',
    availableBalance: 0,
    ledgerBalance: 0,
    status: 'FROZEN',
    createdAt: '2026-08-10T14:20:00Z',
  },
  {
    id: 'wa-004',
    accountNumber: 'WA-8801928374',
    customerName: 'Phạm Thu Dung',
    email: 'dung.pham@example.com',
    phone: '0938445566',
    currency: 'VND',
    availableBalance: 12000000,
    ledgerBalance: 12000000,
    status: 'ACTIVE',
    createdAt: '2026-08-12T09:00:00Z',
  },
  {
    id: 'wa-005',
    accountNumber: 'WA-8801928375',
    customerName: 'Vũ Đức Em',
    email: 'em.vu@example.com',
    phone: '0977889900',
    currency: 'VND',
    availableBalance: 850000,
    ledgerBalance: 850000,
    status: 'ACTIVE',
    createdAt: '2026-08-14T16:45:00Z',
  },
]

export const accountService = {
  async getAccounts(params?: AccountFilterParams): Promise<PaginatedResult<WalletAccountItem>> {
    try {
      const response = await http.get<PaginatedResult<WalletAccountItem>>(
        API_ENDPOINTS.ACCOUNTS.LIST,
        params
      )
      return response.data
    } catch {
      // Return mock data filtered locally if API is offline
      let list = [...mockAccounts]
      if (params?.keyword) {
        const kw = params.keyword.toLowerCase()
        list = list.filter(
          (a) =>
            a.accountNumber.toLowerCase().includes(kw) ||
            a.customerName.toLowerCase().includes(kw) ||
            a.email.toLowerCase().includes(kw) ||
            a.phone.includes(kw)
        )
      }
      if (params?.status) {
        list = list.filter((a) => a.status === params.status)
      }

      const page = params?.page || 1
      const pageSize = params?.pageSize || 10
      const start = (page - 1) * pageSize
      const items = list.slice(start, start + pageSize)

      return {
        items,
        meta: {
          currentPage: page,
          pageSize,
          totalItems: list.length,
          totalPages: Math.ceil(list.length / pageSize),
          hasNext: start + pageSize < list.length,
          hasPrevious: page > 1,
        },
      }
    }
  },

  async topUp(data: TopUpFormData) {
    try {
      return await http.post(API_ENDPOINTS.ACCOUNTS.TOP_UP, data)
    } catch {
      // Mock update
      const account = mockAccounts.find((a) => a.accountNumber === data.accountNumber)
      if (account) {
        account.availableBalance += Number(data.amount)
        account.ledgerBalance += Number(data.amount)
      }
      return { success: true, message: 'Nạp tiền thành công (Demo)' }
    }
  },

  async toggleFreeze(id: string, status: 'ACTIVE' | 'FROZEN') {
    try {
      return await http.post(API_ENDPOINTS.ACCOUNTS.FREEZE(id), { status })
    } catch {
      const account = mockAccounts.find((a) => a.id === id)
      if (account) {
        account.status = status === 'ACTIVE' ? 'FROZEN' : 'ACTIVE'
      }
      return { success: true, message: 'Cập nhật trạng thái ví thành công (Demo)' }
    }
  },
}
