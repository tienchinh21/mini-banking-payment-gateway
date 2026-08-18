import React, { useState } from 'react'
import { Form, Input, Select, Button, Modal, InputNumber, message, Typography, Col, Tag } from 'antd'
import { RollbackOutlined, EyeOutlined, DownloadOutlined } from '@ant-design/icons'
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query'
import {
  PageContainer,
  AppTable,
  AppFilter,
  MoneyDisplay,
  StatusTag,
  ActionMenu,
  type AppTableColumns,
} from '@/components/core'
import { useTable } from '@/hooks/useTable'
import { formatDate } from '@/utils/format'
import { paymentService } from '../services/paymentService'
import type { PaymentItem, RefundFormData } from '../types'

const { Text } = Typography

export const PaymentListPage: React.FC = () => {
  const queryClient = useQueryClient()
  const [filterForm] = Form.useForm()
  const [refundModalVisible, setRefundModalVisible] = useState(false)
  const [selectedPayment, setSelectedPayment] = useState<PaymentItem | null>(null)
  const [refundForm] = Form.useForm()

  const {
    queryParams,
    pagination,
    setTotal,
    setKeyword,
    setFilters,
    handleTableChange,
    handleReset,
  } = useTable<PaymentItem>({
    defaultPageSize: 10,
  })

  // Query payments
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['payments-list', queryParams],
    queryFn: async () => {
      const res = await paymentService.getPayments(queryParams)
      setTotal(res.meta.totalItems)
      return res
    },
  })

  // Refund mutation
  const refundMutation = useMutation({
    mutationFn: (values: RefundFormData) => paymentService.refund(values),
    onSuccess: () => {
      message.success('Tạo yêu cầu hoàn tiền thành công!')
      setRefundModalVisible(false)
      refundForm.resetFields()
      queryClient.invalidateQueries({ queryKey: ['payments-list'] })
    },
  })

  const handleOpenRefund = (record: PaymentItem) => {
    setSelectedPayment(record)
    refundForm.setFieldsValue({
      paymentId: record.id,
      amount: record.amount,
      reason: 'Hoàn tiền theo yêu cầu khách hàng',
    })
    setRefundModalVisible(true)
  }

  const columns: AppTableColumns<PaymentItem> = [
    {
      title: 'Mã giao dịch',
      dataIndex: 'id',
      key: 'id',
      width: 170,
      render: (text) => <Text copyable strong>{text}</Text>,
    },
    {
      title: 'Mã đơn hàng',
      dataIndex: 'orderId',
      key: 'orderId',
      width: 150,
      render: (text) => <Tag color="blue">{text}</Tag>,
    },
    {
      title: 'Merchant',
      dataIndex: 'merchantName',
      key: 'merchantName',
      width: 180,
    },
    {
      title: 'Khách hàng',
      dataIndex: 'payerName',
      key: 'payerName',
      width: 160,
    },
    {
      title: 'Ví thanh toán',
      dataIndex: 'payerWalletNumber',
      key: 'payerWalletNumber',
      width: 160,
    },
    {
      title: 'Số tiền',
      dataIndex: 'amount',
      key: 'amount',
      width: 160,
      align: 'right',
      render: (val, record) => (
        <MoneyDisplay amount={val} currency={record.currency} bold />
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
      title: 'Idempotency Key',
      dataIndex: 'idempotencyKey',
      key: 'idempotencyKey',
      width: 200,
      ellipsis: true,
      render: (key) => <Text code>{key}</Text>,
    },
    {
      title: 'Thời gian tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date) => formatDate(date),
    },
  ]

  const handleSearchSubmit = (values: any) => {
    setKeyword(values.keyword || '')
    setFilters({
      status: values.status,
      merchantId: values.merchantId,
    })
  }

  const handleFilterReset = () => {
    filterForm.resetFields()
    handleReset()
  }

  return (
    <PageContainer
      title="Lịch sử Thanh toán & Giao dịch"
      subTitle="Theo dõi các giao dịch thanh toán Direct Debit qua Merchant Payment API"
      extra={
        <Button icon={<DownloadOutlined />}>
          Xuất dữ liệu (Excel)
        </Button>
      }
    >
      {/* Filter Toolbar */}
      <AppFilter
        form={filterForm}
        onSearch={handleSearchSubmit}
        onReset={handleFilterReset}
      >
        <Col xs={24} sm={12} md={8}>
          <Form.Item name="keyword" label="Tìm kiếm" style={{ marginBottom: 0 }}>
            <Input placeholder="Mã GD, Mã đơn, tên KH, số ví..." allowClear />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="status" label="Trạng thái" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả trạng thái"
              allowClear
              options={[
                { label: 'Thành công', value: 'SUCCEEDED' },
                { label: 'Chờ xử lý', value: 'PENDING' },
                { label: 'Thất bại', value: 'FAILED' },
                { label: 'Đã hoàn tiền', value: 'REFUNDED' },
              ]}
            />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="merchantId" label="Merchant" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả Merchant"
              allowClear
              options={[
                { label: 'E-commerce Shop Alpha', value: 'MCH-01' },
                { label: 'Tech Store Beta', value: 'MCH-02' },
                { label: 'Fashion Hub', value: 'MCH-03' },
              ]}
            />
          </Form.Item>
        </Col>
      </AppFilter>

      {/* AppTable with sticky right action column */}
      <AppTable<PaymentItem>
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
                  onClick: () => message.info(`Xem chi tiết đơn hàng: ${record.orderId}`),
                },
                {
                  key: 'refund',
                  label: 'Hoàn tiền',
                  icon: <RollbackOutlined />,
                  disabled: record.status !== 'SUCCEEDED',
                  onClick: () => handleOpenRefund(record),
                },
              ]}
            />
          ),
        }}
      />

      {/* Refund Modal */}
      <Modal
        title={`Hoàn tiền cho giao dịch ${selectedPayment?.id}`}
        open={refundModalVisible}
        onCancel={() => setRefundModalVisible(false)}
        onOk={() => refundForm.submit()}
        confirmLoading={refundMutation.isPending}
        destroyOnClose
      >
        <Form
          form={refundForm}
          layout="vertical"
          onFinish={(values) => refundMutation.mutate(values)}
        >
          <Form.Item name="paymentId" hidden>
            <Input />
          </Form.Item>

          <Form.Item
            name="amount"
            label="Số tiền hoàn lại (VND)"
            rules={[
              { required: true, message: 'Vui lòng nhập số tiền hoàn' },
              {
                validator: (_, value) => {
                  if (value > (selectedPayment?.amount || 0)) {
                    return Promise.reject(new Error('Số tiền hoàn không được vượt quá giá trị GD'))
                  }
                  return Promise.resolve()
                },
              },
            ]}
          >
            <InputNumber
              style={{ width: '100%' }}
              formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
              parser={(value) => (value ? Number(value.replace(/\$\s?|(,*)/g, '')) : 0)}
              max={selectedPayment?.amount}
            />
          </Form.Item>

          <Form.Item
            name="reason"
            label="Lý do hoàn tiền"
            rules={[{ required: true, message: 'Vui lòng nhập lý do hoàn tiền' }]}
          >
            <Input.TextArea rows={3} placeholder="Nhập lý do hoàn trả cho khách hàng" />
          </Form.Item>
        </Form>
      </Modal>
    </PageContainer>
  )
}
