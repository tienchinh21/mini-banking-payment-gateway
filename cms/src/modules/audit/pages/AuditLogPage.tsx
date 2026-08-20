import React from 'react'
import { Form, Input, Typography, Tag, Col } from 'antd'
import { useQuery } from '@tanstack/react-query'
import { PageContainer, AppTable, AppFilter, type AppTableColumns } from '@/components/core'
import { useTable } from '@/hooks/useTable'
import { formatDate } from '@/utils/format'
import { auditService } from '../services/auditService'
import type { AuditLogItem } from '../types'

const { Text } = Typography

export const AuditLogPage: React.FC = () => {
  const [filterForm] = Form.useForm()

  const {
    queryParams,
    pagination,
    setTotal,
    setKeyword,
    handleTableChange,
    handleReset,
  } = useTable<AuditLogItem>({
    defaultPageSize: 10,
  })

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['audit-logs', queryParams],
    queryFn: async () => {
      const res = await auditService.getAuditLogs(queryParams)
      setTotal(res.meta.totalItems)
      return res
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
