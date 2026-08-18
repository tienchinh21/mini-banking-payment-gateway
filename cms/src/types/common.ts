import type { FilterValue, SorterResult } from 'antd/es/table/interface'

export type TableDensity = 'small' | 'middle' | 'large'

export interface TableState<T = any> {
  page: number
  pageSize: number
  total: number
  filters?: Record<string, FilterValue | null>
  sorter?: SorterResult<T> | SorterResult<T>[]
  keyword?: string
  extraParams?: Record<string, any>
}

export interface OptionItem<T = string | number> {
  label: string
  value: T
  disabled?: boolean
  color?: string
  [key: string]: any
}

export type StatusTagType = 'success' | 'processing' | 'error' | 'warning' | 'default'

export interface BreadcrumbRouteItem {
  path?: string
  title: string
  icon?: React.ReactNode
  children?: BreadcrumbRouteItem[]
}
