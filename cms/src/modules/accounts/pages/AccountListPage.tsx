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
  Divider,
  Alert,
  Spin,
  Empty,
  Tooltip,
} from 'antd'
import {
  PlusOutlined,
  LockOutlined,
  UnlockOutlined,
  EyeOutlined,
  WalletOutlined,
  DollarOutlined,
  ReloadOutlined,
  HistoryOutlined,
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
import { accountService } from '../services/accountService'
import type {
  WalletAccountItem,
  TopUpFormData,
  WalletLedgerEntry,
} from '../types'

const { Text, Paragraph } = Typography

// Preset top-up amount buttons for faster user entry
const QUICK_AMOUNT_PRESETS = [
  { label: '100,000 đ', value: 100000 },
  { label: '500,000 đ', value: 500000 },
  { label: '1,000,000 đ', value: 1000000 },
  { label: '2,000,000 đ', value: 2000000 },
  { label: '5,000,000 đ', value: 5000000 },
]

export const AccountListPage: React.FC = () => {
  const queryClient = useQueryClient()
  const [filterForm] = Form.useForm()
  const [topUpForm] = Form.useForm()

  // Top-Up Modal State
  const [topUpModalVisible, setTopUpModalVisible] = useState(false)
  const [selectedWalletForTopUp, setSelectedWalletForTopUp] = useState<WalletAccountItem | null>(null)

  // Drawer Detail & Ledger State
  const [drawerVisible, setDrawerVisible] = useState(false)
  const [selectedWalletForDetail, setSelectedWalletForDetail] = useState<WalletAccountItem | null>(null)

  const {
    queryParams,
    pagination,
    setTotal,
    setKeyword,
    setFilters,
    handleTableChange,
    handleReset,
  } = useTable<WalletAccountItem>({
    defaultPageSize: 10,
  })

  // 1. Fetch Wallets Query
  const {
    data: accountsData,
    isLoading: isLoadingAccounts,
    isError: isAccountsError,
    refetch: refetchAccounts,
  } = useQuery({
    queryKey: ['accounts-list', queryParams],
    queryFn: async () => {
      const res = await accountService.getAccounts(queryParams)
      setTotal(res?.meta?.totalItems ?? 0)
      return res
    },
  })

  // 2. Fetch Balance of Selected Wallet for Drawer
  const {
    data: liveBalance,
    isLoading: isLoadingBalance,
    refetch: refetchBalance,
  } = useQuery({
    queryKey: ['wallet-balance', selectedWalletForDetail?.accountNumber],
    queryFn: () => {
      if (!selectedWalletForDetail?.accountNumber) return null
      return accountService.getBalance(selectedWalletForDetail.accountNumber)
    },
    enabled: Boolean(drawerVisible && selectedWalletForDetail?.accountNumber),
  })

  // 3. Fetch Ledger Entries of Selected Wallet for Drawer
  const {
    data: ledgerEntries,
    isLoading: isLoadingLedger,
    refetch: refetchLedger,
  } = useQuery({
    queryKey: ['wallet-ledger', selectedWalletForDetail?.accountNumber],
    queryFn: () => {
      if (!selectedWalletForDetail?.accountNumber) return []
      return accountService.getLedger(selectedWalletForDetail.accountNumber)
    },
    enabled: Boolean(drawerVisible && selectedWalletForDetail?.accountNumber),
  })

  // 4. Mutation for Top-up
  const topUpMutation = useMutation({
    mutationFn: (values: TopUpFormData) => accountService.topUp(values),
    onSuccess: (result) => {
      message.success(
        `Nạp ${result.availableBalance?.toLocaleString('vi-VN') || ''} VND vào tài khoản ${result.accountNumber} thành công!`
      )
      setTopUpModalVisible(false)
      topUpForm.resetFields()
      setSelectedWalletForTopUp(null)
      // Invalidate queries to refresh lists and drawer
      queryClient.invalidateQueries({ queryKey: ['accounts-list'] })
      if (selectedWalletForDetail?.accountNumber) {
        queryClient.invalidateQueries({
          queryKey: ['wallet-balance', selectedWalletForDetail.accountNumber],
        })
        queryClient.invalidateQueries({
          queryKey: ['wallet-ledger', selectedWalletForDetail.accountNumber],
        })
      }
    },
    onError: (err: any) => {
      const errorMsg = err?.message || 'Nạp tiền vào ví thất bại. Vui lòng thử lại!'
      message.error(errorMsg)
    },
  })

  // 5. Mutation for Freeze / Unfreeze
  const freezeMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: 'ACTIVE' | 'FROZEN' }) =>
      accountService.toggleFreeze(id, status),
    onSuccess: (_, variables) => {
      const isFreezing = variables.status === 'FROZEN'
      message.success(
        isFreezing ? 'Đã đóng băng ví thành công!' : 'Đã mở khóa ví hoạt động trở lại!'
      )
      queryClient.invalidateQueries({ queryKey: ['accounts-list'] })
      if (selectedWalletForDetail?.id === variables.id) {
        setSelectedWalletForDetail((prev) =>
          prev ? { ...prev, status: variables.status } : null
        )
      }
    },
    onError: (err: any) => {
      const errorMsg = err?.message || 'Cập nhật trạng thái ví thất bại!'
      message.error(errorMsg)
    },
  })

  // Open Top-up Modal
  const handleOpenTopUp = (wallet?: WalletAccountItem) => {
    setSelectedWalletForTopUp(wallet || null)
    topUpForm.resetFields()
    topUpForm.setFieldsValue({
      accountNumber: wallet?.accountNumber || '',
      amount: 500000,
      description: wallet
        ? `Nạp tiền ví ${wallet.accountNumber} (${wallet.customerName})`
        : 'Nạp tiền tài khoản ví',
    })
    setTopUpModalVisible(true)
  }

  // Open Detail Drawer
  const handleOpenDetail = (wallet: WalletAccountItem) => {
    setSelectedWalletForDetail(wallet)
    setDrawerVisible(true)
  }

  // Filter submit
  const handleSearchSubmit = (values: any) => {
    setKeyword(values.keyword || '')
    setFilters({
      status: values.status || undefined,
    })
  }

  // Filter reset
  const handleFilterReset = () => {
    filterForm.resetFields()
    handleReset()
  }

  // Main Table Columns
  const columns: AppTableColumns<WalletAccountItem> = [
    {
      title: 'Số tài khoản ví',
      dataIndex: 'accountNumber',
      key: 'accountNumber',
      width: 180,
      render: (accNo) => (
        <Space size={4}>
          <WalletOutlined style={{ color: '#1677ff' }} />
          <Text copyable strong>
            {accNo}
          </Text>
        </Space>
      ),
    },
    {
      title: 'Chủ tài khoản',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 180,
      render: (name) => <Text strong>{name}</Text>,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      width: 200,
      render: (email) => (email ? <Text type="secondary">{email}</Text> : <Text type="secondary">-</Text>),
    },
    {
      title: 'Số điện thoại',
      dataIndex: 'phone',
      key: 'phone',
      width: 140,
      render: (phone) => (phone ? <Text>{phone}</Text> : <Text type="secondary">-</Text>),
    },
    {
      title: 'Số dư khả dụng',
      dataIndex: 'availableBalance',
      key: 'availableBalance',
      width: 170,
      align: 'right',
      render: (val, record) => (
        <MoneyDisplay amount={val} currency={record.currency} bold colorType="auto" />
      ),
    },
    {
      title: 'Số dư sổ cái',
      dataIndex: 'ledgerBalance',
      key: 'ledgerBalance',
      width: 170,
      align: 'right',
      render: (val, record) => (
        <MoneyDisplay amount={val} currency={record.currency} bold />
      ),
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
      title: 'Ngày tạo',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      render: (date) => formatDate(date),
    },
  ]

  // Ledger Table Columns in Drawer
  const ledgerColumns: AppTableColumns<WalletLedgerEntry> = [
    {
      title: 'Mã bút toán',
      dataIndex: 'id',
      key: 'id',
      width: 120,
      render: (id: string) => (
        <Tooltip title={id}>
          <Text code copyable>
            {id ? (id.length > 10 ? `${id.substring(0, 8)}...` : id) : '-'}
          </Text>
        </Tooltip>
      ),
    },
    {
      title: 'Mã GD sổ cái (Txn ID)',
      dataIndex: 'ledgerTransactionId',
      key: 'ledgerTransactionId',
      width: 170,
      render: (txnId: string) => (
        <Text copyable strong>
          {txnId || '-'}
        </Text>
      ),
    },
    {
      title: 'Vế ghi nhận',
      dataIndex: 'entryType',
      key: 'entryType',
      width: 110,
      align: 'center',
      render: (type: 'DEBIT' | 'CREDIT') =>
        type === 'DEBIT' ? (
          <Tag color="error">Nợ (DEBIT)</Tag>
        ) : (
          <Tag color="success">Có (CREDIT)</Tag>
        ),
    },
    {
      title: 'Loại tài khoản',
      dataIndex: 'accountType',
      key: 'accountType',
      width: 140,
      render: (accType: string) => <Tag color="blue">{accType || 'WalletAccount'}</Tag>,
    },
    {
      title: 'Số tiền biến động',
      dataIndex: 'amount',
      key: 'amount',
      width: 150,
      align: 'right',
      render: (amount: number, record: WalletLedgerEntry) => (
        <MoneyDisplay
          amount={amount}
          currency={record.currency}
          colorType={record.entryType === 'DEBIT' ? 'expense' : 'income'}
          showSign
          bold
        />
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
      title="Quản lý Tài khoản & Ví"
      subTitle="Danh sách ví khách hàng, tra cứu số dư và nạp tiền tài khoản"
      extra={
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => handleOpenTopUp()}
        >
          Nạp tiền ví (Top-up)
        </Button>
      }
    >
      {/* Search & Filter Toolbar */}
      <AppFilter
        form={filterForm}
        onSearch={handleSearchSubmit}
        onReset={handleFilterReset}
      >
        <Col xs={24} sm={12} md={8}>
          <Form.Item name="keyword" label="Tìm kiếm" style={{ marginBottom: 0 }}>
            <Input
              placeholder="Số ví, tên khách hàng, email, SĐT..."
              allowClear
              onPressEnter={() => filterForm.submit()}
            />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="status" label="Trạng thái" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả trạng thái"
              allowClear
              options={[
                { label: 'Hoạt động (ACTIVE)', value: 'ACTIVE' },
                { label: 'Tạm khóa (FROZEN)', value: 'FROZEN' },
                { label: 'Đã đóng (CLOSED)', value: 'CLOSED' },
              ]}
            />
          </Form.Item>
        </Col>
      </AppFilter>

      {/* Error alert if fetch failed */}
      {isAccountsError && (
        <Alert
          type="error"
          showIcon
          message="Không thể tải danh sách ví"
          description="Đã xảy ra lỗi khi kết nối tới máy chủ. Vui lòng kiểm tra kết nối và thử lại."
          action={
            <Button size="small" danger onClick={() => refetchAccounts()}>
              Tải lại
            </Button>
          }
          style={{ marginBottom: 16 }}
        />
      )}

      {/* Main Wallets Table */}
      <AppTable<WalletAccountItem>
        rowKey="id"
        columns={columns}
        dataSource={accountsData?.items || []}
        loading={isLoadingAccounts}
        pagination={pagination}
        onChange={handleTableChange}
        onRefresh={() => refetchAccounts()}
        actionColumn={{
          title: 'Thao tác',
          width: 170,
          fixed: 'right',
          render: (_, record) => {
            const isFrozen = record.status === 'FROZEN'
            return (
              <ActionMenu
                maxInline={2}
                items={[
                  {
                    key: 'topup',
                    label: 'Nạp tiền',
                    icon: <DollarOutlined />,
                    disabled: isFrozen,
                    onClick: () => handleOpenTopUp(record),
                  },
                  {
                    key: 'view',
                    label: 'Xem số dư & Lịch sử sổ cái',
                    icon: <EyeOutlined />,
                    onClick: () => handleOpenDetail(record),
                  },
                  {
                    key: 'freeze',
                    label: isFrozen ? 'Mở khoá ví' : 'Đóng băng ví',
                    danger: !isFrozen,
                    icon: isFrozen ? <UnlockOutlined /> : <LockOutlined />,
                    confirm: {
                      title: isFrozen
                        ? `Xác nhận mở khoá tài khoản ví?`
                        : `Xác nhận đóng băng tài khoản ví?`,
                      description: `Số ví: ${record.accountNumber} - Khách hàng: ${record.customerName}`,
                      okText: isFrozen ? 'Mở khoá' : 'Đóng băng',
                    },
                    onClick: () =>
                      freezeMutation.mutate({
                        id: record.id,
                        status: isFrozen ? 'ACTIVE' : 'FROZEN',
                      }),
                  },
                ]}
              />
            )
          },
        }}
      />

      {/* Top-up Modal */}
      <Modal
        title={
          <Space>
            <DollarOutlined style={{ color: '#52c41a' }} />
            <span>Nạp tiền vào ví tài khoản</span>
          </Space>
        }
        open={topUpModalVisible}
        onCancel={() => {
          setTopUpModalVisible(false)
          setSelectedWalletForTopUp(null)
        }}
        onOk={() => topUpForm.submit()}
        confirmLoading={topUpMutation.isPending}
        destroyOnClose
        okText="Xác nhận nạp tiền"
        cancelText="Hủy"
      >
        <Form
          form={topUpForm}
          layout="vertical"
          onFinish={(values) => topUpMutation.mutate(values)}
          initialValues={{
            amount: 500000,
            description: selectedWalletForTopUp
              ? `Nạp tiền ví ${selectedWalletForTopUp.accountNumber}`
              : 'Nạp tiền tài khoản ví',
          }}
        >
          {selectedWalletForTopUp && (
            <Alert
              type="info"
              showIcon
              message={
                <div>
                  <Text strong>Khách hàng: </Text>
                  <Text>{selectedWalletForTopUp.customerName}</Text>
                  {selectedWalletForTopUp.phone && ` - ${selectedWalletForTopUp.phone}`}
                </div>
              }
              description={
                <div>
                  <Text>Số dư hiện tại: </Text>
                  <MoneyDisplay
                    amount={selectedWalletForTopUp.availableBalance}
                    currency={selectedWalletForTopUp.currency}
                    bold
                  />
                </div>
              }
              style={{ marginBottom: 16 }}
            />
          )}

          <Form.Item
            name="accountNumber"
            label="Số tài khoản ví"
            rules={[
              { required: true, message: 'Vui lòng nhập số tài khoản ví' },
              { whitespace: true, message: 'Số tài khoản ví không được để trống' },
            ]}
          >
            <Input
              placeholder="Ví dụ: WA-8801928371"
              prefix={<WalletOutlined />}
              disabled={Boolean(selectedWalletForTopUp)}
              allowClear={!selectedWalletForTopUp}
            />
          </Form.Item>

          <Form.Item
            name="amount"
            label="Số tiền nạp (VND)"
            rules={[
              { required: true, message: 'Vui lòng nhập số tiền nạp' },
              { type: 'number', min: 10000, message: 'Số tiền tối thiểu là 10,000 VND' },
            ]}
          >
            <InputNumber
              style={{ width: '100%' }}
              formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
              parser={(value) => (value ? Number(value.replace(/\$\s?|(,*)/g, '')) : 0)}
              placeholder="Nhập số tiền nạp (ví dụ 500,000)"
              addonAfter="VND"
              step={50000}
            />
          </Form.Item>

          {/* Quick preset chips */}
          <div style={{ marginBottom: 16 }}>
            <Text type="secondary" style={{ fontSize: 12, display: 'block', marginBottom: 6 }}>
              Chọn nhanh số tiền:
            </Text>
            <Space wrap size={[6, 6]}>
              {QUICK_AMOUNT_PRESETS.map((preset) => (
                <Button
                  key={preset.value}
                  size="small"
                  onClick={() => topUpForm.setFieldValue('amount', preset.value)}
                >
                  {preset.label}
                </Button>
              ))}
            </Space>
          </div>

          <Form.Item name="description" label="Ghi chú nghiệp vụ">
            <Input.TextArea
              rows={3}
              placeholder="Nội dung / Lý do nạp tiền vào ví"
              maxLength={255}
              showCount
            />
          </Form.Item>
        </Form>
      </Modal>

      {/* Wallet Balance & Double-Entry Ledger History Drawer */}
      <Drawer
        title={
          <Space>
            <HistoryOutlined style={{ color: '#1677ff' }} />
            <span>Chi tiết Số dư & Lịch sử Sổ cái (Double-Entry Ledger)</span>
          </Space>
        }
        width={760}
        open={drawerVisible}
        onClose={() => {
          setDrawerVisible(false)
          setSelectedWalletForDetail(null)
        }}
        destroyOnClose
        extra={
          <Space>
            <Button
              icon={<ReloadOutlined />}
              onClick={() => {
                refetchBalance()
                refetchLedger()
              }}
              loading={isLoadingBalance || isLoadingLedger}
            >
              Làm mới
            </Button>
            {selectedWalletForDetail && selectedWalletForDetail.status === 'ACTIVE' && (
              <Button
                type="primary"
                icon={<DollarOutlined />}
                onClick={() => handleOpenTopUp(selectedWalletForDetail)}
              >
                Nạp tiền
              </Button>
            )}
          </Space>
        }
      >
        {selectedWalletForDetail && (
          <div>
            {/* Account Info Descriptions */}
            <Descriptions
              bordered
              size="small"
              column={{ xs: 1, sm: 2 }}
              style={{ marginBottom: 16 }}
            >
              <Descriptions.Item label="Số tài khoản ví">
                <Text copyable strong>
                  {selectedWalletForDetail.accountNumber}
                </Text>
              </Descriptions.Item>
              <Descriptions.Item label="Trạng thái">
                <StatusTag status={selectedWalletForDetail.status} useBadge />
              </Descriptions.Item>
              <Descriptions.Item label="Chủ tài khoản">
                <Text strong>{selectedWalletForDetail.customerName}</Text>
              </Descriptions.Item>
              <Descriptions.Item label="Số điện thoại">
                {selectedWalletForDetail.phone || '-'}
              </Descriptions.Item>
              <Descriptions.Item label="Email" span={2}>
                {selectedWalletForDetail.email || '-'}
              </Descriptions.Item>
            </Descriptions>

            {/* Live Balance Summary Cards */}
            <Spin spinning={isLoadingBalance}>
              <Row gutter={16} style={{ marginBottom: 20 }}>
                <Col xs={24} sm={12}>
                  <Card
                    style={{
                      background: '#f6ffed',
                      borderColor: '#b7eb8f',
                      borderRadius: 8,
                    }}
                    styles={{ body: { padding: 16 } }}
                  >
                    <Text type="secondary">Số dư khả dụng (Available)</Text>
                    <div style={{ marginTop: 8 }}>
                      <MoneyDisplay
                        amount={liveBalance?.availableBalance ?? selectedWalletForDetail.availableBalance}
                        currency={liveBalance?.currency ?? selectedWalletForDetail.currency}
                        bold
                        colorType="income"
                        style={{ fontSize: 22, fontWeight: 700 }}
                      />
                    </div>
                  </Card>
                </Col>

                <Col xs={24} sm={12}>
                  <Card
                    style={{
                      background: '#e6f4ff',
                      borderColor: '#91caff',
                      borderRadius: 8,
                    }}
                    styles={{ body: { padding: 16 } }}
                  >
                    <Text type="secondary">Số dư sổ cái (Ledger)</Text>
                    <div style={{ marginTop: 8 }}>
                      <MoneyDisplay
                        amount={liveBalance?.ledgerBalance ?? selectedWalletForDetail.ledgerBalance}
                        currency={liveBalance?.currency ?? selectedWalletForDetail.currency}
                        bold
                        style={{ fontSize: 22, fontWeight: 700, color: '#0958d9' }}
                      />
                    </div>
                  </Card>
                </Col>
              </Row>
            </Spin>

            <Divider style={{ margin: '16px 0 12px 0' }}>
              <Space>
                <HistoryOutlined />
                <Text strong>Lịch sử bút toán sổ cái của ví</Text>
              </Space>
            </Divider>

            <Paragraph type="secondary" style={{ fontSize: 13, marginBottom: 12 }}>
              Các bút toán ghi nhận Nợ / Có (Debit / Credit) tác động trực tiếp lên tài khoản ví này.
            </Paragraph>

            {/* Ledger Entries Table */}
            <AppTable<WalletLedgerEntry>
              rowKey="id"
              columns={ledgerColumns}
              dataSource={ledgerEntries || []}
              loading={isLoadingLedger}
              pagination={{ pageSize: 5 }}
              size="small"
              autoHeight={false}
              showToolbar={false}
              locale={{
                emptyText: <Empty description="Chưa có bút toán sổ cái nào cho ví này" />,
              }}
            />
          </div>
        )}
      </Drawer>
    </PageContainer>
  )
}
