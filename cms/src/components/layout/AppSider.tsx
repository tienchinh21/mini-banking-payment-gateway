import React from 'react'
import { Layout, Menu, Typography } from 'antd'
import { useLocation, useNavigate } from 'react-router-dom'
import {
  DashboardOutlined,
  WalletOutlined,
  PayCircleOutlined,
  BookOutlined,
  ShopOutlined,
  AuditOutlined,
  SettingOutlined,
  BankOutlined,
} from '@ant-design/icons'
import type { ItemType, MenuItemType } from 'antd/es/menu/interface'

const { Sider } = Layout
const { Title } = Typography

export interface AppSiderProps {
  collapsed: boolean
  onCollapse: (collapsed: boolean) => void
}

export const menuItems: ItemType<MenuItemType>[] = [
  {
    key: '/dashboard',
    icon: <DashboardOutlined />,
    label: 'Tổng quan (Dashboard)',
  },
  {
    key: '/accounts',
    icon: <WalletOutlined />,
    label: 'Tài khoản & Ví',
  },
  {
    key: '/payments',
    icon: <PayCircleOutlined />,
    label: 'Thanh toán (Payments)',
  },
  {
    key: '/ledger',
    icon: <BookOutlined />,
    label: 'Sổ cái kép (Ledger)',
  },
  {
    key: '/merchants',
    icon: <ShopOutlined />,
    label: 'Đối tác Merchant',
  },
  {
    key: '/audit-logs',
    icon: <AuditOutlined />,
    label: 'Audit & Tracing',
  },
  {
    key: '/settings',
    icon: <SettingOutlined />,
    label: 'Cấu hình hệ thống',
  },
]

export const AppSider: React.FC<AppSiderProps> = ({ collapsed, onCollapse }) => {
  const location = useLocation()
  const navigate = useNavigate()

  // Determine active menu item key
  const selectedKey = React.useMemo(() => {
    const pathname = location.pathname
    if (pathname === '/' || pathname === '') return '/dashboard'
    const found = menuItems.find((item) => item?.key && pathname.startsWith(String(item.key)))
    return found ? String(found.key) : pathname
  }, [location.pathname])

  const handleMenuClick = ({ key }: { key: string }) => {
    navigate(key)
  }

  return (
    <Sider
      collapsible
      collapsed={collapsed}
      onCollapse={onCollapse}
      trigger={null}
      width={240}
      theme="dark"
      style={{
        overflow: 'auto',
        height: '100vh',
        position: 'sticky',
        top: 0,
        left: 0,
        zIndex: 101,
        boxShadow: '2px 0 8px 0 rgba(29, 35, 41, 0.05)',
      }}
    >
      <div
        style={{
          height: 64,
          display: 'flex',
          alignItems: 'center',
          justifyContent: collapsed ? 'center' : 'flex-start',
          padding: collapsed ? '0' : '0 20px',
          background: 'rgba(255, 255, 255, 0.04)',
          borderBottom: '1px solid rgba(255, 255, 255, 0.08)',
          gap: 12,
        }}
      >
        <BankOutlined style={{ fontSize: 24, color: '#1677ff' }} />
        {!collapsed && (
          <Title
            level={5}
            style={{
              color: '#ffffff',
              margin: 0,
              fontWeight: 700,
              letterSpacing: 0.5,
              whiteSpace: 'nowrap',
            }}
          >
            MINI BANKING
          </Title>
        )}
      </div>

      <Menu
        theme="dark"
        mode="inline"
        selectedKeys={[selectedKey]}
        items={menuItems}
        onClick={handleMenuClick}
        style={{ marginTop: 8 }}
      />
    </Sider>
  )
}
