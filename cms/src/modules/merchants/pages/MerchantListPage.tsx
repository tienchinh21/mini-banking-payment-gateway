import React, { useState } from 'react'
import {
  Form,
  Input,
  Select,
  Button,
  Modal,
  Switch,
  Alert,
  Space,
  message,
  Typography,
  Col,
} from 'antd'
import {
  PlusOutlined,
  EditOutlined,
  KeyOutlined,
  DeleteOutlined,
  CopyOutlined,
  SafetyCertificateOutlined,
} from '@ant-design/icons'
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
  MerchantStatus,
  CreateMerchantDto,
  UpdateMerchantDto,
} from '../types'

const { Text, Paragraph } = Typography

interface CredentialsModalData {
  title: string
  merchantName: string
  merchantCode?: string
  apiKey: string
  secret: string
}

export const MerchantListPage: React.FC = () => {
  const queryClient = useQueryClient()
  const [filterForm] = Form.useForm()

  // Modal states
  const [createModalVisible, setCreateModalVisible] = useState(false)
  const [createForm] = Form.useForm<CreateMerchantDto>()

  const [editModalVisible, setEditModalVisible] = useState(false)
  const [editingMerchant, setEditingMerchant] = useState<MerchantItem | null>(null)
  const [editForm] = Form.useForm<{ name: string; webhookUrl?: string; isActive: boolean }>()

  const [credentialsData, setCredentialsData] = useState<CredentialsModalData | null>(null)

  const {
    queryParams,
    pagination,
    setTotal,
    setKeyword,
    setFilters,
    handleTableChange,
    handleReset,
  } = useTable<MerchantItem>({
    defaultPageSize: 10,
  })

  // ── 1. Fetch Merchants Query ──────────────────────────────────────────────
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['merchants-list', queryParams],
    queryFn: async () => {
      const result = await merchantService.getMerchants(queryParams)
      setTotal(result.meta.totalItems)
      return result
    },
  })

  // ── 2. Create Merchant Mutation ───────────────────────────────────────────
  const createMutation = useMutation({
    mutationFn: (data: CreateMerchantDto) => merchantService.createMerchant(data),
    onSuccess: (result) => {
      message.success(`Đã thêm Merchant đối tác "${result.name}" thành công!`)
      setCreateModalVisible(false)
      createForm.resetFields()
      queryClient.invalidateQueries({ queryKey: ['merchants-list'] })

      // Display one-time generated credentials modal
      setCredentialsData({
        title: 'Đăng ký Merchant thành công',
        merchantName: result.name,
        merchantCode: result.code,
        apiKey: result.apiKey,
        secret: result.secret,
      })
    },
  })

  // ── 3. Update Merchant Mutation ───────────────────────────────────────────
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: string; data: UpdateMerchantDto }) =>
      merchantService.updateMerchant(id, data),
    onSuccess: (result) => {
      message.success(`Cập nhật thông tin Merchant "${result.name}" thành công!`)
      setEditModalVisible(false)
      setEditingMerchant(null)
      editForm.resetFields()
      queryClient.invalidateQueries({ queryKey: ['merchants-list'] })
    },
  })

  // ── 4. Delete Merchant Mutation ────────────────────────────────────────────
  const deleteMutation = useMutation({
    mutationFn: (id: string) => merchantService.deleteMerchant(id),
    onSuccess: () => {
      message.success('Xoá Merchant thành công!')
      queryClient.invalidateQueries({ queryKey: ['merchants-list'] })
    },
  })

  // ── 5. Regenerate Keys Mutation ────────────────────────────────────────────
  const regenerateMutation = useMutation({
    mutationFn: (merchant: MerchantItem) => merchantService.regenerateKeys(merchant.id),
    onSuccess: (result, merchant) => {
      message.success(`Đã cấp lại API Key & Secret mới cho đối tác "${merchant.name}"!`)
      queryClient.invalidateQueries({ queryKey: ['merchants-list'] })

      // Display newly generated credentials
      setCredentialsData({
        title: 'Cấp lại API Key & Secret thành công',
        merchantName: merchant.name,
        merchantCode: merchant.code,
        apiKey: result.apiKey,
        secret: result.secret,
      })
    },
  })

  // ── Handlers ───────────────────────────────────────────────────────────────
  const handleOpenEdit = (record: MerchantItem) => {
    setEditingMerchant(record)
    editForm.setFieldsValue({
      name: record.name,
      webhookUrl: record.webhookUrl || '',
      isActive: record.status === 'ACTIVE',
    })
    setEditModalVisible(true)
  }

  const handleEditSubmit = () => {
    editForm.validateFields().then((values) => {
      if (!editingMerchant) return
      updateMutation.mutate({
        id: editingMerchant.id,
        data: {
          name: values.name.trim(),
          webhookUrl: values.webhookUrl?.trim() || undefined,
          isActive: values.isActive,
        },
      })
    })
  }

  const handleCreateSubmit = () => {
    createForm.validateFields().then((values) => {
      createMutation.mutate({
        merchantId: values.merchantId.trim(),
        name: values.name.trim(),
        webhookUrl: values.webhookUrl?.trim() || undefined,
      })
    })
  }

  const handleSearchSubmit = (values: { keyword?: string; status?: MerchantStatus }) => {
    setKeyword(values.keyword || '')
    setFilters({
      status: values.status,
    })
  }

  const handleFilterReset = () => {
    filterForm.resetFields()
    handleReset()
  }

  const handleCopyBothCredentials = async (apiKey: string, secret: string) => {
    const textToCopy = `API Key: ${apiKey}\nHMAC Secret: ${secret}`
    try {
      await navigator.clipboard.writeText(textToCopy)
      message.success('Đã sao chép cả API Key và Secret vào bộ nhớ tạm!')
    } catch {
      message.error('Không thể tự động sao chép, vui lòng sao chép thủ công.')
    }
  }

  // ── Table Columns ──────────────────────────────────────────────────────────
  const columns: AppTableColumns<MerchantItem> = [
    {
      title: 'Mã Merchant Code',
      dataIndex: 'code',
      key: 'code',
      width: 180,
      render: (code) => <Text copyable strong>{code}</Text>,
    },
    {
      title: 'Tên đối tác',
      dataIndex: 'name',
      key: 'name',
      width: 220,
      render: (name) => <Text strong>{name}</Text>,
    },
    {
      title: 'Email liên hệ',
      dataIndex: 'contactEmail',
      key: 'contactEmail',
      width: 200,
      render: (email) => <Text type="secondary">{email || '-'}</Text>,
    },
    {
      title: 'API Key (HMAC)',
      dataIndex: 'apiKey',
      key: 'apiKey',
      width: 240,
      render: (key) => (
        <Text copyable code style={{ fontSize: 12 }}>
          {key}
        </Text>
      ),
    },
    {
      title: 'Webhook URL Callback',
      dataIndex: 'webhookUrl',
      key: 'webhookUrl',
      width: 240,
      render: (url) => (
        url ? (
          <Text copyable ellipsis style={{ maxWidth: 220 }}>
            {url}
          </Text>
        ) : (
          <Text type="secondary">Chưa thiết lập</Text>
        )
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 140,
      render: (status) => <StatusTag status={status} />,
    },
    {
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      render: (date) => formatDate(date),
    },
  ]

  return (
    <PageContainer
      title="Quản lý đối tác Merchant"
      subTitle="Cấu hình tài khoản Merchant, cấp phát API Key / HMAC Secret và địa chỉ Webhook"
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
      {/* Search & Filter */}
      <AppFilter
        form={filterForm}
        onSearch={handleSearchSubmit}
        onReset={handleFilterReset}
      >
        <Col xs={24} sm={12} md={8}>
          <Form.Item name="keyword" label="Tìm kiếm nhanh" style={{ marginBottom: 0 }}>
            <Input placeholder="Mã Merchant, tên đối tác, webhook..." allowClear />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="status" label="Trạng thái" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả trạng thái"
              allowClear
              options={[
                { label: 'Tất cả', value: '' },
                { label: 'Đang hoạt động (ACTIVE)', value: 'ACTIVE' },
                { label: 'Tạm khóa (SUSPENDED)', value: 'SUSPENDED' },
              ]}
            />
          </Form.Item>
        </Col>
      </AppFilter>

      {/* Main Merchants Table */}
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
          render: (_, record) => (
            <ActionMenu
              items={[
                {
                  key: 'edit',
                  label: 'Chỉnh sửa',
                  icon: <EditOutlined />,
                  onClick: () => handleOpenEdit(record),
                },
                {
                  key: 'regenerate',
                  label: 'Cấp lại API Key & Secret',
                  icon: <KeyOutlined />,
                  onClick: () => regenerateMutation.mutate(record),
                  confirm: {
                    title: 'Xác nhận cấp lại API Key & HMAC Secret?',
                    description: `API Key và Secret hiện tại của "${record.name}" sẽ bị vô hiệu hóa ngay lập tức. Các hệ thống tích hợp cũ sẽ không thể gọi API cho đến khi cập nhật khóa mới.`,
                    okText: 'Cấp khóa mới',
                    cancelText: 'Hủy',
                  },
                },
                {
                  key: 'delete',
                  label: 'Xoá Merchant',
                  icon: <DeleteOutlined />,
                  danger: true,
                  onClick: () => deleteMutation.mutate(record.id),
                  confirm: {
                    title: `Xác nhận xóa đối tác "${record.name}"?`,
                    description: 'Hành động này không thể hoàn tác. Mọi kết nối API của merchant này sẽ bị chặn.',
                    okText: 'Xác nhận xóa',
                    cancelText: 'Hủy',
                  },
                },
              ]}
            />
          ),
        }}
      />

      {/* ── Modal 1: Create Merchant Form ──────────────────────────────────── */}
      <Modal
        title="Thêm mới đối tác Merchant"
        open={createModalVisible}
        onCancel={() => {
          setCreateModalVisible(false)
          createForm.resetFields()
        }}
        onOk={handleCreateSubmit}
        confirmLoading={createMutation.isPending}
        okText="Tạo Merchant"
        cancelText="Hủy"
        destroyOnClose
        width={560}
      >
        <Alert
          message="Khởi tạo thông tin bảo mật tự động"
          description="Hệ thống sẽ tự động sinh cặp API Key và HMAC Secret an toàn cho Merchant sau khi tạo thành công."
          type="info"
          showIcon
          style={{ marginTop: 12, marginBottom: 20 }}
        />

        <Form form={createForm} layout="vertical" preserve={false}>
          <Form.Item
            name="merchantId"
            label="Mã định danh Merchant (Merchant ID / Code)"
            rules={[
              { required: true, message: 'Vui lòng nhập mã Merchant' },
              {
                pattern: /^[a-zA-Z0-9_-]+$/,
                message: 'Chỉ chấp nhận chữ cái, số, dấu gạch nối (-) hoặc gạch dưới (_)',
              },
            ]}
          >
            <Input placeholder="Ví dụ: ecommerce-shop-alpha, lazada-vn..." />
          </Form.Item>

          <Form.Item
            name="name"
            label="Tên doanh nghiệp / Đối tác"
            rules={[{ required: true, message: 'Vui lòng nhập tên đối tác' }]}
          >
            <Input placeholder="Ví dụ: Cửa hàng Điện tử Alpha, Fashion Hub..." />
          </Form.Item>

          <Form.Item
            name="webhookUrl"
            label="Địa chỉ Webhook nhận callback (Tùy chọn)"
            rules={[
              {
                type: 'url',
                message: 'Vui lòng nhập đúng định dạng URL (http:// hoặc https://)',
              },
            ]}
          >
            <Input placeholder="https://api.partner.com/webhooks/payments" />
          </Form.Item>
        </Form>
      </Modal>

      {/* ── Modal 2: Edit Merchant Form ────────────────────────────────────── */}
      <Modal
        title={`Chỉnh sửa Merchant: ${editingMerchant?.name}`}
        open={editModalVisible}
        onCancel={() => {
          setEditModalVisible(false)
          setEditingMerchant(null)
          editForm.resetFields()
        }}
        onOk={handleEditSubmit}
        confirmLoading={updateMutation.isPending}
        okText="Lưu thay đổi"
        cancelText="Hủy"
        destroyOnClose
        width={540}
      >
        <Form form={editForm} layout="vertical" style={{ marginTop: 16 }}>
          <Form.Item
            name="name"
            label="Tên doanh nghiệp / Đối tác"
            rules={[{ required: true, message: 'Vui lòng nhập tên đối tác' }]}
          >
            <Input />
          </Form.Item>

          <Form.Item
            name="webhookUrl"
            label="Địa chỉ Webhook nhận callback"
            rules={[
              {
                type: 'url',
                message: 'Vui lòng nhập đúng định dạng URL (http:// hoặc https://)',
              },
            ]}
          >
            <Input placeholder="https://api.partner.com/webhooks/payments" />
          </Form.Item>

          <Form.Item
            name="isActive"
            label="Trạng thái kích hoạt"
            valuePropName="checked"
          >
            <Switch checkedChildren="HOẠT ĐỘNG" unCheckedChildren="TẠM KHÓA" />
          </Form.Item>
        </Form>
      </Modal>

      {/* ── Modal 3: Show Newly Generated Credentials (One-time Display) ───── */}
      <Modal
        title={
          <Space>
            <SafetyCertificateOutlined style={{ color: '#52c41a' }} />
            <span>{credentialsData?.title || 'Thông tin xác thực Merchant'}</span>
          </Space>
        }
        open={Boolean(credentialsData)}
        onCancel={() => setCredentialsData(null)}
        width={600}
        footer={[
          <Button
            key="copy"
            icon={<CopyOutlined />}
            onClick={() => {
              if (credentialsData) {
                handleCopyBothCredentials(credentialsData.apiKey, credentialsData.secret)
              }
            }}
          >
            Sao chép cả hai khóa
          </Button>,
          <Button
            key="ok"
            type="primary"
            onClick={() => setCredentialsData(null)}
          >
            Đã lưu khóa an toàn
          </Button>,
        ]}
      >
        {credentialsData && (
          <div style={{ marginTop: 12 }}>
            <Alert
              message="LƯU Ý BẢO MẬT QUAN TRỌNG"
              description="HMAC Secret chỉ hiển thị duy nhất một lần tại màn hình này. Hãy sao chép và lưu trữ an toàn. Bạn sẽ không thể xem lại secret sau khi đóng hộp thoại này."
              type="warning"
              showIcon
              style={{ marginBottom: 16 }}
            />

            <Paragraph>
              Đối tác: <strong>{credentialsData.merchantName}</strong>
              {credentialsData.merchantCode && (
                <span> (Mã: <code>{credentialsData.merchantCode}</code>)</span>
              )}
            </Paragraph>

            <div style={{ background: '#f5f5f5', padding: 16, borderRadius: 8 }}>
              <div style={{ marginBottom: 12 }}>
                <Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
                  API KEY:
                </Text>
                <Text copyable strong code style={{ fontSize: 13 }}>
                  {credentialsData.apiKey}
                </Text>
              </div>

              <div>
                <Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 4 }}>
                  HMAC SECRET KEY:
                </Text>
                <Text copyable strong code style={{ fontSize: 13, color: '#cf1322' }}>
                  {credentialsData.secret}
                </Text>
              </div>
            </div>
          </div>
        )}
      </Modal>
    </PageContainer>
  )
}
