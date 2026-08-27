import React, { useState } from 'react'
import {
  Form,
  Input,
  Select,
  Table,
  Typography,
  Col,
  Row,
  Tag,
  Alert,
  Modal,
  Card,
  Space,
  Button,
  Statistic,
  Spin,
  Tooltip,
} from 'antd'
import {
  CheckCircleOutlined,
  EyeOutlined,
  SafetyCertificateOutlined,
  ReloadOutlined,
  AuditOutlined,
  WarningOutlined,
} from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import {
  PageContainer,
  AppTable,
  AppFilter,
  MoneyDisplay,
  type AppTableColumns,
} from '@/components/core'
import { useTable } from '@/hooks/useTable'
import { formatDate, formatMoney } from '@/utils/format'
import { ledgerService } from '../services/ledgerService'
import type { LedgerEntryItem, EntryType } from '../types'

const { Text, Title, Paragraph } = Typography

/**
 * Render appropriate badge tag for transaction type
 */
const renderTransactionTypeTag = (type: string) => {
  const normalized = (type || '').toUpperCase()
  if (normalized.includes('TOP_UP') || normalized.includes('TOPUP')) {
    return <Tag color="green">Nạp tiền ví (TopUp)</Tag>
  }
  if (normalized.includes('PAYMENT')) {
    return <Tag color="blue">Thanh toán (Payment)</Tag>
  }
  if (normalized.includes('REFUND')) {
    return <Tag color="orange">Hoàn tiền (Refund)</Tag>
  }
  if (normalized.includes('SETTLEMENT')) {
    return <Tag color="cyan">Quyết toán (Settlement)</Tag>
  }
  if (normalized.includes('ADJUSTMENT')) {
    return <Tag color="geekblue">Điều chỉnh (Adjustment)</Tag>
  }
  return <Tag>{type || 'N/A'}</Tag>
}

/**
 * Render tag for ledger account type
 */
const renderAccountTypeTag = (accountType: string) => {
  switch (accountType) {
    case 'WalletAccount':
    case 'USER_WALLET':
      return <Tag color="cyan">Ví người dùng</Tag>
    case 'PlatformClearing':
    case 'PLATFORM_CLEARING':
      return <Tag color="purple">Platform Clearing</Tag>
    case 'MerchantSettlement':
    case 'MERCHANT_SETTLEMENT':
      return <Tag color="gold">Merchant Settlement</Tag>
    case 'PlatformFee':
    case 'PLATFORM_FEE':
      return <Tag color="magenta">Platform Fee</Tag>
    default:
      return <Tag>{accountType}</Tag>
  }
}

export const LedgerListPage: React.FC = () => {
  const [filterForm] = Form.useForm()

  // State for Transaction Inspection Modal
  const [inspectModalOpen, setInspectModalOpen] = useState(false)
  const [selectedTxId, setSelectedTxId] = useState<string | null>(null)

  const {
    queryParams,
    pagination,
    setTotal,
    setFilters,
    handleTableChange,
    handleReset,
  } = useTable<LedgerEntryItem>({
    defaultPageSize: 10,
  })

  // 1. Fetch live ledger entries with server-side pagination & filter
  const { data, isLoading, isError, error, refetch } = useQuery({
    queryKey: ['ledger-entries', queryParams],
    queryFn: async () => {
      const result = await ledgerService.getEntries({
        page: queryParams.page,
        pageSize: queryParams.pageSize,
        keyword: queryParams.keyword,
        entryType: queryParams.entryType as EntryType,
      })
      if (result.meta) {
        setTotal(result.meta.totalItems)
      }
      return result
    },
  })

  // 2. Fetch counterpart entries for inspecting balanced transaction
  const {
    data: txDetailData,
    isLoading: isLoadingTxDetail,
  } = useQuery({
    queryKey: ['ledger-transaction-entries', selectedTxId],
    queryFn: async () => {
      if (!selectedTxId) return null
      return await ledgerService.getEntriesByTransactionId(selectedTxId)
    },
    enabled: Boolean(selectedTxId && inspectModalOpen),
  })

  const handleOpenInspectModal = (transactionId: string) => {
    setSelectedTxId(transactionId)
    setInspectModalOpen(true)
  }

  const handleCloseInspectModal = () => {
    setInspectModalOpen(false)
    setSelectedTxId(null)
  }

  const handleSearchSubmit = (values: { keyword?: string; entryType?: string }) => {
    setFilters({
      keyword: values.keyword || undefined,
      entryType: values.entryType || undefined,
    })
  }

  // ── Table Column Definitions ──────────────────────────────────────────────
  const columns: AppTableColumns<LedgerEntryItem> = [
    {
      title: 'Mã bút toán (Entry ID)',
      dataIndex: 'id',
      key: 'id',
      width: 140,
      render: (id) => (
        <Tooltip title="Nhấn để sao chép mã bút toán">
          <Text code copyable strong style={{ fontSize: 13 }}>
            {id}
          </Text>
        </Tooltip>
      ),
    },
    {
      title: 'Mã giao dịch (Tx ID)',
      dataIndex: 'transactionId',
      key: 'transactionId',
      width: 200,
      render: (txId) => (
        <Space orientation="vertical" size={2}>
          <Text
            strong
            style={{ color: '#1677ff', cursor: 'pointer', fontFamily: 'monospace' }}
            onClick={() => handleOpenInspectModal(txId)}
          >
            {txId}
          </Text>
          <Text type="secondary" style={{ fontSize: 11 }}>
            (Nhấn để xem đối ứng)
          </Text>
        </Space>
      ),
    },
    {
      title: 'Loại GD',
      dataIndex: 'transactionType',
      key: 'transactionType',
      width: 150,
      render: (type) => renderTransactionTypeTag(type),
    },
    {
      title: 'Loại TK',
      dataIndex: 'accountType',
      key: 'accountType',
      width: 150,
      render: (accType) => renderAccountTypeTag(accType),
    },
    {
      title: 'Tài khoản ghi nhận',
      dataIndex: 'accountName',
      key: 'accountName',
      width: 220,
      render: (name, record) => (
        <Space orientation="vertical" size={0}>
          <Text strong style={{ fontSize: 13 }}>
            {name || 'Hệ thống'}
          </Text>
          <Text type="secondary" code style={{ fontSize: 11 }}>
            {record.accountId}
          </Text>
        </Space>
      ),
    },
    {
      title: 'Vế ghi nhận',
      dataIndex: 'entryType',
      key: 'entryType',
      width: 130,
      align: 'center',
      render: (type) => {
        const isDebit = String(type).toUpperCase() === 'DEBIT'
        return (
          <Tag
            color={isDebit ? 'error' : 'success'}
            style={{
              fontWeight: 600,
              padding: '2px 8px',
              borderRadius: 4,
            }}
          >
            {isDebit ? 'GHI NỢ (DEBIT)' : 'GHI CÓ (CREDIT)'}
          </Tag>
        )
      },
    },
    {
      title: 'Số tiền phát sinh',
      dataIndex: 'amount',
      key: 'amount',
      width: 170,
      align: 'right',
      render: (val, record) => {
        const isDebit = String(record.entryType).toUpperCase() === 'DEBIT'
        return (
          <MoneyDisplay
            amount={val}
            currency={record.currency || 'VND'}
            bold
            showSign
            colorType={isDebit ? 'expense' : 'income'}
          />
        )
      },
    },
    {
      title: 'Thời gian hạch toán',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 160,
      render: (date) => (
        <Text type="secondary" style={{ fontSize: 12 }}>
          {formatDate(date)}
        </Text>
      ),
    },
  ]

  // Columns for inspect modal sub-table
  const inspectColumns = [
    {
      title: 'Mã bút toán',
      dataIndex: 'id',
      key: 'id',
      width: 130,
      render: (id: string) => <Text code copyable>{id}</Text>,
    },
    {
      title: 'Tài khoản',
      dataIndex: 'accountName',
      key: 'accountName',
      render: (name: string, record: LedgerEntryItem) => (
        <div>
          <div><strong>{name}</strong></div>
          <Text type="secondary" code style={{ fontSize: 11 }}>{record.accountId}</Text>
        </div>
      ),
    },
    {
      title: 'Loại tài khoản',
      dataIndex: 'accountType',
      key: 'accountType',
      width: 150,
      render: (type: string) => renderAccountTypeTag(type),
    },
    {
      title: 'Vế ghi nhận',
      dataIndex: 'entryType',
      key: 'entryType',
      width: 130,
      align: 'center' as const,
      render: (type: string) => {
        const isDebit = String(type).toUpperCase() === 'DEBIT'
        return (
          <Tag color={isDebit ? 'error' : 'success'} style={{ fontWeight: 600 }}>
            {isDebit ? 'DEBIT (-)' : 'CREDIT (+)'}
          </Tag>
        )
      },
    },
    {
      title: 'Số tiền',
      dataIndex: 'amount',
      key: 'amount',
      width: 160,
      align: 'right' as const,
      render: (val: number, record: LedgerEntryItem) => {
        const isDebit = String(record.entryType).toUpperCase() === 'DEBIT'
        return (
          <MoneyDisplay
            amount={val}
            currency={record.currency || 'VND'}
            bold
            showSign
            colorType={isDebit ? 'expense' : 'income'}
          />
        )
      },
    },
  ]

  return (
    <PageContainer
      title="Sổ cái & Bút toán kế toán"
      subTitle="Nhật ký hạch toán Double-Entry ghi nhận mọi biến động nợ/có trên toàn hệ thống"
      extra={
        <Button
          icon={<ReloadOutlined />}
          loading={isLoading}
          onClick={() => refetch()}
        >
          Làm mới dữ liệu
        </Button>
      }
    >
      {/* 1. Double-entry Invariant Explanation Card */}
      <Card
        style={{
          background: 'linear-gradient(135deg, #f0f5ff 0%, #e6f7ff 100%)',
          borderColor: '#91caff',
          borderRadius: 8,
          marginBottom: 16,
        }}
        size="small"
      >
        <Row align="middle" gutter={[16, 16]}>
          <Col xs={24} md={16}>
            <Space align="start">
              <SafetyCertificateOutlined
                style={{ fontSize: 28, color: '#1677ff', marginTop: 4 }}
              />
              <div>
                <Title level={5} style={{ margin: 0, color: '#0958d9' }}>
                  Kiến trúc Sổ cái kép (Double-Entry Invariant)
                </Title>
                <Paragraph
                  style={{ margin: '4px 0 0', color: '#595959', fontSize: 13 }}
                >
                  Mọi luồng tiền trong hệ thống đều tuân thủ nguyên tắc bất biến:{' '}
                  <Text strong style={{ color: '#0958d9' }}>
                    ∑ Tổng Nợ (Debit) = ∑ Tổng Có (Credit)
                  </Text>
                  . Dữ liệu chỉ ghi thêm (Append-Only), không thể tự ý sửa xoá số dư mà không qua bút toán đối ứng.
                </Paragraph>
              </div>
            </Space>
          </Col>

          <Col xs={24} md={8}>
            <Row gutter={8}>
              <Col span={12}>
                <Card
                  size="small"
                  style={{
                    background: '#fff',
                    borderRadius: 6,
                    border: '1px solid #bae0ff',
                    textAlign: 'center',
                  }}
                >
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    Vế Ghi Nợ (Debit)
                  </Text>
                  <div style={{ marginTop: 2 }}>
                    <Text strong style={{ color: '#cf1322', fontSize: 14 }}>
                      Giảm ví / Tăng chi
                    </Text>
                  </div>
                </Card>
              </Col>
              <Col span={12}>
                <Card
                  size="small"
                  style={{
                    background: '#fff',
                    borderRadius: 6,
                    border: '1px solid #bae0ff',
                    textAlign: 'center',
                  }}
                >
                  <Text type="secondary" style={{ fontSize: 12 }}>
                    Vế Ghi Có (Credit)
                  </Text>
                  <div style={{ marginTop: 2 }}>
                    <Text strong style={{ color: '#389e0d', fontSize: 14 }}>
                      Tăng ví / Tăng thu
                    </Text>
                  </div>
                </Card>
              </Col>
            </Row>
          </Col>
        </Row>
      </Card>

      {/* 2. Error alert if API fetch fails */}
      {isError && (
        <Alert
          message="Lỗi tải dữ liệu sổ cái"
          description={
            (error as Error)?.message ||
            'Không thể kết nối đến máy chủ API để lấy danh sách bút toán sổ cái. Vui lòng kiểm tra lại kết nối.'
          }
          type="error"
          showIcon
          icon={<WarningOutlined />}
          action={
            <Button
              size="small"
              danger
              icon={<ReloadOutlined />}
              onClick={() => refetch()}
            >
              Thử lại
            </Button>
          }
          style={{ marginBottom: 16 }}
        />
      )}

      {/* 3. Filter Bar */}
      <AppFilter
        form={filterForm}
        onSearch={handleSearchSubmit}
        onReset={() => {
          filterForm.resetFields()
          handleReset()
        }}
      >
        <Col xs={24} sm={12} md={10}>
          <Form.Item
            name="keyword"
            label="Tìm kiếm nhanh"
            style={{ marginBottom: 0 }}
          >
            <Input
              placeholder="Nhập Transaction ID, Account ID, Tên tài khoản..."
              allowClear
            />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item
            name="entryType"
            label="Vế ghi nhận"
            style={{ marginBottom: 0 }}
          >
            <Select
              placeholder="Tất cả (Debit / Credit)"
              allowClear
              options={[
                { label: 'Tất cả vế ghi nhận', value: '' },
                { label: 'Ghi Nợ (DEBIT - Trừ)', value: 'DEBIT' },
                { label: 'Ghi Có (CREDIT - Cộng)', value: 'CREDIT' },
              ]}
            />
          </Form.Item>
        </Col>
      </AppFilter>

      {/* 4. Main Ledger Table */}
      <Card bordered={false} style={{ borderRadius: 8 }}>
        <AppTable<LedgerEntryItem>
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
              <Button
                type="link"
                size="small"
                icon={<EyeOutlined />}
                onClick={() => handleOpenInspectModal(record.transactionId)}
              >
                Đối soát GD
              </Button>
            ),
          }}
        />
      </Card>

      {/* 5. Transaction Balancing Inspection Modal */}
      <Modal
        title={
          <Space>
            <AuditOutlined style={{ color: '#1677ff' }} />
            <span>Đối soát bút toán kép theo Giao dịch</span>
          </Space>
        }
        open={inspectModalOpen}
        onCancel={handleCloseInspectModal}
        width={760}
        footer={[
          <Button key="close" type="primary" onClick={handleCloseInspectModal}>
            Đóng
          </Button>,
        ]}
      >
        {isLoadingTxDetail ? (
          <div style={{ textAlign: 'center', padding: '40px 0' }}>
            <Spin size="large" tip="Đang tải các bút toán đối ứng..." />
          </div>
        ) : txDetailData ? (
          <div>
            <div style={{ marginBottom: 16 }}>
              <Text type="secondary">Mã giao dịch (Transaction ID): </Text>
              <Text strong code copyable style={{ fontSize: 13 }}>
                {txDetailData.transactionId}
              </Text>
            </div>

            {/* Invariant Verification Banner */}
            {txDetailData.isBalanced ? (
              <Alert
                message="Bút toán hợp lệ & Cân bằng (Balanced)"
                description="Tổng phát sinh Nợ bằng Tổng phát sinh Có. Giao dịch thỏa mãn tính toàn vẹn của sổ cái kế toán kép."
                type="success"
                showIcon
                icon={<CheckCircleOutlined />}
                style={{ marginBottom: 16 }}
              />
            ) : (
              <Alert
                message="Cảnh báo: Bút toán chưa cân bằng!"
                description="Tổng Nợ và Tổng Có của giao dịch này đang có độ lệch. Cần kiểm tra lại lịch sử ghi nhận."
                type="error"
                showIcon
                style={{ marginBottom: 16 }}
              />
            )}

            {/* Summary Statistics */}
            <Row gutter={16} style={{ marginBottom: 16 }}>
              <Col span={12}>
                <Card size="small" style={{ background: '#fff1f0', borderColor: '#ffa39e' }}>
                  <Statistic
                    title="Tổng phát sinh Nợ (Debit)"
                    value={formatMoney(txDetailData.totalDebit)}
                    valueStyle={{ color: '#cf1322', fontWeight: 600 }}
                    suffix={txDetailData.currency}
                  />
                </Card>
              </Col>
              <Col span={12}>
                <Card size="small" style={{ background: '#f6ffed', borderColor: '#b7eb8f' }}>
                  <Statistic
                    title="Tổng phát sinh Có (Credit)"
                    value={formatMoney(txDetailData.totalCredit)}
                    valueStyle={{ color: '#389e0d', fontWeight: 600 }}
                    suffix={txDetailData.currency}
                  />
                </Card>
              </Col>
            </Row>

            {/* Sub-table with counterpart entries */}
            <Table<LedgerEntryItem>
              rowKey="id"
              size="small"
              columns={inspectColumns}
              dataSource={txDetailData.entries}
              pagination={false}
              bordered
              summary={() => (
                <Table.Summary fixed>
                  <Table.Summary.Row style={{ background: '#fafafa', fontWeight: 600 }}>
                    <Table.Summary.Cell index={0} colSpan={3}>
                      Tổng cộng cân bằng:
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={1} align="center">
                      <Tag color={txDetailData.isBalanced ? 'success' : 'error'}>
                        {txDetailData.isBalanced ? 'CÂN BẰNG' : 'LỆCH'}
                      </Tag>
                    </Table.Summary.Cell>
                    <Table.Summary.Cell index={2} align="right">
                      <Text strong style={{ color: '#1677ff' }}>
                        {formatMoney(txDetailData.totalDebit)} {txDetailData.currency}
                      </Text>
                    </Table.Summary.Cell>
                  </Table.Summary.Row>
                </Table.Summary>
              )}
            />
          </div>
        ) : (
          <Alert
            message="Không tìm thấy bút toán nào cho giao dịch này."
            type="warning"
            showIcon
          />
        )}
      </Modal>
    </PageContainer>
  )
}
