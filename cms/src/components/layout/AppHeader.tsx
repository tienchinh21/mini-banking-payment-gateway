import React from 'react'
import { Layout, Button, Avatar, Dropdown, Space, Typography, Badge, type MenuProps } from 'antd'
import {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  UserOutlined,
  LogoutOutlined,
  SettingOutlined,
  BellOutlined,
} from '@ant-design/icons'
import { APP_CONFIG } from '@/constants/common'

const { Header } = Layout
const { Text } = Typography

export interface AppHeaderProps {
  collapsed: boolean
  onToggleCollapse: () => void
}

export const AppHeader: React.FC<AppHeaderProps> = ({ collapsed, onToggleCollapse }) => {
  const userMenuItems: MenuProps['items'] = [
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'Thông tin cá nhân',
    },
    {
      key: 'settings',
      icon: <SettingOutlined />,
      label: 'Cài đặt hệ thống',
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      danger: true,
      label: 'Đăng xuất',
      onClick: () => {
        // Handle logout
      },
    },
  ]

  return (
    <Header
      style={{
        background: '#ffffff',
        padding: '0 24px',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'space-between',
        borderBottom: '1px solid #f0f0f0',
        position: 'sticky',
        top: 0,
        zIndex: 100,
        height: 64,
      }}
    >
      <Space size="middle">
        <Button
          type="text"
          icon={collapsed ? <MenuUnfoldOutlined /> : <MenuFoldOutlined />}
          onClick={onToggleCollapse}
          style={{ fontSize: 16, width: 40, height: 40 }}
        />
        <Text strong style={{ fontSize: 16 }}>
          {APP_CONFIG.NAME}
        </Text>
      </Space>

      <Space size="large">
        <Badge count={0} dot>
          <Button type="text" shape="circle" icon={<BellOutlined />} />
        </Badge>

        <Dropdown menu={{ items: userMenuItems }} placement="bottomRight" arrow>
          <Space style={{ cursor: 'pointer' }}>
            <Avatar style={{ backgroundColor: '#1677ff' }} icon={<UserOutlined />} />
            <Text strong>Admin Operator</Text>
          </Space>
        </Dropdown>
      </Space>
    </Header>
  )
}
