import React, { useState } from 'react'
import {
  Row,
  Col,
  Card,
  Statistic,
  Typography,
  Button,
  message,
  Tag,
  Space,
  Modal,
  Descriptions,
  Badge,
  Tooltip,
} from 'antd'
import {
  WalletOutlined,
  DollarOutlined,
  CheckCircleOutlined,
  ShopOutlined,
  ReloadOutlined,
  DatabaseOutlined,
  CloudServerOutlined,
  ApiOutlined,
  ArrowRightOutlined,
  EyeOutlined,
  TransactionOutlined,
} from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { useNavigate } from 'react-router-dom'
import { PageContainer, AppTable, MoneyDisplay, StatusTag, ActionMenu, type AppTableColumns } from '@/components/core'
import { formatDate } from '@/utils/format'
import { dashboardService } from '../services/dashboardService'
import type { RecentPaymentItem } from '../types'

const { Text } = Typography

export const DashboardPage: React.FC = () => {
  const navigate = useNavigate()
  const [selectedPayment, setSelectedPayment] = useState<RecentPaymentItem | null>(null)
  const [detailModalOpen, setDetailModalOpen] = useState(false)

  // Fetch Dashboard Stats from GET /api/v1/admin/dashboard/stats
  const {
    data: statsData,
    refetch: refetchStats,
    isFetching: isStatsFetching,
  } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: () => dashboardService.getStats(),
  })

  // Fetch Health Check from GET /health
  const {
    data: healthData,
    refetch: refetchHealth,
    isFetching: isHealthFetching,
  } = useQuery({
    queryKey: ['system-health'],
    queryFn: () => dashboardService.getHealth(),
    refetchInterval: 30000, // auto refresh health every 30s
  })

  const isLoading = isStatsFetching || isHealthFetching

  const handleRefresh = async () => {
    await Promise.all([refetchStats(), refetchHealth()])
    message.success('Đã cập nhật dữ liệu mới nhất từ máy chủ')
  }

  const handleViewPayment = (payment: RecentPaymentItem) => {
    setSelectedPayment(payment)
    setDetailModalOpen(true)
  }

  // Health helpers
  const getHealthTag = (status?: string) => {
    const isHealthy = status?.toLowerCase() === 'healthy'
    const isDegraded = status?.toLowerCase() === 'degraded'
    return {
      color: isHealthy ? 'success' : isDegraded ? 'warning' : 'error',
      text: isHealthy ? 'Healthy' : isDegraded ? 'Degraded' : 'Unhealthy',
      isHealthy,
    }
  }

  const getCheckByName = (name: string) => {
    return healthData?.checks?.find((c: { name: string }) => c.name.toLowerCase().includes(name.toLowerCase()))
  }

  const postgresCheck = getCheckByName('postgres')
  const redisCheck = getCheckByName('redis')
  const rabbitCheck = getCheckByName('rabbit')
  const overallHealthy = healthData?.status?.toLowerCase() === 'healthy'
  const columns: AppTableColumns<RecentPaymentItem> = [
    {
      title: 'Mã GD (Payment ID)',
      dataIndex: 'id',
      key: 'id',
      width: 170,
      render: (text, record) => (
        <Space size={4}>
          <Text
            strong
            copyable
            style={{ color: '#1677ff', cursor: 'pointer' }}
            onClick={() => handleViewPayment(record)}
          >
            {text}
          </Text>
        </Space>
      ),
    },
    {
      title: 'Mã đơn hàng',
      dataIndex: 'orderId',
      key: 'orderId',
      width: 160,
      render: (orderId) => (
        <Text copyable style={{ fontSize: 13 }}>
          {orderId || '-'}
        </Text>
      ),
    },
    {
      title: 'Merchant',
      dataIndex: 'merchantName',
      key: 'merchantName',
      width: 180,
      render: (m) => <Text strong>{m}</Text>,
    },
    {
      title: 'Khách hàng',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 160,
    },
    {
      title: 'Số tiền',
      dataIndex: 'amount',
      key: 'amount',
      width: 160,
      align: 'right',
      render: (val, record) => (
        <MoneyDisplay amount={val} currency={record.currency || 'VND'} bold />
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 130,
      align: 'center',
      render: (status) => <StatusTag status={status} />,
    },
    {
      title: 'Thời gian tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date) => formatDate(date),
    },
  ]

  return (
    <PageContainer
      title="Tổng quan hệ thống"
      subTitle="Giám sát luồng tiền, số dư ví, trạng thái hạ tầng và giao dịch thanh toán theo thời gian thực"
      contained={false}
      extra={
        <Space>
          <Button
            type="default"
            icon={<TransactionOutlined />}
            onClick={() => navigate('/payments')}
          >
            Xem tất cả thanh toán
          </Button>
          <Button
            type="primary"
            icon={<ReloadOutlined />}
            loading={isLoading}
            onClick={handleRefresh}
          >
            Làm mới
          </Button>
        </Space>
      }
    >
      {/* Infrastructure Health Status Banner */}
      <Card
        size="small"
        bordered={false}
        style={{
          marginBottom: 20,
          borderRadius: 8,
          background: overallHealthy
            ? 'linear-gradient(135deg, #f6ffed 0%, #ffffff 100%)'
            : 'linear-gradient(135deg, #fff2f0 0%, #ffffff 100%)',
          border: overallHealthy ? '1px solid #b7eb8f' : '1px solid #ffccc7',
        }}
      >
        <Row align="middle" justify="space-between" gutter={[16, 12]}>
          <Col xs={24} md={8}>
            <Space size={10}>
              <Badge status={overallHealthy ? 'success' : 'error'} />
              <div>
                <Text strong style={{ fontSize: 14 }}>
                  Trạng thái hạ tầng hệ thống:
                </Text>{' '}
                <Tag color={overallHealthy ? 'success' : 'error'} style={{ fontWeight: 600 }}>
                  {overallHealthy ? 'ALL SYSTEMS OPERATIONAL' : 'SYSTEM DEGRADED'}
                </Tag>
              </div>
            </Space>
          </Col>

          <Col xs={24} md={16}>
            <Space wrap size={16} style={{ justifyContent: 'flex-end', width: '100%' }}>
              {/* PostgreSQL */}
              <Tooltip title={postgresCheck?.exception ? `Lỗi: ${postgresCheck.exception}` : 'Database chính lưu trữ Transaction & Ledger'}>
                <Space size={6}>
                  <DatabaseOutlined style={{ color: postgresCheck?.status === 'Healthy' ? '#52c41a' : '#ff4d4f' }} />
                  <Text style={{ fontSize: 13 }}>PostgreSQL:</Text>
                  <Tag color={getHealthTag(postgresCheck?.status).color}>
                    {getHealthTag(postgresCheck?.status).text}
                    {postgresCheck?.duration ? ` (${Math.round(postgresCheck.duration)}ms)` : ''}
                  </Tag>
                </Space>
              </Tooltip>

              {/* Redis */}
              <Tooltip title={redisCheck?.exception ? `Lỗi: ${redisCheck.exception}` : 'Bộ nhớ đệm & Distributed Lock / Idempotency'}>
                <Space size={6}>
                  <CloudServerOutlined style={{ color: redisCheck?.status === 'Healthy' ? '#52c41a' : '#ff4d4f' }} />
                  <Text style={{ fontSize: 13 }}>Redis:</Text>
                  <Tag color={getHealthTag(redisCheck?.status).color}>
                    {getHealthTag(redisCheck?.status).text}
                    {redisCheck?.duration ? ` (${Math.round(redisCheck.duration)}ms)` : ''}
                  </Tag>
                </Space>
              </Tooltip>

              {/* RabbitMQ */}
              <Tooltip title={rabbitCheck?.exception ? `Lỗi: ${rabbitCheck.exception}` : 'Message Broker luân chuyển Outbox & Webhooks'}>
                <Space size={6}>
                  <ApiOutlined style={{ color: rabbitCheck?.status === 'Healthy' ? '#52c41a' : '#ff4d4f' }} />
                  <Text style={{ fontSize: 13 }}>RabbitMQ:</Text>
                  <Tag color={getHealthTag(rabbitCheck?.status).color}>
                    {getHealthTag(rabbitCheck?.status).text}
                    {rabbitCheck?.duration ? ` (${Math.round(rabbitCheck.duration)}ms)` : ''}
                  </Tag>
                </Space>
              </Tooltip>
            </Space>
          </Col>
        </Row>
      </Card>

      {/* Real Metric Statistic Cards */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        {/* Card 1: Tổng số dư khả dụng */}
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} hoverable style={{ borderRadius: 8 }}>
            <Statistic
              title="Tổng số dư khả dụng"
              value={statsData?.totalBalance ?? 0}
              formatter={(val) => (
                <MoneyDisplay
                  amount={Number(val)}
                  currency="VND"
                  bold
                  style={{ fontSize: 24, color: '#1677ff' }}
                />
              )}
              prefix={<WalletOutlined style={{ color: '#1677ff', marginRight: 8 }} />}
            />
            <div style={{ marginTop: 8, fontSize: 12, color: '#8c8c8c' }}>
              Số ví kích hoạt: <Text strong>{statsData?.activeWallets ?? 0}</Text> tài khoản
            </div>
          </Card>
        </Col>

        {/* Card 2: Doanh số hôm nay */}
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} hoverable style={{ borderRadius: 8 }}>
            <Statistic
              title="Doanh số hôm nay"
              value={statsData?.dailyPayments ?? 0}
              formatter={(val) => (
                <MoneyDisplay
                  amount={Number(val)}
                  currency="VND"
                  bold
                  style={{ fontSize: 24, color: '#52c41a' }}
                />
              )}
              prefix={<DollarOutlined style={{ color: '#52c41a', marginRight: 8 }} />}
            />
            <div style={{ marginTop: 8, fontSize: 12, color: '#8c8c8c' }}>
              Tổng luồng tiền tích lũy: <MoneyDisplay amount={statsData?.totalVolume ?? 0} currency="VND" />
            </div>
          </Card>
        </Col>

        {/* Card 3: Tổng số GD & Tỷ lệ thành công */}
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} hoverable style={{ borderRadius: 8 }}>
            <Statistic
              title="Tổng số GD & Tỷ lệ thành công"
              value={statsData?.successRate ?? 100}
              precision={1}
              suffix="%"
              valueStyle={{ color: '#52c41a', fontSize: 24 }}
              prefix={<CheckCircleOutlined style={{ marginRight: 8 }} />}
            />
            <div style={{ marginTop: 8, fontSize: 12, color: '#8c8c8c' }}>
              Tổng cộng: <Text strong>{statsData?.totalPayments ?? 0}</Text> giao dịch
            </div>
          </Card>
        </Col>

        {/* Card 4: Đối tác Merchant đang hoạt động */}
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} hoverable style={{ borderRadius: 8 }}>
            <Statistic
              title="Đối tác Merchant đang hoạt động"
              value={statsData?.activeMerchants ?? 0}
              valueStyle={{ color: '#722ed1', fontSize: 24 }}
              prefix={<ShopOutlined style={{ marginRight: 8 }} />}
            />
            <div style={{ marginTop: 8, fontSize: 12, color: '#8c8c8c' }}>
              Cổng thanh toán & Webhook tích hợp
            </div>
          </Card>
        </Col>
      </Row>

      {/* Recent Payments Table */}
      <Card
        bordered={false}
        title={
          <Space>
            <TransactionOutlined style={{ color: '#1677ff' }} />
            <span>Giao dịch thanh toán gần nhất</span>
          </Space>
        }
        extra={
          <Button
            type="link"
            icon={<ArrowRightOutlined />}
            onClick={() => navigate('/payments')}
          >
            Xem tất cả
          </Button>
        }
        style={{ borderRadius: 8 }}
      >
        <AppTable<RecentPaymentItem>
          rowKey="id"
          columns={columns}
          dataSource={statsData?.recentPayments || []}
          loading={isStatsFetching}
          pagination={false}
          autoHeight={false}
          scrollY={360}
          actionColumn={{
            title: 'Thao tác',
            width: 110,
            render: (_, record) => (
              <ActionMenu
                items={[
                  {
                    key: 'view',
                    label: 'Chi tiết',
                    icon: <EyeOutlined />,
                    onClick: () => handleViewPayment(record),
                  },
                ]}
              />
            ),
          }}
        />
      </Card>

      {/* Payment Detail Modal */}
      <Modal
        title={
          <Space>
            <TransactionOutlined style={{ color: '#1677ff' }} />
            <span>Chi tiết giao dịch thanh toán</span>
          </Space>
        }
        open={detailModalOpen}
        onCancel={() => setDetailModalOpen(false)}
        footer={[
          <Button key="close" onClick={() => setDetailModalOpen(false)}>
            Đóng
          </Button>,
          <Button
            key="goto"
            type="primary"
            icon={<ArrowRightOutlined />}
            onClick={() => {
              setDetailModalOpen(false)
              navigate('/payments')
            }}
          >
            Quản lý thanh toán
          </Button>,
        ]}
        width={600}
      >
        {selectedPayment && (
          <Descriptions bordered column={1} size="small" style={{ marginTop: 16 }}>
            <Descriptions.Item label="Mã giao dịch (Payment ID)">
              <Text copyable strong>
                {selectedPayment.id}
              </Text>
            </Descriptions.Item>
            <Descriptions.Item label="Mã đơn hàng (Order ID)">
              <Text copyable>{selectedPayment.orderId || '-'}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Đối tác Merchant">
              <Text strong>{selectedPayment.merchantName}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Khách hàng">
              {selectedPayment.customerName}
            </Descriptions.Item>
            <Descriptions.Item label="Số tiền thanh toán">
              <MoneyDisplay
                amount={selectedPayment.amount}
                currency={selectedPayment.currency || 'VND'}
                bold
                style={{ fontSize: 16, color: '#1677ff' }}
              />
            </Descriptions.Item>
            <Descriptions.Item label="Trạng thái">
              <StatusTag status={selectedPayment.status} />
            </Descriptions.Item>
            <Descriptions.Item label="Thời gian khởi tạo">
              {formatDate(selectedPayment.createdAt)}
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>
    </PageContainer>
  )
}
