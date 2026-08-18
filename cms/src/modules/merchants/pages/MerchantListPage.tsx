import React, { useState } from 'react'
import { Form, Input, Button, Modal, message, Typography, Col } from 'antd'
import { PlusOutlined, KeyOutlined, EyeOutlined } from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
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

const { Text } = Typography

interface MerchantItem {
  id: string
  code: string
  name: string
  contactEmail: string
  apiKey: string
  status: 'ACTIVE' | 'SUSPENDED'
  webhookUrl: string
  createdAt: string
}

const mockMerchants: MerchantItem[] = [
  {
    id: 'mch-01',
    code: 'MCH-ECOM-ALPHA',
    name: 'E-commerce Shop Alpha',
    contactEmail: 'tech@ecomalpha.com',
    apiKey: 'mch_live_key_998127391823791',
    status: 'ACTIVE',
    webhookUrl: 'https://alpha.example.com/api/webhooks/minibanking',
    createdAt: '2026-08-01T00:00:00Z',
  },
  {
    id: 'mch-02',
    code: 'MCH-TECH-BETA',
    name: 'Tech Store Beta',
    contactEmail: 'admin@techbeta.vn',
    apiKey: 'mch_live_key_445129381726354',
    status: 'ACTIVE',
    webhookUrl: 'https://beta.example.com/webhooks/payments',
    createdAt: '2026-08-05T00:00:00Z',
  },
  {
    id: 'mch-03',
    code: 'MCH-FASHION-HUB',
    name: 'Fashion Hub',
    contactEmail: 'support@fashionhub.com',
    apiKey: 'mch_live_key_771829384756123',
    status: 'SUSPENDED',
    webhookUrl: 'https://fashionhub.com/api/payment-callback',
    createdAt: '2026-08-10T00:00:00Z',
  },
]

export const MerchantListPage: React.FC = () => {
  const [filterForm] = Form.useForm()
  const [createModalVisible, setCreateModalVisible] = useState(false)
  const [createForm] = Form.useForm()

  const {
    queryParams,
    pagination,
    setTotal,
    setKeyword,
    handleTableChange,
    handleReset,
  } = useTable<MerchantItem>()

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['merchants-list', queryParams],
    queryFn: async () => {
      let list = [...mockMerchants]
      if (queryParams.keyword) {
        const kw = String(queryParams.keyword).toLowerCase()
        list = list.filter(
          (m) =>
            m.name.toLowerCase().includes(kw) ||
            m.code.toLowerCase().includes(kw) ||
            m.contactEmail.toLowerCase().includes(kw)
        )
      }
      setTotal(list.length)
      return { items: list, total: list.length }
    },
  })

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
    },
    {
      title: 'Email liên hệ',
      dataIndex: 'contactEmail',
      key: 'contactEmail',
      width: 180,
    },
    {
      title: 'API Key (HMAC)',
      dataIndex: 'apiKey',
      key: 'apiKey',
      width: 220,
      render: (key) => <Text copyable code>{key}</Text>,
    },
    {
      title: 'Webhook URL Callback',
      dataIndex: 'webhookUrl',
      key: 'webhookUrl',
      width: 260,
      render: (url) => <Text ellipsis>{url}</Text>,
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
      width: 160,
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
                  onClick: () => message.info(`Xem cấu hình ${record.name}`),
                },
                {
                  key: 'regen',
                  label: 'Cấp lại API Key',
                  icon: <KeyOutlined />,
                  confirm: {
                    title: 'Xác nhận cấp lại API Key?',
                    description: 'Hành động này sẽ vô hiệu hóa API Key cũ ngay lập tức.',
                  },
                  onClick: () => message.success(`Đã cấp lại API Key mới cho ${record.code}`),
                },
              ]}
            />
          ),
        }}
      />

      <Modal
        title="Thêm Đối tác Merchant"
        open={createModalVisible}
        onCancel={() => setCreateModalVisible(false)}
        onOk={() => {
          createForm.validateFields().then(() => {
            message.success('Tạo đối tác Merchant thành công!')
            setCreateModalVisible(false)
            createForm.resetFields()
          })
        }}
      >
        <Form form={createForm} layout="vertical">
          <Form.Item
            name="name"
            label="Tên thương mại Merchant"
            rules={[{ required: true, message: 'Vui lòng nhập tên đối tác' }]}
          >
            <Input placeholder="Ví dụ: Shopee Mall, Tiki Store..." />
          </Form.Item>

          <Form.Item
            name="contactEmail"
            label="Email nhận thông báo kỹ thuật"
            rules={[
              { required: true, message: 'Vui lòng nhập email' },
              { type: 'email', message: 'Email không hợp lệ' },
            ]}
          >
            <Input placeholder="tech@merchant.com" />
          </Form.Item>

          <Form.Item
            name="webhookUrl"
            label="Webhook URL Endpoint"
            rules={[{ required: true, message: 'Vui lòng nhập webhook endpoint' }]}
          >
            <Input placeholder="https://merchant.com/api/payment-callback" />
          </Form.Item>
        </Form>
      </Modal>
    </PageContainer>
  )
}
