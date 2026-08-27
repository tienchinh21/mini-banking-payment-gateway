import React, { useState } from 'react'
import {
  Card,
  Form,
  Input,
  Button,
  Switch,
  Typography,
  message,
  Row,
  Col,
  Tabs,
  InputNumber,
  Select,
  Tag,
  Space,
  Badge,
  Descriptions,
  Popconfirm,
  Tooltip,
} from 'antd'
import {
  SaveOutlined,
  ReloadOutlined,
  LockOutlined,
  ApiOutlined,
  ThunderboltOutlined,
  SettingOutlined,
  UndoOutlined,
  DatabaseOutlined,
  SafetyCertificateOutlined,
} from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { PageContainer } from '@/components/core'
import { APP_CONFIG } from '@/constants/common'
import { settingsService } from '../services/settingsService'
import type { SystemSettingsConfig } from '../types'

const { Text, Title, Paragraph } = Typography

export const SettingsPage: React.FC = () => {
  const [form] = Form.useForm<SystemSettingsConfig>()
  const [activeTab, setActiveTab] = useState('security')

  // Fetch Live System Info from GET /api/v1/system/info
  const {
    data: systemInfo,
    refetch: refetchSystemInfo,
    isFetching: isInfoFetching,
  } = useQuery({
    queryKey: ['system-info'],
    queryFn: () => settingsService.getSystemInfo(),
  })

  // Fetch Live Infrastructure Health from GET /health
  const {
    data: healthData,
    refetch: refetchHealth,
    isFetching: isHealthFetching,
  } = useQuery({
    queryKey: ['settings-health'],
    queryFn: () => settingsService.getHealth(),
    refetchInterval: 30000,
  })

  // Initial settings loaded from localStorage
  const [currentSettings, setCurrentSettings] = useState<SystemSettingsConfig>(() =>
    settingsService.getSettings()
  )

  const isRefreshing = isInfoFetching || isHealthFetching

  const handleRefreshSystemStatus = async () => {
    await Promise.all([refetchSystemInfo(), refetchHealth()])
    message.success('Đã làm mới thông tin hệ thống và trạng thái hạ tầng')
  }

  const handleSave = (values: SystemSettingsConfig) => {
    settingsService.saveSettings(values)
    setCurrentSettings(values)
    message.success('Lưu cấu hình hệ thống thành công!')
  }

  const handleResetDefaults = () => {
    const defaults = settingsService.resetSettings()
    setCurrentSettings(defaults)
    form.setFieldsValue(defaults)
    message.info('Đã khôi phục cấu hình hệ thống về mặc định')
  }

  // Health helpers
  const getCheckByName = (name: string) => {
    return healthData?.checks?.find((c: { name: string }) => c.name.toLowerCase().includes(name.toLowerCase()))
  }

  const postgresCheck = getCheckByName('postgres')
  const redisCheck = getCheckByName('redis')
  const rabbitCheck = getCheckByName('rabbit')
  const isHealthy = healthData?.status?.toLowerCase() === 'healthy'

  const tabItems = [
    {
      key: 'security',
      label: (
        <span>
          <LockOutlined />
          Bảo mật (HMAC & JWT)
        </span>
      ),
      children: (
        <Card bordered={false} style={{ borderRadius: 8 }}>
          <Title level={5}>Cấu hình xác thực & Chữ ký số</Title>
          <Paragraph type="secondary" style={{ fontSize: 13, marginBottom: 20 }}>
            Quản lý cơ chế xác thực chữ ký HMAC-SHA256 cho các Merchant API gọi vào hệ thống và thời hạn mã hóa JWT.
          </Paragraph>

          <Row gutter={24}>
            <Col xs={24} md={12}>
              <Form.Item
                name={['security', 'enableHmacValidation']}
                label="Bắt buộc xác thực chữ ký HMAC cho Merchant API"
                valuePropName="checked"
                extra="Mọi request tạo thanh toán, hoàn tiền từ Merchant bắt buộc phải có header X-Signature"
              >
                <Switch checkedChildren="BẬT" unCheckedChildren="TẮT" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['security', 'requireNonceCheck']}
                label="Bảo vệ chống gửi lặp Nonce (Replay Attack)"
                valuePropName="checked"
                extra="Ngăn chặn kẻ gian gửi lại cùng 1 request đã bắt được trên đường truyền"
              >
                <Switch checkedChildren="BẬT" unCheckedChildren="TẮT" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['security', 'hmacAlgorithm']}
                label="Thuật toán mã hóa chữ ký (HMAC Algorithm)"
                rules={[{ required: true }]}
              >
                <Select
                  options={[
                    { label: 'HMAC-SHA256 (Khuyến nghị)', value: 'HmacSHA256' },
                    { label: 'HMAC-SHA512 (Bảo mật cao)', value: 'HmacSHA512' },
                  ]}
                />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['security', 'jwtExpiresInMinutes']}
                label="Thời hạn Access Token Admin (phút)"
                rules={[{ required: true }]}
              >
                <InputNumber min={5} max={1440} style={{ width: '100%' }} addonAfter="phút" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['security', 'maxTimestampDriftSeconds']}
                label="Độ lệch thời gian Timestamp tối đa (giây)"
                extra="Khoảng thời gian chấp nhận giữa timestamp trong header và đồng hồ máy chủ"
              >
                <InputNumber min={30} max={3600} style={{ width: '100%' }} addonAfter="giây" />
              </Form.Item>
            </Col>
          </Row>
        </Card>
      ),
    },
    {
      key: 'worker',
      label: (
        <span>
          <ThunderboltOutlined />
          Outbox & Worker Settings
        </span>
      ),
      children: (
        <Card bordered={false} style={{ borderRadius: 8 }}>
          <Title level={5}>Xử lý bất đồng bộ & Webhook Retry</Title>
          <Paragraph type="secondary" style={{ fontSize: 13, marginBottom: 20 }}>
            Điều chỉnh tham số tiến trình Outbox Dispatcher quét bảng sự kiện và phân phối Webhook tới Merchant.
          </Paragraph>

          <Row gutter={24}>
            <Col xs={24} md={12}>
              <Form.Item
                name={['worker', 'outboxBatchSize']}
                label="Outbox Batch Size (Số message / lần quét)"
                rules={[{ required: true }]}
                extra="Số lượng sự kiện Outbox tối đa lấy từ Database trong mỗi chu kỳ"
              >
                <InputNumber min={10} max={500} style={{ width: '100%' }} addonAfter="msg" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['worker', 'outboxIntervalMs']}
                label="Chu kỳ quét Outbox Publisher (mili-giây)"
                rules={[{ required: true }]}
                extra="Tần suất quét cơ sở dữ liệu để gửi sự kiện vào RabbitMQ"
              >
                <InputNumber min={100} max={10000} step={100} style={{ width: '100%' }} addonAfter="ms" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['worker', 'webhookMaxRetries']}
                label="Số lần retry tối đa cho Webhook callback"
                rules={[{ required: true }]}
                extra="Áp dụng Exponential Backoff (1s, 5s, 30s, 2m, 10m)"
              >
                <InputNumber min={1} max={10} style={{ width: '100%' }} addonAfter="lần" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['worker', 'webhookTimeoutSeconds']}
                label="Thời gian chờ phản hồi Webhook HTTP (giây)"
                rules={[{ required: true }]}
              >
                <InputNumber min={3} max={60} style={{ width: '100%' }} addonAfter="giây" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['worker', 'deadLetterQueueEnabled']}
                label="Kích hoạt Dead-Letter Queue (DLQ)"
                valuePropName="checked"
                extra="Lưu trữ sự kiện thất bại sau khi hết số lần retry để xử lý lại thủ công"
              >
                <Switch checkedChildren="BẬT" unCheckedChildren="TẮT" />
              </Form.Item>
            </Col>
          </Row>
        </Card>
      ),
    },
    {
      key: 'rateLimit',
      label: (
        <span>
          <ApiOutlined />
          Rate Limiting
        </span>
      ),
      children: (
        <Card bordered={false} style={{ borderRadius: 8 }}>
          <Title level={5}>Giới hạn tần suất gọi API (Rate Limiting)</Title>
          <Paragraph type="secondary" style={{ fontSize: 13, marginBottom: 20 }}>
            Kiểm soát lưu lượng truy cập chống DDOS và đảm bảo chất lượng dịch vụ (QoS) cho từng Merchant API key.
          </Paragraph>

          <Row gutter={24}>
            <Col xs={24} md={12}>
              <Form.Item
                name={['rateLimit', 'enableRateLimiter']}
                label="Kích hoạt Redis Distributed Rate Limiter"
                valuePropName="checked"
                extra="Bảo vệ hệ thống theo thuật toán Leaky Bucket / Sliding Window"
              >
                <Switch checkedChildren="BẬT" unCheckedChildren="TẮT" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['rateLimit', 'rateLimitStorage']}
                label="Bộ lưu trữ Rate Limit Counter"
                rules={[{ required: true }]}
              >
                <Select
                  options={[
                    { label: 'Redis Cluster (Phân tán đa instance)', value: 'redis' },
                    { label: 'In-Memory (Chỉ máy chủ hiện tại)', value: 'memory' },
                  ]}
                />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['rateLimit', 'rateLimitPerMinute']}
                label="Hạn mức tối đa (Request / Phút)"
                rules={[{ required: true }]}
                extra="Số lượng request tiêu chuẩn trong 60 giây của 1 Client IP / API Key"
              >
                <InputNumber min={10} max={10000} step={10} style={{ width: '100%' }} addonAfter="req/min" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['rateLimit', 'rateLimitBurst']}
                label="Dung lượng chịu tải đột biến (Burst Capacity)"
                rules={[{ required: true }]}
                extra="Số lượng request vượt ngưỡng cho phép trong tích tắc (Burst window)"
              >
                <InputNumber min={5} max={1000} style={{ width: '100%' }} addonAfter="req" />
              </Form.Item>
            </Col>
          </Row>
        </Card>
      ),
    },
    {
      key: 'general',
      label: (
        <span>
          <SettingOutlined />
          Thanh toán & Kế toán
        </span>
      ),
      children: (
        <Card bordered={false} style={{ borderRadius: 8 }}>
          <Title level={5}>Tham số thanh toán & Sổ cái kế toán</Title>
          <Paragraph type="secondary" style={{ fontSize: 13, marginBottom: 20 }}>
            Quy định các giá trị tiền tệ, hạn mức giao dịch ví và cấu hình thời gian quyết toán sổ cái định kỳ.
          </Paragraph>

          <Row gutter={24}>
            <Col xs={24} md={12}>
              <Form.Item
                name={['general', 'systemName']}
                label="Tên cổng thanh toán hệ thống"
                rules={[{ required: true }]}
              >
                <Input />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['general', 'defaultCurrency']}
                label="Đơn vị tiền tệ hạch toán sổ cái"
                rules={[{ required: true }]}
              >
                <Input disabled />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['general', 'maxDebitPerTxn']}
                label="Hạn mức trừ tiền tối đa / giao dịch (VND)"
                rules={[{ required: true }]}
                extra="Giao dịch thanh toán ví vượt quá số tiền này sẽ bị từ chối tự động"
              >
                <InputNumber
                  min={1000}
                  max={1000000000}
                  step={5000000}
                  formatter={(val) => `${val}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
                  style={{ width: '100%' }}
                  addonAfter="₫"
                />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['general', 'idempotencyTtlSeconds']}
                label="Thời gian lưu trữ Idempotency Key (giây)"
                rules={[{ required: true }]}
                extra="86400 giây = 24 giờ. Đảm bảo chống double-spending và thanh toán lặp"
              >
                <InputNumber min={60} max={604800} style={{ width: '100%' }} addonAfter="giây" />
              </Form.Item>
            </Col>

            <Col xs={24} md={12}>
              <Form.Item
                name={['general', 'autoSettlementHour']}
                label="Thời điểm tự động quyết toán hàng ngày (Giờ)"
                rules={[{ required: true }]}
                extra="Hệ thống tạo phiên Settlement cho các merchant vào khung giờ này"
              >
                <InputNumber min={0} max={23} style={{ width: '100%' }} addonAfter="giờ (0-23)" />
              </Form.Item>
            </Col>
          </Row>
        </Card>
      ),
    },
  ]

  return (
    <PageContainer
      title="Cấu hình hệ thống"
      subTitle="Thiết lập tham số thanh toán, webhook retry, bảo mật HMAC và kiểm soát hạ tầng"
      contained={false}
      extra={
        <Space>
          <Button
            icon={<ReloadOutlined />}
            loading={isRefreshing}
            onClick={handleRefreshSystemStatus}
          >
            Làm mới trạng thái
          </Button>
        </Space>
      }
    >
      <Row gutter={[24, 24]}>
        {/* Left Column: Settings Form with Tabs */}
        <Col xs={24} lg={16}>
          <Form
            form={form}
            layout="vertical"
            initialValues={currentSettings}
            onFinish={handleSave}
          >
            <Tabs
              activeKey={activeTab}
              onChange={setActiveTab}
              items={tabItems}
              style={{ background: '#fff', borderRadius: 8, padding: '12px 16px' }}
            />

            <div style={{ marginTop: 20, display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
              <Popconfirm
                title="Khôi phục cài đặt mặc định?"
                description="Tất cả thiết lập sẽ được đặt lại về giá trị gốc của hệ thống."
                onConfirm={handleResetDefaults}
                okText="Khôi phục"
                cancelText="Hủy"
              >
                <Button icon={<UndoOutlined />}>
                  Khôi phục mặc định
                </Button>
              </Popconfirm>

              <Button
                type="primary"
                htmlType="submit"
                icon={<SaveOutlined />}
                size="large"
                style={{ minWidth: 160 }}
              >
                Lưu cấu hình
              </Button>
            </div>
          </Form>
        </Col>

        {/* Right Column: Live Infrastructure & System Metadata */}
        <Col xs={24} lg={8}>
          <Space orientation="vertical" size={16} style={{ width: '100%' }}>
            {/* System Info Card */}
            <Card
              title={
                <Space>
                  <SafetyCertificateOutlined style={{ color: '#1677ff' }} />
                  <span>Thông tin phần mềm</span>
                </Space>
              }
              bordered={false}
              style={{ borderRadius: 8 }}
            >
              <Descriptions column={1} size="small" bordered>
                <Descriptions.Item label="Hệ thống CMS">
                  <Text strong>{APP_CONFIG.NAME}</Text>
                </Descriptions.Item>
                <Descriptions.Item label="Phiên bản CMS">
                  <Tag color="blue">{APP_CONFIG.SYSTEM_VERSION}</Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Tên Backend API">
                  <Text>{systemInfo?.name || 'Mini Banking API'}</Text>
                </Descriptions.Item>
                <Descriptions.Item label="Framework Backend">
                  <Tag color="geekblue">{systemInfo?.framework || '.NET 8 ASP.NET Core'}</Tag>
                </Descriptions.Item>
                <Descriptions.Item label="Môi trường chạy">
                  <Tag color={systemInfo?.environment === 'Production' ? 'magenta' : 'green'}>
                    {systemInfo?.environment || 'Development'}
                  </Tag>
                </Descriptions.Item>
              </Descriptions>
            </Card>

            {/* Infrastructure Health Card */}
            <Card
              title={
                <Space>
                  <DatabaseOutlined style={{ color: isHealthy ? '#52c41a' : '#ff4d4f' }} />
                  <span>Trạng thái dịch vụ hạ tầng</span>
                </Space>
              }
              extra={
                <Tag color={isHealthy ? 'success' : 'error'} style={{ fontWeight: 600 }}>
                  {isHealthy ? 'HEALTHY' : 'UNHEALTHY'}
                </Tag>
              }
              bordered={false}
              style={{ borderRadius: 8 }}
            >
              <Descriptions column={1} size="small" bordered>
                {/* PostgreSQL Database */}
                <Descriptions.Item label="PostgreSQL Database">
                  <Space align="center">
                    <Badge status={postgresCheck?.status === 'Healthy' ? 'success' : 'error'} />
                    <Text strong>
                      {postgresCheck?.status === 'Healthy' ? 'Sẵn sàng' : 'Mất kết nối'}
                    </Text>
                    {postgresCheck?.duration ? (
                      <Tag color="default" style={{ fontSize: 11 }}>
                        {Math.round(postgresCheck.duration)}ms
                      </Tag>
                    ) : null}
                  </Space>
                </Descriptions.Item>

                {/* Redis Distributed Cache & Lock */}
                <Descriptions.Item label="Redis Cache & Locks">
                  <Space align="center">
                    <Badge status={redisCheck?.status === 'Healthy' ? 'success' : 'error'} />
                    <Text strong>
                      {redisCheck?.status === 'Healthy' ? 'Hoạt động' : 'Mất kết nối'}
                    </Text>
                    {redisCheck?.duration ? (
                      <Tag color="default" style={{ fontSize: 11 }}>
                        {Math.round(redisCheck.duration)}ms
                      </Tag>
                    ) : null}
                  </Space>
                </Descriptions.Item>

                {/* RabbitMQ Message Broker */}
                <Descriptions.Item label="RabbitMQ Broker">
                  <Space align="center">
                    <Badge status={rabbitCheck?.status === 'Healthy' ? 'success' : 'error'} />
                    <Text strong>
                      {rabbitCheck?.status === 'Healthy' ? 'Đang chạy' : 'Mất kết nối'}
                    </Text>
                    {rabbitCheck?.duration ? (
                      <Tag color="default" style={{ fontSize: 11 }}>
                        {Math.round(rabbitCheck.duration)}ms
                      </Tag>
                    ) : null}
                  </Space>
                </Descriptions.Item>

                {/* Latency check */}
                <Descriptions.Item label="Thời gian phản hồi Health">
                  <Text code>{Math.round(healthData?.totalDuration || 0)} ms</Text>
                </Descriptions.Item>
              </Descriptions>

              <div style={{ marginTop: 12, textAlign: 'right' }}>
                <Tooltip title="Tự động kiểm tra sức khỏe mỗi 30 giây">
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    Auto-polling: 30s
                  </Text>
                </Tooltip>
              </div>
            </Card>
          </Space>
        </Col>
      </Row>
    </PageContainer>
  )
}
