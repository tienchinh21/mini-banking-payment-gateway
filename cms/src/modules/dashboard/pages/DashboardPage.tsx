import React from 'react'
import { Row, Col, Card, Statistic, Typography, Button, message } from 'antd'
import {
  WalletOutlined,
  PayCircleOutlined,
  CheckCircleOutlined,
  ShopOutlined,
  ReloadOutlined,
} from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import { PageContainer, AppTable, MoneyDisplay, StatusTag, ActionMenu, type AppTableColumns } from '@/components/core'
import { formatDate } from '@/utils/format'
import { dashboardService } from '../services/dashboardService'
import type { RecentPaymentItem } from '../types'

const { Text } = Typography

export const DashboardPage: React.FC = () => {
  // Fetch real system statistics
  const { data: statsData, refetch, isFetching } = useQuery({
    queryKey: ['dashboard-stats'],
    queryFn: () => dashboardService.getStats(),
  })

  const columns: AppTableColumns<RecentPaymentItem> = [
    {
      title: 'Mã GD (Payment ID)',
      dataIndex: 'id',
      key: 'id',
      width: 150,
      render: (text) => <Text strong>{text}</Text>,
    },
    {
      title: 'Mã đơn hàng',
      dataIndex: 'orderId',
      key: 'orderId',
      width: 170,
    },
    {
      title: 'Merchant',
      dataIndex: 'merchantName',
      key: 'merchantName',
      width: 200,
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
      width: 140,
      align: 'center',
      render: (status) => <StatusTag status={status} />,
    },
    {
      title: 'Thời gian',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date) => formatDate(date),
    },
  ]

  return (
    <PageContainer
      title="Tổng quan hệ thống"
      subTitle="Giám sát luồng tiền, số dư ví và giao dịch thanh toán theo thời gian thực"
      contained={false}
      extra={
        <Button
          icon={<ReloadOutlined />}
          loading={isFetching}
          onClick={() => {
            refetch()
            message.success('Đã cập nhật dữ liệu mới nhất')
          }}
        >
          Làm mới
        </Button>
      }
    >
      {/* Metric Cards */}
      <Row gutter={[16, 16]} style={{ marginBottom: 24 }}>
        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} hoverable style={{ borderRadius: 8 }}>
            <Statistic
              title="Tổng số dư ví hệ thống"
              value={statsData?.totalBalance ?? 0}
              formatter={(val) => (
                <MoneyDisplay amount={Number(val)} currency="VND" bold style={{ fontSize: 24 }} />
              )}
              prefix={<WalletOutlined style={{ color: '#1677ff', marginRight: 8 }} />}
            />
          </Card>
        </Col>

        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} hoverable style={{ borderRadius: 8 }}>
            <Statistic
              title="Doanh số thanh toán hôm nay"
              value={statsData?.dailyPayments ?? 0}
              formatter={(val) => (
                <MoneyDisplay amount={Number(val)} currency="VND" bold style={{ fontSize: 24 }} />
              )}
              prefix={<PayCircleOutlined style={{ color: '#52c41a', marginRight: 8 }} />}
            />
          </Card>
        </Col>

        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} hoverable style={{ borderRadius: 8 }}>
            <Statistic
              title="Tỷ lệ giao dịch thành công"
              value={statsData?.successRate ?? 100}
              precision={1}
              suffix="%"
              valueStyle={{ color: '#52c41a' }}
              prefix={<CheckCircleOutlined style={{ marginRight: 8 }} />}
            />
          </Card>
        </Col>

        <Col xs={24} sm={12} lg={6}>
          <Card bordered={false} hoverable style={{ borderRadius: 8 }}>
            <Statistic
              title="Đối tác Merchant tích hợp"
              value={statsData?.activeMerchants ?? 0}
              valueStyle={{ color: '#722ed1' }}
              prefix={<ShopOutlined style={{ marginRight: 8 }} />}
            />
          </Card>
        </Col>
      </Row>

      {/* Recent Payments Table */}
      <Card
        bordered={false}
        title="Giao dịch thanh toán gần nhất"
        style={{ borderRadius: 8 }}
      >
        <AppTable<RecentPaymentItem>
          rowKey="id"
          columns={columns}
          dataSource={statsData?.recentPayments || []}
          loading={isFetching}
          pagination={false}
          autoHeight={false}
          scrollY={320}
          actionColumn={{
            title: 'Thao tác',
            width: 100,
            render: (_, record) => (
              <ActionMenu
                items={[
                  {
                    key: 'view',
                    label: 'Chi tiết',
                    onClick: () => message.info(`Xem chi tiết GD: ${record.id}`),
                  },
                ]}
              />
            ),
          }}
        />
      </Card>
    </PageContainer>
  )
}
