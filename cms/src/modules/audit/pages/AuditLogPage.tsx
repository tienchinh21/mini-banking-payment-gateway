import React from 'react'
import { Form, Input, Typography, Tag, Col } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { PageContainer, AppTable, AppFilter, type AppTableColumns } from '@/components/core'
import { useTable } from '@/hooks/useTable'
import { formatDate } from '@/utils/format'

const { Text } = Typography

interface AuditLogItem {
  id: string
  correlationId: string
  action: string
  actor: string
  ipAddress: string
  resource: string
  status: 'SUCCESS' | 'FAILURE'
  details: string
  timestamp: string
}

const mockLogs: AuditLogItem[] = [
  {
    id: 'LOG-001',
    correlationId: 'cms-1723719200-a1b2c3d',
    action: 'MERCHANT_PAYMENT_INITIATED',
    actor: 'MCH-ECOM-ALPHA',
    ipAddress: '10.0.1.45',
    resource: '/api/v1/merchant/payments',
    status: 'SUCCESS',
    details: 'Verified HMAC signature. Processed payment for order ORD-99881',
    timestamp: '2026-08-15T10:00:00Z',
  },
  {
    id: 'LOG-002',
    correlationId: 'cms-1723719200-e5f6g7h',
    action: 'WALLET_DEBIT_LOCK_ACQUIRED',
    actor: 'PAYMENT_WORKER',
    ipAddress: '127.0.0.1',
    resource: 'WalletAccount:WA-8801928371',
    status: 'SUCCESS',
    details: 'Pessimistic row lock acquired on Account WA-8801928371',
    timestamp: '2026-08-15T10:00:01Z',
  },
  {
    id: 'LOG-003',
    correlationId: 'cms-1723719200-i8j9k0l',
    action: 'OUTBOX_EVENT_PUBLISHED',
    actor: 'OUTBOX_DISPATCHER',
    ipAddress: '127.0.0.1',
    resource: 'RabbitMQ:payment.succeeded',
    status: 'SUCCESS',
    details: 'Event published to exchange mini_banking.events with routing key payment.succeeded',
    timestamp: '2026-08-15T10:00:02Z',
  },
]

export const AuditLogPage: React.FC = () => {
  const [filterForm] = Form.useForm()

  const {
    queryParams,
    pagination,
    setTotal,
    setKeyword,
    handleTableChange,
    handleReset,
  } = useTable<AuditLogItem>()

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['audit-logs', queryParams],
    queryFn: async () => {
      let list = [...mockLogs]
      if (queryParams.keyword) {
        const kw = queryParams.keyword.toLowerCase()
        list = list.filter(
          (l) =>
            l.action.toLowerCase().includes(kw) ||
            l.correlationId.toLowerCase().includes(kw) ||
            l.actor.toLowerCase().includes(kw)
        )
      }
      setTotal(list.length)
      return { items: list, total: list.length }
    },
  })

  const columns: AppTableColumns<AuditLogItem> = [
    {
      title: 'Log ID',
      dataIndex: 'id',
      key: 'id',
      width: 120,
      render: (id) => <Text code>{id}</Text>,
    },
    {
      title: 'Correlation ID (Trace)',
      dataIndex: 'correlationId',
      key: 'correlationId',
      width: 220,
      render: (trace) => <Text copyable strong style={{ fontSize: 13 }}>{trace}</Text>,
    },
    {
      title: 'Hành động (Action)',
      dataIndex: 'action',
      key: 'action',
      width: 230,
      render: (action) => <Tag color="blue">{action}</Tag>,
    },
    {
      title: 'Tác tử (Actor)',
      dataIndex: 'actor',
      key: 'actor',
      width: 170,
    },
    {
      title: 'IP Client',
      dataIndex: 'ipAddress',
      key: 'ipAddress',
      width: 130,
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      align: 'center',
      render: (status) => (
        <Tag color={status === 'SUCCESS' ? 'success' : 'error'}>{status}</Tag>
      ),
    },
    {
      title: 'Chi tiết sự kiện',
      dataIndex: 'details',
      key: 'details',
      width: 320,
      ellipsis: true,
    },
    {
      title: 'Thời gian',
      dataIndex: 'timestamp',
      key: 'timestamp',
      width: 170,
      render: (t) => formatDate(t),
    },
  ]

  return (
    <PageContainer
      title="Nhật ký kiểm toán & Tracing"
      subTitle="Theo dõi dấu vết hoạt động hệ thống qua Correlation ID và OpenTelemetry"
    >
      <AppFilter
        form={filterForm}
        onSearch={(vals) => setKeyword(vals.keyword || '')}
        onReset={() => {
          filterForm.resetFields()
          handleReset()
        }}
      >
        <Col xs={24} sm={12} md={8}>
          <Form.Item name="keyword" label="Tìm kiếm" style={{ marginBottom: 0 }}>
            <Input placeholder="Correlation ID, Action, Actor..." allowClear />
          </Form.Item>
        </Col>
      </AppFilter>

      <AppTable<AuditLogItem>
        rowKey="id"
        columns={columns}
        dataSource={data?.items || []}
        loading={isLoading}
        pagination={pagination}
        onChange={handleTableChange}
        onRefresh={() => refetch()}
      />
    </PageContainer>
  )
}
