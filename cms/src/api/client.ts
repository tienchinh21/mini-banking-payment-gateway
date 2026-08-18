import axios, { type AxiosError, type AxiosInstance, type InternalAxiosRequestConfig } from 'axios'
import { message } from 'antd'
import { STORAGE_KEYS } from '@/constants/common'
import { storage } from '@/utils/storage'
import { generateCorrelationId } from '@/utils/helper'
import { ApiError, type ApiResponse } from '@/types/api'

const baseURL = import.meta.env.VITE_API_BASE_URL || '/api/v1'

export const apiClient: AxiosInstance = axios.create({
  baseURL,
  timeout: 30000,
  headers: {
    'Content-Type': 'application/json',
    Accept: 'application/json',
  },
})

// Request Interceptor
apiClient.interceptors.request.use(
  (config: InternalAxiosRequestConfig) => {
    // Inject auth token if present
    const token = storage.get<string>(STORAGE_KEYS.ACCESS_TOKEN)
    if (token && config.headers) {
      config.headers.Authorization = `Bearer ${token}`
    }

    // Inject correlation ID for tracing
    if (config.headers && !config.headers['X-Correlation-Id']) {
      config.headers['X-Correlation-Id'] = generateCorrelationId()
    }

    return config
  },
  (error) => {
    return Promise.reject(error)
  }
)

// Response Interceptor
apiClient.interceptors.response.use(
  (response) => {
    // If API wraps responses in { success, data, message }
    const responseData = response.data as ApiResponse
    if (responseData && typeof responseData === 'object' && 'success' in responseData) {
      if (!responseData.success) {
        const errMsg = responseData.message || 'Yêu cầu không thành công'
        message.error(errMsg)
        return Promise.reject(new ApiError(errMsg, response.status, responseData.errors))
      }
    }
    return response
  },
  (error: AxiosError<ApiResponse>) => {
    const status = error.response?.status
    const errorData = error.response?.data
    let errorMessage = 'Đã có lỗi xảy ra, vui lòng thử lại sau.'

    if (errorData?.message) {
      errorMessage = errorData.message
    } else if (error.message === 'Network Error') {
      errorMessage = 'Không thể kết nối tới máy chủ (Network Error).'
    } else if (error.code === 'ECONNABORTED') {
      errorMessage = 'Quá thời gian yêu cầu máy chủ (Timeout).'
    } else if (status === 401) {
      errorMessage = 'Phiên đăng nhập đã hết hạn. Vui lòng đăng nhập lại.'
      storage.remove(STORAGE_KEYS.ACCESS_TOKEN)
      storage.remove(STORAGE_KEYS.USER_INFO)
      // Optional: redirect to login if auth is active
    } else if (status === 403) {
      errorMessage = 'Bạn không có quyền thực hiện thao tác này.'
    } else if (status === 404) {
      errorMessage = 'Không tìm thấy tài nguyên yêu cầu (404).'
    } else if (status === 500) {
      errorMessage = 'Lỗi hệ thống (Internal Server Error 500).'
    }

    message.error(errorMessage)

    return Promise.reject(
      new ApiError(errorMessage, status || 500, errorData?.errors)
    )
  }
)

/**
 * Standard HTTP helper methods
 */
export const http = {
  get: <T = any>(url: string, params?: Record<string, any>): Promise<ApiResponse<T>> =>
    apiClient.get(url, { params }).then((res) => res.data),

  post: <T = any>(url: string, data?: any, config?: any): Promise<ApiResponse<T>> =>
    apiClient.post(url, data, config).then((res) => res.data),

  put: <T = any>(url: string, data?: any): Promise<ApiResponse<T>> =>
    apiClient.put(url, data).then((res) => res.data),

  patch: <T = any>(url: string, data?: any): Promise<ApiResponse<T>> =>
    apiClient.patch(url, data).then((res) => res.data),

  delete: <T = any>(url: string, params?: Record<string, any>): Promise<ApiResponse<T>> =>
    apiClient.delete(url, { params }).then((res) => res.data),
}
