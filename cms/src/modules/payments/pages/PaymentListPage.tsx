import React, { useState } from 'react'
import {
  Form,
  Input,
  Select,
  Button,
  Modal,
  Drawer,
  InputNumber,
  message,
  Typography,
  Col,
  Row,
  Card,
  Space,
  Tag,
  Descriptions,
  Alert,
  Spin,
  Empty,
  Table,
} from 'antd'
import {
  RollbackOutlined,
  EyeOutlined,
  ReloadOutlined,
  ExclamationCircleOutlined,
  AuditOutlined,
  CloseCircleOutlined,
  InfoCircleOutlined,
} from '@ant-design/icons'
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
import { merchantService } from '@/modules/merchants/services/merchantService'
import type { PaymentItem, PaymentLedgerEntry, RefundFormData } from '../types'

const { Text } = Typography

export const PaymentListPage: React.FC = () => {
  const queryClient = useQueryClient()
  const [filterForm] = Form.useForm()
  const [refundForm] = Form.useForm<RefundFormData>()

  // Detail Drawer state
  const [drawerVisible, setDrawerVisible] = useState(false)
  const [selectedPaymentId, setSelectedPaymentId] = useState<string | null>(null)

  // Refund Modal state
  const [refundModalVisible, setRefundModalVisible] = useState(false)
  const [selectedPaymentForRefund, setSelectedPaymentForRefund] = useState<PaymentItem | null>(null)

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

  // ── 1. Fetch Payments List ───────────────────────────────────────────────────
  const {
    data: paymentsData,
    isLoading: isLoadingPayments,
    isError: isPaymentsError,
    error: paymentsError,
    refetch: refetchPayments,
  } = useQuery({
    queryKey: ['payments-list', queryParams],
    queryFn: async () => {
      const res = await paymentService.getPayments(queryParams)
      setTotal(res?.meta?.totalItems ?? res?.items?.length ?? 0)
      return res
    },
  })

  // ── 2. Fetch Merchants for Filter Dropdown ──────────────────────────────────
  const { data: merchantsData } = useQuery({
    queryKey: ['filter-merchants-list'],
    queryFn: async () => {
      const res = await merchantService.getMerchants({ pageSize: 100 })
      return res?.items ?? []
    },
  })

  // ── 3. Fetch Single Payment Detail for Drawer ───────────────────────────────
  const {
    data: paymentDetail,
    isLoading: isLoadingDetail,
    isError: isDetailError,
    refetch: refetchDetail,
  } = useQuery({
    queryKey: ['payment-detail', selectedPaymentId],
    queryFn: () => {
      if (!selectedPaymentId) return null
      return paymentService.getPaymentById(selectedPaymentId)
    },
    enabled: Boolean(drawerVisible && selectedPaymentId),
  })

  // ── 4. Refund Mutation ─────────────────────────────────────────────────────
  const refundMutation = useMutation({
    mutationFn: (values: RefundFormData) => paymentService.refund(values),
    onSuccess: (result) => {
      message.success(`Hoàn tiền thành công cho giao dịch! Mã hoàn tiền: ${result.refundId || 'Đã ghi nhận'}`)
      setRefundModalVisible(false)
      refundForm.resetFields()
      setSelectedPaymentForRefund(null)
      queryClient.invalidateQueries({ queryKey: ['payments-list'] })
      if (selectedPaymentId) {
        queryClient.invalidateQueries({ queryKey: ['payment-detail', selectedPaymentId] })
      }
    },
  })

  // Open Drawer Detail
  const handleOpenDetail = (record: PaymentItem) => {
    setSelectedPaymentId(record.id)
    setDrawerVisible(true)
  }

  // Open Refund Modal
  const handleOpenRefund = (record: PaymentItem) => {
    setSelectedPaymentForRefund(record)
    refundForm.setFieldsValue({
      paymentId: record.id,
      amount: record.amount,
      reason: 'Hoàn tiền theo yêu cầu khách hàng',
    })
    setRefundModalVisible(true)
  }

  const handleQuickSetAmount = (percentage: number) => {
    if (!selectedPaymentForRefund) return
    const calculated = Math.round((selectedPaymentForRefund.amount * percentage) / 100)
    refundForm.setFieldValue('amount', calculated)
  }

  // Search submit
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

  // Columns for main Payment Table
  const columns: AppTableColumns<PaymentItem> = [
    {
      title: 'Mã giao dịch',
      dataIndex: 'id',
      key: 'id',
      width: 220,
      render: (text) => (
        <Text copyable strong style={{ fontFamily: 'monospace', fontSize: 13 }}>
          {text}
        </Text>
      ),
    },
    {
      title: 'Mã đơn hàng',
      dataIndex: 'orderId',
      key: 'orderId',
      width: 150,
      render: (text) => <Tag color="blue">{text || '-'}</Tag>,
    },
    {
      title: 'Merchant',
      dataIndex: 'merchantName',
      key: 'merchantName',
      width: 180,
      render: (name, record) => (
        <div>
          <div style={{ fontWeight: 500 }}>{name}</div>
          <Text type="secondary" style={{ fontSize: 12 }}>
            {record.merchantId}
          </Text>
        </div>
      ),
    },
    {
      title: 'Người thanh toán',
      dataIndex: 'payerName',
      key: 'payerName',
      width: 180,
      render: (name, record) => (
        <div>
          <div style={{ fontWeight: 500 }}>{name || 'Khách hàng'}</div>
          <Text type="secondary" style={{ fontSize: 12, fontFamily: 'monospace' }}>
            {record.payerWalletNumber}
          </Text>
        </div>
      ),
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
      title: 'Thời gian tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date) => formatDate(date),
    },
  ]

  // Columns for Ledger Entries table in Detail Drawer
  const ledgerColumns = [
    {
      title: 'Mã bút toán',
      dataIndex: 'id',
      key: 'id',
      width: 180,
      render: (id: string) => (
        <Text copyable style={{ fontFamily: 'monospace', fontSize: 12 }}>
          {id}
        </Text>
      ),
    },
    {
      title: 'Tài khoản',
      dataIndex: 'accountType',
      key: 'accountType',
      render: (type: string, record: PaymentLedgerEntry) => {
        let label = type
        if (type === 'WalletAccount') label = 'Ví người dùng'
        else if (type === 'PlatformClearing') label = 'Tài khoản Platform Clearing'
        else if (type === 'MerchantSettlement') label = 'Tài khoản Merchant Settlement'

        return (
          <div>
            <Tag color="cyan">{label}</Tag>
            <div style={{ fontSize: 11, color: '#8c8c8c', fontFamily: 'monospace', marginTop: 2 }}>
              ID: {record.accountId}
            </div>
          </div>
        )
      },
    },
    {
      title: 'Ghi sổ',
      dataIndex: 'isDebit',
      key: 'isDebit',
      width: 110,
      align: 'center' as const,
      render: (isDebit: boolean) =>
        isDebit ? (
          <Tag color="error" style={{ fontWeight: 600 }}>
            DEBIT (Nợ)
          </Tag>
        ) : (
          <Tag color="success" style={{ fontWeight: 600 }}>
            CREDIT (Có)
          </Tag>
        ),
    },
    {
      title: 'Số tiền',
      dataIndex: 'amount',
      key: 'amount',
      width: 140,
      align: 'right' as const,
      render: (amount: number, record: PaymentLedgerEntry) => (
        <MoneyDisplay amount={amount} currency={record.currency || 'VND'} bold />
      ),
    },
    {
      title: 'Thời gian',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 150,
      render: (date: string) => formatDate(date),
    },
  ]

  return (
    <PageContainer
      title="Quản lý Thanh toán & Hoàn tiền"
      subTitle="Theo dõi các giao dịch thanh toán Direct Debit và thực hiện hoàn tiền đối soát qua Merchant Payment API"
      extra={
        <Button
          icon={<ReloadOutlined />}
          onClick={() => {
            refetchPayments()
            message.info('Đang làm mới danh sách thanh toán...')
          }}
          loading={isLoadingPayments}
        >
          Làm mới
        </Button>
      }
    >
      {/* Filter Toolbar */}
      <AppFilter
        form={filterForm}
        onSearch={handleSearchSubmit}
        onReset={handleFilterReset}
      >
        <Col xs={24} sm={12} md={10}>
          <Form.Item name="keyword" label="Tìm kiếm" style={{ marginBottom: 0 }}>
            <Input
              placeholder="Mã GD, Mã đơn hàng, tên khách hàng, số ví, merchant..."
              allowClear
            />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={7}>
          <Form.Item name="status" label="Trạng thái" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả trạng thái"
              allowClear
              options={[
                { label: 'Thành công (SUCCEEDED)', value: 'SUCCEEDED' },
                { label: 'Chờ xử lý (PENDING)', value: 'PENDING' },
                { label: 'Thất bại (FAILED)', value: 'FAILED' },
              ]}
            />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={7}>
          <Form.Item name="merchantId" label="Merchant" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả Merchant"
              allowClear
              showSearch
              filterOption={(input, option) =>
                (option?.label ?? '').toLowerCase().includes(input.toLowerCase())
              }
              options={
                merchantsData?.map((m) => ({
                  label: `${m.name} (${m.code})`,
                  value: m.code,
                })) || []
              }
            />
          </Form.Item>
        </Col>
      </AppFilter>

      {/* Error Alert if Payments query failed */}
      {isPaymentsError && (
        <Alert
          type="error"
          showIcon
          message="Lỗi khi tải danh sách thanh toán"
          description={
            paymentsError instanceof Error
              ? paymentsError.message
              : 'Không thể kết nối đến máy chủ. Vui lòng kiểm tra lại dịch vụ Backend.'
          }
          style={{ marginBottom: 16 }}
          action={
            <Button size="small" danger onClick={() => refetchPayments()}>
              Thử lại
            </Button>
          }
        />
      )}

      {/* AppTable with pagination and sticky right action column */}
      <AppTable<PaymentItem>
        rowKey="id"
        columns={columns}
        dataSource={paymentsData?.items || []}
        loading={isLoadingPayments}
        pagination={pagination}
        onChange={handleTableChange}
        onRefresh={() => refetchPayments()}
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

      {/* ── Detail Drawer ──────────────────────────────────────────────────────── */}
      <Drawer
        title={
          <Space align="center">
            <AuditOutlined style={{ color: '#1677ff' }} />
            <span>Chi tiết giao dịch thanh toán</span>
            {paymentDetail?.status && <StatusTag status={paymentDetail.status} />}
          </Space>
        }
        placement="right"
        width={720}
        open={drawerVisible}
        onClose={() => {
          setDrawerVisible(false)
          setSelectedPaymentId(null)
        }}
        extra={
          <Space>
            <Button
              icon={<ReloadOutlined />}
              size="small"
              onClick={() => refetchDetail()}
              loading={isLoadingDetail}
            >
              Làm mới
            </Button>
            {paymentDetail?.status === 'SUCCEEDED' && (
              <Button
                type="primary"
                danger
                icon={<RollbackOutlined />}
                size="small"
                onClick={() => {
                  if (paymentDetail) {
                    handleOpenRefund(paymentDetail)
                  }
                }}
              >
                Hoàn tiền
              </Button>
            )}
          </Space>
        }
      >
        {isLoadingDetail && (
          <div style={{ textAlign: 'center', padding: '60px 0' }}>
            <Spin tip="Đang tải thông tin chi tiết giao dịch..." size="large" />
          </div>
        )}

        {isDetailError && (
          <Alert
            type="error"
            showIcon
            message="Không thể tải thông tin chi tiết"
            description="Đã xảy ra lỗi khi lấy dữ liệu giao dịch từ hệ thống."
            action={
              <Button size="small" onClick={() => refetchDetail()}>
                Thử lại
              </Button>
            }
          />
        )}

        {paymentDetail && (
          <div>
            {/* Failure Alert Banner */}
            {paymentDetail.status === 'FAILED' && (
              <Alert
                type="error"
                showIcon
                icon={<CloseCircleOutlined />}
                message={
                  <Text strong style={{ color: '#cf1322' }}>
                    Giao dịch thanh toán thất bại
                  </Text>
                }
                description={
                  <div>
                    <div>
                      <strong>Mã lỗi (Failure Code): </strong>
                      <Tag color="error">{paymentDetail.failureCode || 'SYSTEM_ERROR'}</Tag>
                    </div>
                    {paymentDetail.description && (
                      <div style={{ marginTop: 4 }}>
                        <strong>Chi tiết lỗi: </strong>
                        {paymentDetail.description}
                      </div>
                    )}
                  </div>
                }
                style={{ marginBottom: 20 }}
              />
            )}

            {/* General Info Card */}
            <Card
              title="Thông tin thanh toán"
              size="small"
              bordered={false}
              style={{ background: '#fafafa', marginBottom: 20 }}
            >
              <Descriptions size="small" column={{ xs: 1, sm: 2 }} bordered>
                <Descriptions.Item label="Mã giao dịch">
                  <Text copyable strong style={{ fontFamily: 'monospace' }}>
                    {paymentDetail.id}
                  </Text>
                </Descriptions.Item>

                <Descriptions.Item label="Mã đơn hàng">
                  <Tag color="blue">{paymentDetail.orderId || '-'}</Tag>
                </Descriptions.Item>

                <Descriptions.Item label="Số tiền thanh toán">
                  <MoneyDisplay
                    amount={paymentDetail.amount}
                    currency={paymentDetail.currency}
                    bold
                    style={{ fontSize: 16 }}
                  />
                </Descriptions.Item>

                <Descriptions.Item label="Trạng thái">
                  <StatusTag status={paymentDetail.status} />
                </Descriptions.Item>

                <Descriptions.Item label="Đối tác Merchant">
                  <div>
                    <strong>{paymentDetail.merchantName}</strong>
                    <div style={{ fontSize: 12, color: '#8c8c8c' }}>
                      Mã: {paymentDetail.merchantId}
                    </div>
                  </div>
                </Descriptions.Item>

                <Descriptions.Item label="Người thanh toán">
                  <div>
                    <strong>{paymentDetail.payerName || 'Khách hàng'}</strong>
                    <div style={{ fontSize: 12, color: '#8c8c8c', fontFamily: 'monospace' }}>
                      Số ví: {paymentDetail.payerWalletNumber}
                    </div>
                  </div>
                </Descriptions.Item>

                <Descriptions.Item label="Thời gian tạo">
                  {formatDate(paymentDetail.createdAt)}
                </Descriptions.Item>

                <Descriptions.Item label="Idempotency Key">
                  <Text copyable code style={{ fontSize: 11 }}>
                    {paymentDetail.idempotencyKey}
                  </Text>
                </Descriptions.Item>

                {paymentDetail.callbackUrl && (
                  <Descriptions.Item label="Callback URL" span={2}>
                    <Text copyable ellipsis style={{ maxWidth: 450 }}>
                      {paymentDetail.callbackUrl}
                    </Text>
                  </Descriptions.Item>
                )}

                {paymentDetail.description && (
                  <Descriptions.Item label="Nội dung mô tả" span={2}>
                    {paymentDetail.description}
                  </Descriptions.Item>
                )}

                {paymentDetail.ledgerTransactionId && (
                  <Descriptions.Item label="Mã GD sổ cái (Ledger Tx ID)" span={2}>
                    <Text copyable code>
                      {paymentDetail.ledgerTransactionId}
                    </Text>
                  </Descriptions.Item>
                )}
              </Descriptions>
            </Card>

            {/* Double-entry Ledger Entries Section */}
            <Card
              title={
                <Space>
                  <AuditOutlined />
                  <span>Bút toán sổ cái kép (Double-Entry Ledger Entries)</span>
                </Space>
              }
              size="small"
              bordered={false}
              style={{ background: '#fafafa' }}
            >
              <Alert
                type="info"
                showIcon
                icon={<InfoCircleOutlined />}
                message="Kiến trúc Double-Entry Ledger"
                description="Mỗi giao dịch thanh toán hạch toán đồng thời bút toán Nợ (Debit) và bút toán Có (Credit) đảm bảo cân bằng bất biến."
                style={{ marginBottom: 12 }}
              />

              {paymentDetail.ledgerEntries && paymentDetail.ledgerEntries.length > 0 ? (
                <Table<PaymentLedgerEntry>
                  rowKey="id"
                  size="small"
                  columns={ledgerColumns}
                  dataSource={paymentDetail.ledgerEntries}
                  pagination={false}
                  bordered
                />
              ) : (
                <Empty
                  image={Empty.PRESENTED_IMAGE_SIMPLE}
                  description={
                    paymentDetail.status === 'FAILED'
                      ? 'Giao dịch thất bại nên không phát sinh bút toán sổ cái.'
                      : 'Chưa có bút toán sổ cái nào được liên kết.'
                  }
                />
              )}
            </Card>
          </div>
        )}
      </Drawer>

      {/* ── Refund Modal ───────────────────────────────────────────────────────── */}
      <Modal
        title={
          <Space>
            <ExclamationCircleOutlined style={{ color: '#faad14' }} />
            <span>Xác nhận hoàn tiền cho giao dịch</span>
          </Space>
        }
        open={refundModalVisible}
        onCancel={() => {
          setRefundModalVisible(false)
          refundForm.resetFields()
          setSelectedPaymentForRefund(null)
        }}
        onOk={() => refundForm.submit()}
        confirmLoading={refundMutation.isPending}
        okText="Xác nhận hoàn tiền"
        cancelText="Hủy"
        okButtonProps={{ danger: true }}
        destroyOnClose
        width={560}
      >
        {selectedPaymentForRefund && (
          <div style={{ marginTop: 12 }}>
            <Alert
              type="warning"
              showIcon
              message="Lưu ý khi hoàn tiền"
              description="Hệ thống sẽ ghi nhận bút toán hoàn tiền vào sổ cái và cộng lại số dư cho tài khoản ví của khách hàng."
              style={{ marginBottom: 16 }}
            />

            <Card size="small" style={{ background: '#f5f5f5', marginBottom: 16 }}>
              <Row gutter={[12, 8]}>
                <Col span={12}>
                  <Text type="secondary">Mã giao dịch:</Text>
                  <div>
                    <Text copyable strong style={{ fontFamily: 'monospace', fontSize: 12 }}>
                      {selectedPaymentForRefund.id}
                    </Text>
                  </div>
                </Col>
                <Col span={12}>
                  <Text type="secondary">Mã đơn hàng:</Text>
                  <div>
                    <Tag color="blue">{selectedPaymentForRefund.orderId || '-'}</Tag>
                  </div>
                </Col>
                <Col span={12}>
                  <Text type="secondary">Khách hàng:</Text>
                  <div style={{ fontWeight: 500 }}>
                    {selectedPaymentForRefund.payerName} ({selectedPaymentForRefund.payerWalletNumber})
                  </div>
                </Col>
                <Col span={12}>
                  <Text type="secondary">Số tiền gốc:</Text>
                  <div>
                    <MoneyDisplay
                      amount={selectedPaymentForRefund.amount}
                      currency={selectedPaymentForRefund.currency}
                      bold
                    />
                  </div>
                </Col>
              </Row>
            </Card>

            <Form
              form={refundForm}
              layout="vertical"
              onFinish={(values) => {
                Modal.confirm({
                  title: 'Xác nhận thực hiện hoàn tiền?',
                  content: (
                    <div>
                      <p>
                        Bạn đang yêu cầu hoàn số tiền{' '}
                        <strong>
                          <MoneyDisplay
                            amount={values.amount}
                            currency={selectedPaymentForRefund.currency}
                            bold
                          />
                        </strong>{' '}
                        cho khách hàng <strong>{selectedPaymentForRefund.payerName}</strong>.
                      </p>
                      <p style={{ color: '#8c8c8c', fontSize: 12 }}>
                        Lý do: {values.reason}
                      </p>
                    </div>
                  ),
                  okText: 'Đồng ý hoàn tiền',
                  cancelText: 'Xem lại',
                  okButtonProps: { danger: true },
                  onOk: () => refundMutation.mutate(values),
                })
              }}
            >
              <Form.Item name="paymentId" hidden>
                <Input />
              </Form.Item>

              <Form.Item
                name="amount"
                label={
                  <Space style={{ width: '100%', justifyContent: 'space-between' }}>
                    <span>Số tiền hoàn trả (VND)</span>
                    <Space size={4}>
                      <Button
                        size="small"
                        type="dashed"
                        onClick={() => handleQuickSetAmount(100)}
                      >
                        100%
                      </Button>
                      <Button
                        size="small"
                        type="dashed"
                        onClick={() => handleQuickSetAmount(50)}
                      >
                        50%
                      </Button>
                    </Space>
                  </Space>
                }
                rules={[
                  { required: true, message: 'Vui lòng nhập số tiền hoàn trả' },
                  {
                    validator: (_, value) => {
                      if (value <= 0) {
                        return Promise.reject(new Error('Số tiền hoàn phải lớn hơn 0 đ'))
                      }
                      if (value > (selectedPaymentForRefund?.amount || 0)) {
                        return Promise.reject(
                          new Error('Số tiền hoàn không được vượt quá số tiền thanh toán gốc')
                        )
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
                  min={1}
                  max={selectedPaymentForRefund.amount}
                  placeholder="Nhập số tiền hoàn"
                  addonAfter="VND"
                />
              </Form.Item>

              <Form.Item
                name="reason"
                label="Lý do hoàn tiền"
                rules={[
                  { required: true, message: 'Vui lòng nhập lý do hoàn tiền' },
                  { min: 5, message: 'Lý do hoàn tiền cần ít nhất 5 ký tự' },
                ]}
              >
                <Input.TextArea
                  rows={3}
                  placeholder="Ví dụ: Khách hàng hủy đơn, hoàn tiền theo yêu cầu đối soát..."
                  showCount
                  maxLength={255}
                />
              </Form.Item>
            </Form>
          </div>
        )}
      </Modal>
    </PageContainer>
  )
}
