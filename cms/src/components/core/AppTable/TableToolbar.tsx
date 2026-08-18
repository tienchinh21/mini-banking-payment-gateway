import React from 'react'
import { Button, Tooltip, Space, Dropdown, type MenuProps } from 'antd'
import {
  ReloadOutlined,
  ColumnHeightOutlined,
  FullscreenOutlined,
  FullscreenExitOutlined,
} from '@ant-design/icons'
import type { TableDensity } from '@/types/common'

interface TableToolbarProps {
  title?: React.ReactNode
  extra?: React.ReactNode
  density: TableDensity
  onDensityChange: (density: TableDensity) => void
  onRefresh?: () => void
  isFullscreen: boolean
  onToggleFullscreen: () => void
}

export const TableToolbar: React.FC<TableToolbarProps> = ({
  title,
  extra,
  density,
  onDensityChange,
  onRefresh,
  isFullscreen,
  onToggleFullscreen,
}) => {
  const densityMenuItems: MenuProps['items'] = [
    {
      key: 'large',
      label: 'Mặc định (Large)',
      onClick: () => onDensityChange('large'),
    },
    {
      key: 'middle',
      label: 'Vừa phải (Middle)',
      onClick: () => onDensityChange('middle'),
    },
    {
      key: 'small',
      label: 'Nhỏ gọn (Small / Compact)',
      onClick: () => onDensityChange('small'),
    },
  ]

  return (
    <div
      style={{
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        marginBottom: 16,
        flexWrap: 'wrap',
        gap: 12,
      }}
    >
      <div style={{ fontWeight: 600, fontSize: 16, color: '#1f1f1f' }}>
        {title}
      </div>

      <Space size="middle" wrap>
        {extra}

        <Space size="small">
          {onRefresh && (
            <Tooltip title="Làm mới dữ liệu">
              <Button
                type="text"
                shape="circle"
                icon={<ReloadOutlined />}
                onClick={onRefresh}
              />
            </Tooltip>
          )}

          <Dropdown menu={{ items: densityMenuItems, selectedKeys: [density] }} trigger={['click']}>
            <Tooltip title="Mật độ hiển thị">
              <Button type="text" shape="circle" icon={<ColumnHeightOutlined />} />
            </Tooltip>
          </Dropdown>

          <Tooltip title={isFullscreen ? 'Thu nhỏ' : 'Toàn màn hình'}>
            <Button
              type="text"
              shape="circle"
              icon={isFullscreen ? <FullscreenExitOutlined /> : <FullscreenOutlined />}
              onClick={onToggleFullscreen}
            />
          </Tooltip>
        </Space>
      </Space>
    </div>
  )
}
