import { useState, useCallback, useMemo } from 'react'
import type { TablePaginationConfig } from 'antd'
import type { FilterValue, SorterResult } from 'antd/es/table/interface'
import { DEFAULT_PAGINATION } from '@/constants/common'
import { cleanParams } from '@/utils/helper'

export interface UseTableOptions {
  defaultPage?: number
  defaultPageSize?: number
  defaultFilters?: Record<string, any>
  defaultKeyword?: string
}

export function useTable<T = any>(options: UseTableOptions = {}) {
  const {
    defaultPage = DEFAULT_PAGINATION.PAGE,
    defaultPageSize = DEFAULT_PAGINATION.PAGE_SIZE,
    defaultFilters = {},
    defaultKeyword = '',
  } = options

  const [page, setPage] = useState<number>(defaultPage)
  const [pageSize, setPageSize] = useState<number>(defaultPageSize)
  const [total, setTotal] = useState<number>(0)
  const [keyword, setKeyword] = useState<string>(defaultKeyword)
  const [filters, setFilters] = useState<Record<string, any>>(defaultFilters)
  const [sorter, setSorter] = useState<{
    field?: string
    order?: 'asc' | 'desc'
  }>({})

  // Handle table change (pagination, filters, sorter)
  const handleTableChange = useCallback(
    (
      newPagination: TablePaginationConfig,
      newFilters: Record<string, FilterValue | null>,
      newSorter: SorterResult<T> | SorterResult<T>[]
    ) => {
      if (newPagination.current) setPage(newPagination.current)
      if (newPagination.pageSize) setPageSize(newPagination.pageSize)

      // Normalize filters
      const cleanFilterValues: Record<string, any> = {}
      Object.keys(newFilters).forEach((key) => {
        if (newFilters[key] !== null) {
          cleanFilterValues[key] = newFilters[key]
        }
      })
      setFilters((prev) => ({ ...prev, ...cleanFilterValues }))

      // Normalize sorter
      if (!Array.isArray(newSorter) && newSorter.field) {
        setSorter({
          field: String(newSorter.field),
          order: newSorter.order === 'ascend' ? 'asc' : newSorter.order === 'descend' ? 'desc' : undefined,
        })
      } else {
        setSorter({})
      }
    },
    []
  )

  // Handle keyword search
  const handleSearch = useCallback((newKeyword: string) => {
    setKeyword(newKeyword)
    setPage(1) // Always reset to page 1 on search
  }, [])

  // Apply custom filters
  const handleFilterChange = useCallback((newFilters: Record<string, any>) => {
    setFilters(newFilters)
    setPage(1) // Always reset to page 1 on filter
  }, [])

  // Reset all filters & pagination
  const handleReset = useCallback(() => {
    setPage(defaultPage)
    setPageSize(defaultPageSize)
    setKeyword(defaultKeyword)
    setFilters(defaultFilters)
    setSorter({})
  }, [defaultPage, defaultPageSize, defaultKeyword, defaultFilters])

  // Formatted API Query Parameters
  const queryParams: Record<string, any> = useMemo(() => {
    return cleanParams({
      page,
      pageSize,
      keyword: keyword.trim() || undefined,
      sortBy: sorter.field,
      sortOrder: sorter.order,
      ...filters,
    })
  }, [page, pageSize, keyword, sorter, filters])

  // Antd Pagination configuration
  const paginationConfig: TablePaginationConfig = useMemo(
    () => ({
      current: page,
      pageSize,
      total,
      showSizeChanger: true,
      pageSizeOptions: [...DEFAULT_PAGINATION.PAGE_SIZE_OPTIONS],
      showTotal: (totalCount, range) =>
        `${range[0]}-${range[1]} / Tổng ${totalCount} bản ghi`,
      onChange: (newPage, newSize) => {
        setPage(newPage)
        setPageSize(newSize)
      },
    }),
    [page, pageSize, total]
  )

  return {
    page,
    setPage,
    pageSize,
    setPageSize,
    total,
    setTotal,
    keyword,
    setKeyword: handleSearch,
    filters,
    setFilters: handleFilterChange,
    sorter,
    setSorter,
    queryParams,
    pagination: paginationConfig,
    handleTableChange,
    handleReset,
  }
}
