import React, { useState } from 'react'
import {
  Card,
  Form,
  Input,
  Button,
  Checkbox,
  Typography,
  Space,
  Divider,
  Tag,
  Alert,
  Tooltip,
  App,
} from 'antd'
import {
  LockOutlined,
  MailOutlined,
  BankOutlined,
  LoginOutlined,
  SafetyCertificateOutlined,
  ThunderboltOutlined,
  CrownOutlined,
  TeamOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons'
import { useNavigate, useLocation } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { APP_CONFIG } from '@/constants/common'
import type { LoginCredentials } from '../types'

const { Title, Text } = Typography

// Demo accounts for Quick Login
const DEMO_ACCOUNTS = [
  {
    roleTitle: 'Quản trị viên Hệ thống (System Admin)',
    role: 'Admin',
    email: 'admin@minibanking.local',
    password: 'Admin@123',
    icon: <CrownOutlined style={{ color: '#faad14' }} />,
    tagColor: 'gold',
    description: 'Toàn quyền cấu hình, ví tiền, sổ cái & merchant',
  },
  {
    roleTitle: 'Vận hành viên (Operator)',
    role: 'Operator',
    email: 'operator@minibanking.local',
    password: 'Admin@123',
    icon: <TeamOutlined style={{ color: '#1677ff' }} />,
    tagColor: 'blue',
    description: 'Quản trị tài khoản ví, phê duyệt và tra cứu giao dịch',
  },
]

export const LoginPage: React.FC = () => {
  const [form] = Form.useForm<LoginCredentials>()
  const [loading, setLoading] = useState<boolean>(false)
  const [quickLoginLoading, setQuickLoginLoading] = useState<string | null>(null)
  const [errorMessage, setErrorMessage] = useState<string | null>(null)

  const { login } = useAuth()
  const { message } = App.useApp()
  const navigate = useNavigate()
  const location = useLocation()

  // Get return redirect destination from router state
  const from = (location.state as any)?.from?.pathname || '/dashboard'

  const handleFinish = async (values: LoginCredentials) => {
    setLoading(true)
    setErrorMessage(null)
    try {
      const user = await login(values)
      message.success(`Đăng nhập thành công! Chào mừng ${user.fullName || user.email}`)
      navigate(from, { replace: true })
    } catch (err: any) {
      const msg = err?.message || 'Đăng nhập thất bại. Vui lòng kiểm tra lại thông tin.'
      setErrorMessage(msg)
      message.error(msg)
    } finally {
      setLoading(false)
    }
  }

  // Quick 1-click login action
  const handleQuickLogin = async (acc: (typeof DEMO_ACCOUNTS)[0]) => {
    form.setFieldsValue({
      email: acc.email,
      password: acc.password,
      remember: true,
    })
    setQuickLoginLoading(acc.email)
    setErrorMessage(null)
    try {
      await login({
        email: acc.email,
        password: acc.password,
        remember: true,
      })
      message.success(`Đăng nhập thành công với vai trò ${acc.roleTitle}!`)
      navigate(from, { replace: true })
    } catch (err: any) {
      const msg = err?.message || 'Đăng nhập thất bại.'
      setErrorMessage(msg)
    } finally {
      setQuickLoginLoading(null)
    }
  }

  // Fill credentials only into form
  const handleFillCredentials = (acc: (typeof DEMO_ACCOUNTS)[0]) => {
    form.setFieldsValue({
      email: acc.email,
      password: acc.password,
      remember: true,
    })
    message.info(`Đã điền thông tin tài khoản ${acc.role}`)
  }

  return (
    <div
      style={{
        minHeight: '100vh',
        width: '100%',
        display: 'flex',
        alignItems: 'center',
        justifyContent: 'center',
        background: 'linear-gradient(135deg, #0b192c 0%, #1e3e62 50%, #000000 100%)',
        position: 'relative',
        overflow: 'hidden',
        padding: '24px 16px',
      }}
    >
      {/* Decorative Background Circles */}
      <div
        style={{
          position: 'absolute',
          width: 500,
          height: 500,
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(22, 119, 255, 0.15) 0%, rgba(22, 119, 255, 0) 70%)',
          top: '-10%',
          left: '-10%',
          pointerEvents: 'none',
        }}
      />
      <div
        style={{
          position: 'absolute',
          width: 600,
          height: 600,
          borderRadius: '50%',
          background: 'radial-gradient(circle, rgba(82, 196, 26, 0.1) 0%, rgba(82, 196, 26, 0) 70%)',
          bottom: '-15%',
          right: '-10%',
          pointerEvents: 'none',
        }}
      />

      <Card
        style={{
          width: '100%',
          maxWidth: 480,
          borderRadius: 20,
          boxShadow: '0 24px 64px rgba(0, 0, 0, 0.45)',
          border: '1px solid rgba(255, 255, 255, 0.12)',
          background: 'rgba(255, 255, 255, 0.96)',
          backdropFilter: 'blur(16px)',
          overflow: 'hidden',
        }}
        styles={{
          body: {
            padding: '36px 32px 28px',
          },
        }}
      >
        {/* Brand Header */}
        <div style={{ textAlign: 'center', marginBottom: 28 }}>
          <div
            style={{
              display: 'inline-flex',
              alignItems: 'center',
              justifyContent: 'center',
              width: 64,
              height: 64,
              borderRadius: 18,
              background: 'linear-gradient(135deg, #1677ff 0%, #0958d9 100%)',
              color: '#ffffff',
              fontSize: 32,
              marginBottom: 14,
              boxShadow: '0 8px 24px rgba(22, 119, 255, 0.35)',
            }}
          >
            <BankOutlined />
          </div>

          <Title level={3} style={{ margin: 0, fontWeight: 700, letterSpacing: -0.5 }}>
            {APP_CONFIG.NAME}
          </Title>
          <Text type="secondary" style={{ fontSize: 13, marginTop: 4, display: 'block' }}>
            Hệ thống Quản trị Cổng Thanh toán & Sổ cái Kép
          </Text>
        </div>

        {errorMessage && (
          <Alert
            message={errorMessage}
            type="error"
            showIcon
            closable
            onClose={() => setErrorMessage(null)}
            style={{ marginBottom: 20, borderRadius: 8 }}
          />
        )}

        {/* Login Form */}
        <Form
          form={form}
          name="loginForm"
          layout="vertical"
          initialValues={{
            email: 'admin@minibanking.local',
            password: 'Admin@123',
            remember: true,
          }}
          onFinish={handleFinish}
          requiredMark={false}
          size="large"
        >
          <Form.Item
            name="email"
            label={<Text strong style={{ fontSize: 13 }}>Địa chỉ Email</Text>}
            rules={[
              { required: true, message: 'Vui lòng nhập email của bạn!' },
              { type: 'email', message: 'Định dạng email không hợp lệ!' },
            ]}
          >
            <Input
              prefix={<MailOutlined style={{ color: '#8c8c8c' }} />}
              placeholder="admin@minibanking.local"
              autoComplete="username"
              allowClear
            />
          </Form.Item>

          <Form.Item
            name="password"
            label={<Text strong style={{ fontSize: 13 }}>Mật khẩu</Text>}
            rules={[{ required: true, message: 'Vui lòng nhập mật khẩu của bạn!' }]}
          >
            <Input.Password
              prefix={<LockOutlined style={{ color: '#8c8c8c' }} />}
              placeholder="••••••••"
              autoComplete="current-password"
            />
          </Form.Item>

          <div
            style={{
              display: 'flex',
              justifyContent: 'space-between',
              alignItems: 'center',
              marginBottom: 20,
            }}
          >
            <Form.Item name="remember" valuePropName="checked" noStyle>
              <Checkbox>Ghi nhớ đăng nhập</Checkbox>
            </Form.Item>

            <Tooltip title="Mật khẩu mặc định trong hệ thống demo: Admin@123">
              <Button type="link" size="small" style={{ padding: 0, fontSize: 13 }}>
                Quên mật khẩu?
              </Button>
            </Tooltip>
          </div>

          <Form.Item style={{ marginBottom: 16 }}>
            <Button
              type="primary"
              htmlType="submit"
              block
              loading={loading}
              icon={<LoginOutlined />}
              style={{
                height: 44,
                borderRadius: 8,
                fontSize: 15,
                fontWeight: 600,
                background: 'linear-gradient(135deg, #1677ff 0%, #0958d9 100%)',
                boxShadow: '0 4px 14px rgba(22, 119, 255, 0.3)',
              }}
            >
              Đăng nhập Hệ thống
            </Button>
          </Form.Item>
        </Form>

        {/* Quick Login Section */}
        <Divider plain style={{ margin: '18px 0', fontSize: 12, color: '#8c8c8c' }}>
          <Space orientation="horizontal" size={4}>
            <ThunderboltOutlined style={{ color: '#faad14' }} />
            <span>Đăng nhập Nhanh (Quick Login)</span>
          </Space>
        </Divider>

        <Space direction="vertical" style={{ width: '100%' }} size={10}>
          {DEMO_ACCOUNTS.map((acc) => {
            const isLoggingIn = quickLoginLoading === acc.email
            return (
              <div
                key={acc.email}
                style={{
                  display: 'flex',
                  alignItems: 'center',
                  justifyContent: 'space-between',
                  padding: '10px 14px',
                  background: '#f8fafc',
                  border: '1px solid #e2e8f0',
                  borderRadius: 10,
                  transition: 'all 0.2s ease',
                }}
              >
                <div style={{ flex: 1, minWidth: 0, marginRight: 8 }}>
                  <div style={{ display: 'flex', alignItems: 'center', gap: 6, marginBottom: 2 }}>
                    {acc.icon}
                    <Text strong style={{ fontSize: 13 }}>
                      {acc.role}
                    </Text>
                    <Tag color={acc.tagColor} style={{ fontSize: 10, lineHeight: '18px', padding: '0 4px' }}>
                      {acc.email}
                    </Tag>
                  </div>
                  <Text type="secondary" style={{ fontSize: 11, display: 'block' }}>
                    {acc.description}
                  </Text>
                </div>

                <Space orientation="horizontal" size={6}>
                  <Tooltip title="Điền thông tin vào form">
                    <Button
                      size="small"
                      onClick={() => handleFillCredentials(acc)}
                      style={{ fontSize: 11 }}
                    >
                      Điền form
                    </Button>
                  </Tooltip>
                  <Button
                    type="primary"
                    size="small"
                    ghost
                    loading={isLoggingIn}
                    onClick={() => handleQuickLogin(acc)}
                    icon={<ThunderboltOutlined />}
                    style={{ fontSize: 11, fontWeight: 600 }}
                  >
                    1-Click Login
                  </Button>
                </Space>
              </div>
            )
          })}
        </Space>

        {/* Security Footer Notice */}
        <div
          style={{
            marginTop: 20,
            paddingTop: 14,
            borderTop: '1px solid #f0f0f0',
            textAlign: 'center',
          }}
        >
          <Space size={16}>
            <Text type="secondary" style={{ fontSize: 11, display: 'inline-flex', alignItems: 'center', gap: 4 }}>
              <SafetyCertificateOutlined style={{ color: '#52c41a' }} /> Bảo mật 256-bit SSL
            </Text>
            <Text type="secondary" style={{ fontSize: 11, display: 'inline-flex', alignItems: 'center', gap: 4 }}>
              <InfoCircleOutlined style={{ color: '#1677ff' }} /> Mini Banking v{APP_CONFIG.SYSTEM_VERSION}
            </Text>
          </Space>
        </div>
      </Card>
    </div>
  )
}

export default LoginPage
