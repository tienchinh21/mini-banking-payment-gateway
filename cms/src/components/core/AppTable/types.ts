import type { ReactNode } from 'react'
import type { TableProps, TablePaginationConfig } from 'antd'
import type { ColumnType } from 'antd/es/table'

export interface AppTableColumn<T = any> extends ColumnType<T> {
  // Option to hide column by default
  hidden?: boolean
}

export type AppTableColumns<T = any> = AppTableColumn<T>[]

export interface ActionColumnConfig<T = any> {
  title?: string
  width?: number | string
  fixed?: 'left' | 'right' | boolean
  render: (value: any, record: T, index: number) => ReactNode
}

export interface AppTableProps<T = any> extends Omit<TableProps<T>, 'columns'> {
  columns: AppTableColumns<T>
  /** Custom action column configured with fixed right by default */
  actionColumn?: ActionColumnConfig<T>
  /** Enable dynamic calculation of table scroll height based on viewport */
  autoHeight?: boolean
  /** Custom scroll Y height override */
  scrollY?: number | string
  /** Custom scroll X width override, default is 'max-content' */
  scrollX?: number | string
  /** Table title or toolbar header */
  titleText?: ReactNode
  /** Extra toolbar actions (e.g. Export, Add button) */
  toolbarExtra?: ReactNode
  /** Callback for refresh button in toolbar */
  onRefresh?: () => void
  /** Show/hide default toolbar tools (density, refresh, fullscreen) */
  showToolbar?: boolean
  /** Pagination config */
  pagination?: TablePaginationConfig | false
}
