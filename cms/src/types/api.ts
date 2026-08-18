export interface ApiResponse<T = any> {
  success: boolean
  message: string
  data: T
  errors?: string[] | Record<string, string[]>
  timestamp?: string
}

export interface PaginationParams {
  page?: number
  pageSize?: number
  keyword?: string
  sortBy?: string
  sortOrder?: 'asc' | 'desc'
  [key: string]: any
}

export interface PaginationMeta {
  currentPage: number
  pageSize: number
  totalItems: number
  totalPages: number
  hasNext: boolean
  hasPrevious: boolean
}

export interface PaginatedResult<T> {
  items: T[]
  meta: PaginationMeta
}

export class ApiError extends Error {
  statusCode: number
  errors?: string[] | Record<string, string[]>

  constructor(message: string, statusCode = 500, errors?: string[] | Record<string, string[]>) {
    super(message)
    this.name = 'ApiError'
    this.statusCode = statusCode
    this.errors = errors
  }
}
