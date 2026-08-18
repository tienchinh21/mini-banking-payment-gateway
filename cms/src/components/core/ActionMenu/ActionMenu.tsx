import React from 'react'
import { Button, Dropdown, Space, Popconfirm, Tooltip, type MenuProps } from 'antd'
import { MoreOutlined } from '@ant-design/icons'

export interface ActionItem {
  key: string
  label: React.ReactNode
  icon?: React.ReactNode
  onClick?: () => void
  disabled?: boolean
  danger?: boolean
  confirm?: {
    title: string
    description?: string
    okText?: string
    cancelText?: string
  }
}

export interface ActionMenuProps {
  items: ActionItem[]
  /** Maximum number of inline buttons to show before collapsing to More dropdown */
  maxInline?: number
  size?: 'small' | 'middle'
}

export const ActionMenu: React.FC<ActionMenuProps> = ({
  items,
  maxInline = 2,
  size = 'small',
}) => {
  if (!items || items.length === 0) return null

  const inlineItems = items.slice(0, maxInline)
  const dropdownItems = items.slice(maxInline)

  const menuProps: MenuProps = {
    items: dropdownItems.map((item) => ({
      key: item.key,
      label: item.confirm ? (
        <Popconfirm
          title={item.confirm.title}
          description={item.confirm.description}
          okText={item.confirm.okText || 'Đồng ý'}
          cancelText={item.confirm.cancelText || 'Hủy'}
          onConfirm={item.onClick}
        >
          <span style={{ width: '100%', display: 'inline-block' }}>{item.label}</span>
        </Popconfirm>
      ) : (
        item.label
      ),
      icon: item.icon,
      danger: item.danger,
      disabled: item.disabled,
      onClick: item.confirm ? undefined : item.onClick,
    })),
  }

  return (
    <Space size={4}>
      {inlineItems.map((item) => {
        if (item.confirm) {
          return (
            <Popconfirm
              key={item.key}
              title={item.confirm.title}
              description={item.confirm.description}
              okText={item.confirm.okText || 'Đồng ý'}
              cancelText={item.confirm.cancelText || 'Hủy'}
              onConfirm={item.onClick}
              disabled={item.disabled}
            >
              <Button
                type="link"
                size={size}
                danger={item.danger}
                icon={item.icon}
                disabled={item.disabled}
                style={{ padding: '0 4px' }}
              >
                {item.label}
              </Button>
            </Popconfirm>
          )
        }

        return (
          <Button
            key={item.key}
            type="link"
            size={size}
            danger={item.danger}
            icon={item.icon}
            disabled={item.disabled}
            onClick={item.onClick}
            style={{ padding: '0 4px' }}
          >
            {item.label}
          </Button>
        )
      })}

      {dropdownItems.length > 0 && (
        <Dropdown menu={menuProps} trigger={['click']} placement="bottomRight">
          <Tooltip title="Thao tác khác">
            <Button
              type="text"
              size={size}
              icon={<MoreOutlined />}
              style={{ padding: '0 4px' }}
            />
          </Tooltip>
        </Dropdown>
      )}
    </Space>
  )
}
