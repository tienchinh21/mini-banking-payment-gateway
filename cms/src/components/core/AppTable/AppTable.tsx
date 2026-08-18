import { useState, useMemo, useRef, type ReactElement } from 'react'
import { Table } from 'antd'
import type { ColumnsType } from 'antd/es/table'
import { useDynamicHeight } from '@/hooks/useDynamicHeight'
import type { TableDensity } from '@/types/common'
import { TableToolbar } from './TableToolbar'
import type { AppTableProps } from './types'

export function AppTable<T extends object = any>({
  columns,
  actionColumn,
  autoHeight = true,
  scrollY,
  scrollX = 'max-content',
  titleText,
  toolbarExtra,
  onRefresh,
  showToolbar = true,
  pagination,
  size: initialSize = 'middle',
  ...restProps
}: AppTableProps<T>): ReactElement {
  const containerRef = useRef<HTMLDivElement>(null)
  const defaultDensity: TableDensity =
    initialSize === 'small' || initialSize === 'middle' || initialSize === 'large'
      ? initialSize
      : 'middle'
  const [density, setDensity] = useState<TableDensity>(defaultDensity)
  const [isFullscreen, setIsFullscreen] = useState<boolean>(false)

  // Calculate dynamic scroll height if autoHeight is enabled and no explicit scrollY provided
  const dynamicHeight = useDynamicHeight(containerRef, {
    offsetBottom: pagination ? 140 : 80,
    minHeight: 300,
  })

  const computedScrollY = useMemo(() => {
    if (scrollY !== undefined) return scrollY
    if (autoHeight) return dynamicHeight
    return undefined
  }, [scrollY, autoHeight, dynamicHeight])

  // Build columns with fixed action column attached at the end
  const finalColumns = useMemo(() => {
    const visibleCols: ColumnsType<T> = columns
      .filter((col) => !col.hidden)
      .map((col) => ({
        ellipsis: col.ellipsis ?? true,
        ...col,
      }))

    if (actionColumn) {
      visibleCols.push({
        title: actionColumn.title || 'Thao tác',
        key: '__action__',
        width: actionColumn.width || 120,
        fixed: actionColumn.fixed ?? 'right',
        align: 'center',
        render: actionColumn.render,
      })
    }

    return visibleCols
  }, [columns, actionColumn])

  const handleToggleFullscreen = () => {
    if (!containerRef.current) return

    if (!document.fullscreenElement) {
      containerRef.current.requestFullscreen().then(() => setIsFullscreen(true)).catch(() => {})
    } else {
      document.exitFullscreen().then(() => setIsFullscreen(false)).catch(() => {})
    }
  }

  return (
    <div
      ref={containerRef}
      style={{
        width: '100%',
        background: isFullscreen ? '#ffffff' : 'transparent',
        padding: isFullscreen ? '20px' : 0,
        overflow: 'hidden',
      }}
    >
      {showToolbar && (
        <TableToolbar
          title={titleText}
          extra={toolbarExtra}
          density={density}
          onDensityChange={setDensity}
          onRefresh={onRefresh}
          isFullscreen={isFullscreen}
          onToggleFullscreen={handleToggleFullscreen}
        />
      )}

      <Table<T>
        columns={finalColumns}
        size={density}
        scroll={{
          x: scrollX,
          y: computedScrollY,
        }}
        pagination={
          pagination === false
            ? false
            : {
                showSizeChanger: true,
                showQuickJumper: true,
                ...pagination,
              }
        }
        {...restProps}
      />
    </div>
  )
}
