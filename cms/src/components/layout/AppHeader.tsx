import React, { useState } from 'react'
import {
  Layout,
  Button,
  Avatar,
  Dropdown,
  Space,
  Typography,
  Badge,
  Tag,
  Modal,
  Descriptions,
  App,
  type MenuProps,
} from 'antd'
import {
  MenuFoldOutlined,
  MenuUnfoldOutlined,
  UserOutlined,
  LogoutOutlined,
  SettingOutlined,
  BellOutlined,
  SafetyCertificateOutlined,
  MailOutlined,
  IdcardOutlined,
} from '@ant-design/icons'
import { useNavigate } from 'react-router-dom'
import { APP_CONFIG } from '@/constants/common'
import { useAuth } from '@/modules/auth'

const { Header } = Layout
const { Text } = Typography

export interface AppHeaderProps {
  collapsed: boolean
  onToggleCollapse: () => void
}

export const AppHeader: React.FC<AppHeaderProps> = ({ collapsed, onToggleCollapse }) => {
  const { user, logout } = useAuth()
  const { modal, message } = App.useApp()
  const navigate = useNavigate()
  const [profileModalVisible, setProfileModalVisible] = useState<boolean>(false)

  const displayName = user?.fullName || 'System Administrator'
  const displayRole = user?.role || 'Admin'
  const displayEmail = user?.email || 'admin@minibanking.local'

  const handleLogout = () => {
    modal.confirm({
      title: 'Xác nhận đăng xuất',
      icon: <LogoutOutlined style={{ color: '#ff4d4f' }} />,
      content: 'Bạn có chắc chắn muốn đăng xuất khỏi phiên làm việc hiện tại?',
      okText: 'Đăng xuất',
      cancelText: 'Hủy bỏ',
      okButtonProps: { danger: true },
      onOk: async () => {
        try {
          await logout()
          message.success('Đã đăng xuất thành công!')
          navigate('/login', { replace: true })
        } catch {
          message.error('Có lỗi xảy ra khi đăng xuất')
        }
      },
    })
  }

  const userMenuItems: MenuProps['items'] = [
    {
      key: 'user-info',
      disabled: true,
      label: (
        <div style={{ padding: '4px 0', minWidth: 180 }}>
          <Text strong style={{ display: 'block', color: '#1f1f1f' }}>
            {displayName}
          </Text>
          <Text type="secondary" style={{ fontSize: 12, display: 'block' }}>
            {displayEmail}
          </Text>
          <Tag
            color={displayRole === 'Admin' ? 'gold' : 'blue'}
            style={{ marginTop: 6, fontSize: 11 }}
          >
            {displayRole}
          </Tag>
        </div>
      ),
    },
    {
      type: 'divider',
    },
    {
      key: 'profile',
      icon: <UserOutlined />,
      label: 'Thông tin cá nhân',
      onClick: () => setProfileModalVisible(true),
    },
    {
      key: 'settings',
      icon: <SettingOutlined />,
      label: 'Cài đặt hệ thống',
      onClick: () => navigate('/settings'),
    },
    {
      type: 'divider',
    },
    {
      key: 'logout',
      icon: <LogoutOutlined />,
      danger: true,
      label: 'Đăng xuất',
      onClick: handleLogout,
    },
  ]

  return (
    <>
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

          <Dropdown menu={{ items: userMenuItems }} placement="bottomRight" arrow trigger={['click', 'hover']}>
            <Space style={{ cursor: 'pointer', padding: '4px 8px', borderRadius: 8 }}>
              <Avatar
                style={{
                  backgroundColor: displayRole === 'Admin' ? '#1677ff' : '#52c41a',
                  verticalAlign: 'middle',
                }}
                icon={<UserOutlined />}
              >
                {displayName.charAt(0)}
              </Avatar>
              <div style={{ display: 'flex', flexDirection: 'column', lineHeight: 1.2 }}>
                <Text strong style={{ fontSize: 13 }}>
                  {displayName}
                </Text>
                <Text type="secondary" style={{ fontSize: 11 }}>
                  {displayRole}
                </Text>
              </div>
            </Space>
          </Dropdown>
        </Space>
      </Header>

      {/* User Profile Modal */}
      <Modal
        title={
          <Space>
            <IdcardOutlined style={{ color: '#1677ff' }} />
            <span>Thông tin người dùng</span>
          </Space>
        }
        open={profileModalVisible}
        onCancel={() => setProfileModalVisible(false)}
        footer={[
          <Button key="close" type="primary" onClick={() => setProfileModalVisible(false)}>
            Đóng
          </Button>,
        ]}
        width={480}
      >
        <Descriptions
          bordered
          column={1}
          size="middle"
          style={{ marginTop: 16 }}
        >
          <Descriptions.Item label="Mã định danh (ID)">
            <Text code>{user?.id || 'usr_admin_001'}</Text>
          </Descriptions.Item>
          <Descriptions.Item label="Họ và tên">
            <Text strong>{displayName}</Text>
          </Descriptions.Item>
          <Descriptions.Item label="Email">
            <Space>
              <MailOutlined style={{ color: '#8c8c8c' }} />
              <Text>{displayEmail}</Text>
            </Space>
          </Descriptions.Item>
          <Descriptions.Item label="Vai trò / Phân quyền">
            <Tag color={displayRole === 'Admin' ? 'gold' : 'blue'}>
              <SafetyCertificateOutlined style={{ marginRight: 4 }} />
              {displayRole}
            </Tag>
          </Descriptions.Item>
          <Descriptions.Item label="Trạng thái tài khoản">
            <Tag color="success">HOẠT ĐỘNG (ACTIVE)</Tag>
          </Descriptions.Item>
        </Descriptions>
      </Modal>
    </>
  )
}
