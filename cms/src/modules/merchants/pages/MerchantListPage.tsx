import React, { useState } from 'react'
import {
  Form,
  Input,
  Button,
  Modal,
  message,
  Typography,
  Col,
  Descriptions,
  Tag,
  Alert,
} from 'antd'
import { PlusOutlined, KeyOutlined, EyeOutlined, CheckCircleOutlined } from '@ant-design/icons'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  PageContainer,
  AppTable,
  AppFilter,
  StatusTag,
  ActionMenu,
  type AppTableColumns,
} from '@/components/core'
import { useTable } from '@/hooks/useTable'
import { formatDate } from '@/utils/format'
import { merchantService } from '../services/merchantService'
import type {
  MerchantItem,
  CreateMerchantFormData,
  RegenerateKeyResult,
} from '../types'

const { Text, Paragraph } = Typography

export const MerchantListPage: React.FC = () => {
  const queryClient = useQueryClient()
  const [filterForm] = Form.useForm()
  const [createModalVisible, setCreateModalVisible] = useState(false)
  const [detailModalVisible, setDetailModalVisible] = useState(false)
  const [selectedMerchant, setSelectedMerchant] = useState<MerchantItem | null>(null)
  const [keyResultModalVisible, setKeyResultModalVisible] = useState(false)
  const [generatedKeys, setGeneratedKeys] = useState<RegenerateKeyResult | null>(null)
  const [createForm] = Form.useForm<CreateMerchantFormData>()

  const {
    queryParams,
    pagination,
    setTotal,
    setKeyword,
    handleTableChange,
    handleReset,
  } = useTable<MerchantItem>({
    defaultPageSize: 10,
  })

  // 1. Query Merchants list
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['merchants-list', queryParams],
    queryFn: async () => {
      const res = await merchantService.getMerchants(queryParams)
      setTotal(res.meta.totalItems)
      return res
    },
  })

  // 2. Mutation Create Merchant
  const createMutation = useMutation({
    mutationFn: (values: CreateMerchantFormData) => merchantService.createMerchant(values),
    onSuccess: (res: any) => {
      message.success('Tạo đối tác Merchant thành công!')
      setCreateModalVisible(false)
      createForm.resetFields()
      queryClient.invalidateQueries({ queryKey: ['merchants-list'] })

      if (res?.data?.apiKey && res?.data?.secret) {
        setGeneratedKeys({
          id: res.data.id,
          code: res.data.code,
          apiKey: res.data.apiKey,
          secret: res.data.secret,
        })
        setKeyResultModalVisible(true)
      }
    },
    onError: (err: any) => {
      message.error(err?.message || 'Không thể tạo Merchant')
    },
  })

  // 3. Mutation Regenerate API Keys
  const regenKeysMutation = useMutation({
    mutationFn: (id: string) => merchantService.regenerateKeys(id),
    onSuccess: (res: any) => {
      message.success('Cấp lại API Key & Secret thành công!')
      queryClient.invalidateQueries({ queryKey: ['merchants-list'] })

      const payload = res?.data || res
      if (payload?.apiKey) {
        setGeneratedKeys(payload)
        setKeyResultModalVisible(true)
      }
    },
    onError: (err: any) => {
      message.error(err?.message || 'Không thể cấp lại API Key')
    },
  })

  const handleOpenDetail = (record: MerchantItem) => {
    setSelectedMerchant(record)
    setDetailModalVisible(true)
  }

  const columns: AppTableColumns<MerchantItem> = [
    {
      title: 'Mã Merchant Code',
      dataIndex: 'code',
      key: 'code',
      width: 170,
      render: (code) => <Text copyable strong>{code}</Text>,
    },
    {
      title: 'Tên đối tác',
      dataIndex: 'name',
      key: 'name',
      width: 200,
      render: (name) => <Text strong>{name}</Text>,
    },
    {
      title: 'Email liên hệ',
      dataIndex: 'contactEmail',
      key: 'contactEmail',
      width: 200,
    },
    {
      title: 'API Key (HMAC)',
      dataIndex: 'apiKey',
      key: 'apiKey',
      width: 240,
      render: (key) => <Text copyable code>{key}</Text>,
    },
    {
      title: 'Webhook URL Callback',
      dataIndex: 'webhookUrl',
      key: 'webhookUrl',
      width: 260,
      render: (url) => (url ? <Text copyable ellipsis>{url}</Text> : <Text type="secondary">-</Text>),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 130,
      align: 'center',
      render: (status) => <StatusTag status={status} useBadge />,
    },
    {
      title: 'Ngày tham gia',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date) => formatDate(date),
    },
  ]

  return (
    <PageContainer
      title="Quản lý Đối tác Merchant"
      subTitle="Cấu hình thông tin tích hợp Payment Gateway, API Key và Webhook Callback"
      extra={
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => setCreateModalVisible(true)}
        >
          Thêm Merchant mới
        </Button>
      }
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
            <Input placeholder="Mã merchant, tên đối tác, email..." allowClear />
          </Form.Item>
        </Col>
      </AppFilter>

      <AppTable<MerchantItem>
        rowKey="id"
        columns={columns}
        dataSource={data?.items || []}
        loading={isLoading}
        pagination={pagination}
        onChange={handleTableChange}
        onRefresh={() => refetch()}
        actionColumn={{
          title: 'Thao tác',
          width: 140,
          fixed: 'right',
          render: (_, record) => (
            <ActionMenu
              items={[
                {
                  key: 'view',
                  label: 'Chi tiết',
                  icon: <EyeOutlined />,
                  onClick: () => handleOpenDetail(record),
                },
                {
                  key: 'regen',
                  label: 'Cấp lại API Key',
                  icon: <KeyOutlined />,
                  confirm: {
                    title: 'Xác nhận cấp lại API Key?',
                    description: `Hành động này sẽ tạo cặp API Key & Secret mới và vô hiệu hóa khóa cũ của ${record.code}.`,
                  },
                  onClick: () => regenKeysMutation.mutate(record.id || record.code),
                },
              ]}
            />
          ),
        }}
      />

      {/* Modal Create Merchant */}
      <Modal
        title="Thêm Đối tác Merchant Mới"
        open={createModalVisible}
        onCancel={() => setCreateModalVisible(false)}
        onOk={() => createForm.submit()}
        confirmLoading={createMutation.isPending}
        destroyOnClose
      >
        <Form
          form={createForm}
          layout="vertical"
          onFinish={(values) => createMutation.mutate(values)}
        >
          <Form.Item
            name="code"
            label="Mã định danh Merchant (Merchant Code)"
            rules={[
              { required: true, message: 'Vui lòng nhập mã Merchant' },
              { pattern: /^[A-Za-z0-9_-]+$/, message: 'Mã chỉ chứa chữ cái, số, gạch ngang và gạch dưới' },
            ]}
          >
            <Input placeholder="Ví dụ: MCH-TIKI, MCH-SHOPEE" />
          </Form.Item>

          <Form.Item
            name="name"
            label="Tên thương mại Merchant"
            rules={[{ required: true, message: 'Vui lòng nhập tên đối tác' }]}
          >
            <Input placeholder="Ví dụ: Tiki Corporation, Shopee Mall..." />
          </Form.Item>

          <Form.Item
            name="contactEmail"
            label="Email nhận thông báo kỹ thuật"
            rules={[
              { type: 'email', message: 'Email không đúng định dạng' },
            ]}
          >
            <Input placeholder="tech@merchant.com" />
          </Form.Item>

          <Form.Item
            name="webhookUrl"
            label="Webhook URL Endpoint (Tùy chọn)"
          >
            <Input placeholder="https://merchant.com/api/payment-callback" />
          </Form.Item>
        </Form>
      </Modal>

      {/* Modal Merchant Detail */}
      <Modal
        title={`Thông tin chi tiết Merchant: ${selectedMerchant?.name}`}
        open={detailModalVisible}
        onCancel={() => setDetailModalVisible(false)}
        footer={[
          <Button key="close" type="primary" onClick={() => setDetailModalVisible(false)}>
            Đóng
          </Button>,
        ]}
      >
        {selectedMerchant && (
          <Descriptions column={1} bordered size="small">
            <Descriptions.Item label="ID hệ thống">
              <Text code>{selectedMerchant.id}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Mã Merchant">
              <Text copyable strong>{selectedMerchant.code}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Tên đối tác">{selectedMerchant.name}</Descriptions.Item>
            <Descriptions.Item label="Email liên hệ">{selectedMerchant.contactEmail}</Descriptions.Item>
            <Descriptions.Item label="API Key hiện tại">
              <Text copyable code>{selectedMerchant.apiKey}</Text>
            </Descriptions.Item>
            <Descriptions.Item label="Webhook Callback URL">
              {selectedMerchant.webhookUrl ? (
                <Text copyable>{selectedMerchant.webhookUrl}</Text>
              ) : (
                <Text type="secondary">Chưa cấu hình</Text>
              )}
            </Descriptions.Item>
            <Descriptions.Item label="Trạng thái">
              <Tag color={selectedMerchant.status === 'ACTIVE' ? 'success' : 'error'}>
                {selectedMerchant.status}
              </Tag>
            </Descriptions.Item>
            <Descriptions.Item label="Ngày tạo">
              {formatDate(selectedMerchant.createdAt)}
            </Descriptions.Item>
          </Descriptions>
        )}
      </Modal>

      {/* Modal Key Generation Result */}
      <Modal
        title="Thông tin Chứng thực API (API Credentials)"
        open={keyResultModalVisible}
        onCancel={() => setKeyResultModalVisible(false)}
        footer={[
          <Button key="ok" type="primary" onClick={() => setKeyResultModalVisible(false)}>
            Tôi đã lưu thông tin
          </Button>,
        ]}
      >
        <Alert
          message="Lưu ý bảo mật quan trọng"
          description="Khóa Secret (HMAC Secret) chỉ được hiển thị một lần duy nhất tại đây. Hãy lưu trữ an toàn để cấu hình ký HMAC-SHA256 cho các cuộc gọi Payment API."
          type="warning"
          showIcon
          icon={<CheckCircleOutlined />}
          style={{ marginBottom: 16 }}
        />

        {generatedKeys && (
          <div style={{ background: '#f8fafc', padding: 16, borderRadius: 8, border: '1px solid #e2e8f0' }}>
            <div style={{ marginBottom: 12 }}>
              <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
                Mã Merchant:
              </Text>
              <Text strong copyable>{generatedKeys.code}</Text>
            </div>

            <div style={{ marginBottom: 12 }}>
              <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
                API Key (Header X-Api-Key):
              </Text>
              <Paragraph copyable code style={{ marginBottom: 0 }}>
                {generatedKeys.apiKey}
              </Paragraph>
            </div>

            <div>
              <Text type="secondary" style={{ display: 'block', fontSize: 12 }}>
                HMAC Secret Key:
              </Text>
              <Paragraph copyable code style={{ marginBottom: 0, color: '#cf1322' }}>
                {generatedKeys.secret}
              </Paragraph>
            </div>
          </div>
        )}
      </Modal>
    </PageContainer>
  )
}
