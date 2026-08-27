import React, { useState } from 'react'
import {
  Form,
  Input,
  Typography,
  Tag,
  Col,
  Drawer,
  Descriptions,
  Space,
  Button,
  Card,
  Divider,
  Empty,
  Select,
} from 'antd'
import {
  AuditOutlined,
  EyeOutlined,
  CopyOutlined,
  SearchOutlined,
  ReloadOutlined,
  CheckCircleOutlined,
  CloseCircleOutlined,
  ApiOutlined,
} from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import {
  PageContainer,
  AppTable,
  AppFilter,
  ActionMenu,
  type AppTableColumns,
} from '@/components/core'
import { useTable } from '@/hooks/useTable'
import { formatDate } from '@/utils/format'
import { auditService } from '../services/auditService'
import type { AuditLogItem } from '../types'

const { Text } = Typography

export const AuditLogPage: React.FC = () => {
  const [filterForm] = Form.useForm()
  const [detailDrawerOpen, setDetailDrawerOpen] = useState(false)
  const [selectedLog, setSelectedLog] = useState<AuditLogItem | null>(null)

  const {
    queryParams,
    pagination,
    setTotal,
    setFilters,
    handleTableChange,
    handleReset,
  } = useTable<AuditLogItem>({
    defaultPageSize: 10,
  })

  // Query audit logs with pagination and filters
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['audit-logs', queryParams],
    queryFn: async () => {
      const res = await auditService.getAuditLogs(queryParams)
      setTotal(res.meta.totalItems)
      return res
    },
  })

  const handleOpenDetail = (log: AuditLogItem) => {
    setSelectedLog(log)
    setDetailDrawerOpen(true)
  }

  const handleSearch = (values: any) => {
    setFilters({
      keyword: values.keyword || undefined,
      correlationId: values.correlationId || undefined,
      actor: values.actor || undefined,
      action: values.action || undefined,
    })
  }

  const handleFilterReset = () => {
    filterForm.resetFields()
    handleReset()
  }

  // Format action badge color
  const getActionTagColor = (action: string) => {
    const act = action.toUpperCase()
    if (act.includes('CREATE') || act.includes('POST') || act.includes('INITIATED')) return 'green'
    if (act.includes('UPDATE') || act.includes('PUT') || act.includes('PATCH')) return 'orange'
    if (act.includes('DELETE')) return 'red'
    if (act.includes('READ') || act.includes('GET')) return 'blue'
    return 'purple'
  }

  // Format HTTP Method color
  const getMethodTagColor = (method?: string) => {
    const m = (method || '').toUpperCase()
    switch (m) {
      case 'GET':
        return 'blue'
      case 'POST':
        return 'green'
      case 'PUT':
      case 'PATCH':
        return 'orange'
      case 'DELETE':
        return 'red'
      default:
        return 'default'
    }
  }

  // Prettify JSON if applicable
  const formatRequestBody = (body?: string | null) => {
    if (!body) return 'Không có Request Body (N/A)'
    try {
      const parsed = JSON.parse(body)
      return JSON.stringify(parsed, null, 2)
    } catch {
      return body
    }
  }

  const columns: AppTableColumns<AuditLogItem> = [
    {
      title: 'Log ID',
      dataIndex: 'id',
      key: 'id',
      width: 130,
      render: (id, record) => (
        <TooltipText id={id} onClick={() => handleOpenDetail(record)} />
      ),
    },
    {
      title: 'Correlation ID (Trace)',
      dataIndex: 'correlationId',
      key: 'correlationId',
      width: 220,
      render: (trace) =>
        trace ? (
          <Text copyable strong style={{ fontSize: 13, color: '#1677ff' }}>
            {trace}
          </Text>
        ) : (
          <Text type="secondary">-</Text>
        ),
    },
    {
      title: 'Hành động (Action)',
      dataIndex: 'action',
      key: 'action',
      width: 160,
      render: (action) => (
        <Tag color={getActionTagColor(action)} style={{ fontWeight: 500 }}>
          {action}
        </Tag>
      ),
    },
    {
      title: 'Tác tử (Actor)',
      dataIndex: 'actor',
      key: 'actor',
      width: 180,
      render: (actor) => (
        <Text strong style={{ fontSize: 13 }}>
          {actor || 'Hệ thống'}
        </Text>
      ),
    },
    {
      title: 'IP Client',
      dataIndex: 'ipAddress',
      key: 'ipAddress',
      width: 140,
      render: (ip) => <Text code>{ip || '127.0.0.1'}</Text>,
    },
    {
      title: 'Endpoint / Resource',
      dataIndex: 'resource',
      key: 'resource',
      width: 240,
      ellipsis: true,
      render: (res, record) => (
        <Space size={4}>
          {record.method && (
            <Tag color={getMethodTagColor(record.method)} style={{ fontSize: 11 }}>
              {record.method}
            </Tag>
          )}
          <Text code style={{ fontSize: 12 }}>
            {record.path || res || '-'}
          </Text>
        </Space>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 120,
      align: 'center',
      render: (status) => {
        const isSuccess = status === 'SUCCESS' || status === 'OK'
        return (
          <Tag
            icon={isSuccess ? <CheckCircleOutlined /> : <CloseCircleOutlined />}
            color={isSuccess ? 'success' : 'error'}
          >
            {status}
          </Tag>
        )
      },
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
      subTitle="Theo dõi dấu vết hoạt động hệ thống, bảo mật phân quyền và truy vết Correlation ID"
      contained={false}
      extra={
        <Button icon={<ReloadOutlined />} loading={isLoading} onClick={() => refetch()}>
          Làm mới
        </Button>
      }
    >
      {/* Filters Form */}
      <AppFilter
        form={filterForm}
        onSearch={handleSearch}
        onReset={handleFilterReset}
      >
        <Col xs={24} sm={12} md={6}>
          <Form.Item name="keyword" label="Từ khóa chung" style={{ marginBottom: 0 }}>
            <Input
              placeholder="Nhập bất kỳ từ khóa..."
              prefix={<SearchOutlined style={{ color: '#bfbfbf' }} />}
              allowClear
            />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="correlationId" label="Correlation ID" style={{ marginBottom: 0 }}>
            <Input placeholder="Trace / Correlation ID..." allowClear />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="actor" label="Tác tử (Actor / Email)" style={{ marginBottom: 0 }}>
            <Input placeholder="admin@domain.com, MCH-..." allowClear />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="action" label="Loại hành động" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả hành động"
              allowClear
              options={[
                { label: 'Tất cả', value: '' },
                { label: 'Read (GET)', value: 'Read' },
                { label: 'Create (POST)', value: 'Create' },
                { label: 'Update (PUT/PATCH)', value: 'Update' },
                { label: 'Delete (DELETE)', value: 'Delete' },
              ]}
            />
          </Form.Item>
        </Col>
      </AppFilter>

      {/* Main Table */}
      <Card bordered={false} style={{ borderRadius: 8 }}>
        <AppTable<AuditLogItem>
          rowKey="id"
          columns={columns}
          dataSource={data?.items || []}
          loading={isLoading}
          pagination={pagination}
          onChange={handleTableChange}
          onRefresh={() => refetch()}
          actionColumn={{
            title: 'Thao tác',
            width: 120,
            render: (_, record) => (
              <ActionMenu
                items={[
                  {
                    key: 'detail',
                    label: 'Xem chi tiết Log',
                    icon: <EyeOutlined />,
                    onClick: () => handleOpenDetail(record),
                  },
                ]}
              />
            ),
          }}
        />
      </Card>

      {/* Audit Log Detail Drawer */}
      <Drawer
        title={
          <Space>
            <AuditOutlined style={{ color: '#1677ff' }} />
            <span>Chi tiết bản ghi kiểm toán (Audit Log)</span>
          </Space>
        }
        width={680}
        open={detailDrawerOpen}
        onClose={() => setDetailDrawerOpen(false)}
        extra={
          selectedLog?.correlationId ? (
            <Button
              icon={<CopyOutlined />}
              onClick={() => {
                navigator.clipboard.writeText(selectedLog.correlationId)
              }}
            >
              Sao chép Correlation ID
            </Button>
          ) : null
        }
      >
        {selectedLog ? (
          <div>
            <Descriptions bordered column={1} size="small">
              <Descriptions.Item label="Mã bản ghi (Log ID)">
                <Text copyable strong>
                  {selectedLog.id}
                </Text>
              </Descriptions.Item>

              <Descriptions.Item label="Correlation ID (Trace)">
                {selectedLog.correlationId ? (
                  <Text copyable strong style={{ color: '#1677ff' }}>
                    {selectedLog.correlationId}
                  </Text>
                ) : (
                  <Text type="secondary">N/A</Text>
                )}
              </Descriptions.Item>

              <Descriptions.Item label="Thời gian ghi nhận">
                {formatDate(selectedLog.timestamp)}
              </Descriptions.Item>

              <Descriptions.Item label="Hành động (Action)">
                <Tag color={getActionTagColor(selectedLog.action)} style={{ fontWeight: 600 }}>
                  {selectedLog.action}
                </Tag>
              </Descriptions.Item>

              <Descriptions.Item label="Tác tử thực hiện (Actor)">
                <Text strong>{selectedLog.actor || 'Hệ thống (System)'}</Text>
              </Descriptions.Item>

              <Descriptions.Item label="IP Client">
                <Text code>{selectedLog.ipAddress || '127.0.0.1'}</Text>
              </Descriptions.Item>

              <Descriptions.Item label="HTTP Request">
                <Space>
                  {selectedLog.method && (
                    <Tag color={getMethodTagColor(selectedLog.method)}>
                      {selectedLog.method}
                    </Tag>
                  )}
                  <Text code>{selectedLog.path || selectedLog.resource}</Text>
                </Space>
              </Descriptions.Item>

              <Descriptions.Item label="Mã phản hồi (Response Code)">
                <Space>
                  <Tag
                    color={
                      (selectedLog.responseStatusCode || 200) < 400
                        ? 'success'
                        : (selectedLog.responseStatusCode || 200) < 500
                        ? 'warning'
                        : 'error'
                    }
                    style={{ fontWeight: 600 }}
                  >
                    HTTP {selectedLog.responseStatusCode || (selectedLog.status === 'SUCCESS' ? 200 : 500)}
                  </Tag>
                  <Tag color={selectedLog.status === 'SUCCESS' ? 'success' : 'error'}>
                    {selectedLog.status}
                  </Tag>
                </Space>
              </Descriptions.Item>

              <Descriptions.Item label="Chi tiết tóm tắt">
                <Text>{selectedLog.details || '-'}</Text>
              </Descriptions.Item>
            </Descriptions>

            <Divider style={{ marginTop: 24, marginBottom: 12 }}>
              <Space>
                <ApiOutlined />
                <Text strong>Request Body (Payload)</Text>
              </Space>
            </Divider>

            <Card
              size="small"
              style={{
                background: '#1f1f1f',
                borderRadius: 6,
                border: '1px solid #303030',
              }}
            >
              <pre
                style={{
                  color: '#52c41a',
                  margin: 0,
                  fontSize: 12,
                  lineHeight: 1.5,
                  maxHeight: 300,
                  overflow: 'auto',
                  fontFamily: 'monospace',
                }}
              >
                {formatRequestBody(selectedLog.requestBody)}
              </pre>
            </Card>

            <div style={{ marginTop: 24, textAlign: 'right' }}>
              <Button type="primary" onClick={() => setDetailDrawerOpen(false)}>
                Đóng
              </Button>
            </div>
          </div>
        ) : (
          <Empty description="Không có thông tin chi tiết" />
        )}
      </Drawer>
    </PageContainer>
  )
}

// Sub-component for clickable Log ID with copy & tooltip
const TooltipText: React.FC<{ id: string; onClick: () => void }> = ({ id, onClick }) => {
  return (
    <Text
      code
      style={{ cursor: 'pointer', color: '#1677ff' }}
      onClick={onClick}
    >
      {id}
    </Text>
  )
}
