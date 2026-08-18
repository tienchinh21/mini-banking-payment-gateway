import React from 'react'
import { Form, Input, Select, Typography, Col, Tag, Alert } from 'antd'
import { CheckCircleOutlined } from '@ant-design/icons'
import { useQuery } from '@tanstack/react-query'
import {
  PageContainer,
  AppTable,
  AppFilter,
  MoneyDisplay,
  StatusTag,
  type AppTableColumns,
} from '@/components/core'
import { useTable } from '@/hooks/useTable'
import { formatDate } from '@/utils/format'

const { Text } = Typography

interface LedgerEntryItem {
  id: string
  transactionId: string
  transactionType: string
  accountId: string
  accountName: string
  accountType: 'USER_WALLET' | 'MERCHANT_SETTLEMENT' | 'PLATFORM_CLEARING' | 'PLATFORM_FEE'
  entryType: 'DEBIT' | 'CREDIT'
  amount: number
  currency: string
  createdAt: string
}

const mockLedgerEntries: LedgerEntryItem[] = [
  {
    id: 'ENT-001',
    transactionId: 'TXN-9001',
    transactionType: 'PAYMENT_DIRECT_DEBIT',
    accountId: 'WA-8801928371',
    accountName: 'Ví người dùng (Nguyễn Văn An)',
    accountType: 'USER_WALLET',
    entryType: 'DEBIT',
    amount: 250000,
    currency: 'VND',
    createdAt: '2026-08-15T10:00:00Z',
  },
  {
    id: 'ENT-002',
    transactionId: 'TXN-9001',
    transactionType: 'PAYMENT_DIRECT_DEBIT',
    accountId: 'SYS-CLEARING-01',
    accountName: 'Tài khoản Platform Clearing',
    accountType: 'PLATFORM_CLEARING',
    entryType: 'CREDIT',
    amount: 250000,
    currency: 'VND',
    createdAt: '2026-08-15T10:00:00Z',
  },
  {
    id: 'ENT-003',
    transactionId: 'TXN-9002',
    transactionType: 'TOP_UP',
    accountId: 'SYS-CLEARING-01',
    accountName: 'Tài khoản Platform Clearing',
    accountType: 'PLATFORM_CLEARING',
    entryType: 'DEBIT',
    amount: 1000000,
    currency: 'VND',
    createdAt: '2026-08-15T10:30:00Z',
  },
  {
    id: 'ENT-004',
    transactionId: 'TXN-9002',
    transactionType: 'TOP_UP',
    accountId: 'WA-8801928372',
    accountName: 'Ví người dùng (Trần Thị Bình)',
    accountType: 'USER_WALLET',
    entryType: 'CREDIT',
    amount: 1000000,
    currency: 'VND',
    createdAt: '2026-08-15T10:30:00Z',
  },
]

export const LedgerListPage: React.FC = () => {
  const [filterForm] = Form.useForm()

  const {
    queryParams,
    pagination,
    setTotal,
    setKeyword,
    setFilters,
    handleTableChange,
    handleReset,
  } = useTable<LedgerEntryItem>({
    defaultPageSize: 10,
  })

  const { data, isLoading, refetch } = useQuery({
    queryKey: ['ledger-entries', queryParams],
    queryFn: async () => {
      let list = [...mockLedgerEntries]
      if (queryParams.keyword) {
        const kw = String(queryParams.keyword).toLowerCase()
        list = list.filter(
          (e) =>
            e.transactionId.toLowerCase().includes(kw) ||
            e.accountId.toLowerCase().includes(kw) ||
            e.accountName.toLowerCase().includes(kw)
        )
      }
      if (queryParams.entryType) {
        list = list.filter((e) => e.entryType === queryParams.entryType)
      }
      setTotal(list.length)
      return { items: list, total: list.length }
    },
  })

  const columns: AppTableColumns<LedgerEntryItem> = [
    {
      title: 'Mã bút toán (Entry ID)',
      dataIndex: 'id',
      key: 'id',
      width: 140,
      render: (id) => <Text code>{id}</Text>,
    },
    {
      title: 'Mã giao dịch sổ cái (Txn ID)',
      dataIndex: 'transactionId',
      key: 'transactionId',
      width: 160,
      render: (txn) => <Text copyable strong>{txn}</Text>,
    },
    {
      title: 'Loại nghiệp vụ',
      dataIndex: 'transactionType',
      key: 'transactionType',
      width: 180,
      render: (type) => <StatusTag status={type} />,
    },
    {
      title: 'Tài khoản ghi nhận',
      dataIndex: 'accountId',
      key: 'accountId',
      width: 220,
      render: (accId, record) => (
        <div>
          <Text strong>{record.accountName}</Text>
          <div>
            <Text type="secondary" style={{ fontSize: 12 }}>
              {accId}
            </Text>
          </div>
        </div>
      ),
    },
    {
      title: 'Phân loại',
      dataIndex: 'accountType',
      key: 'accountType',
      width: 160,
      render: (type) => <Tag>{type}</Tag>,
    },
    {
      title: 'Ghi Nợ (Debit)',
      key: 'debit',
      width: 150,
      align: 'right',
      render: (_, record) =>
        record.entryType === 'DEBIT' ? (
          <MoneyDisplay amount={record.amount} currency={record.currency} colorType="expense" bold />
        ) : (
          '-'
        ),
    },
    {
      title: 'Ghi Có (Credit)',
      key: 'credit',
      width: 150,
      align: 'right',
      render: (_, record) =>
        record.entryType === 'CREDIT' ? (
          <MoneyDisplay amount={record.amount} currency={record.currency} colorType="income" bold />
        ) : (
          '-'
        ),
    },
    {
      title: 'Thời gian',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date) => formatDate(date),
    },
  ]

  const handleSearchSubmit = (values: any) => {
    setKeyword(values.keyword || '')
    setFilters({
      entryType: values.entryType,
    })
  }

  return (
    <PageContainer
      title="Sổ cái kép (Double-Entry Ledger)"
      subTitle="Nhật ký bút toán kế toán bất biến đảm bảo nguyên lý Tổng Nợ (Debit) = Tổng Có (Credit)"
    >
      <Alert
        message="Bất biến kế toán: sum(Debit) == sum(Credit)"
        description="Mọi biến động số dư trong hệ thống Mini Banking đều tạo ra các cặp bút toán Debit & Credit đối ứng cân bằng trong cùng 1 database transaction."
        type="info"
        showIcon
        icon={<CheckCircleOutlined style={{ color: '#52c41a' }} />}
        style={{ marginBottom: 16 }}
      />

      <AppFilter
        form={filterForm}
        onSearch={handleSearchSubmit}
        onReset={() => {
          filterForm.resetFields()
          handleReset()
        }}
      >
        <Col xs={24} sm={12} md={8}>
          <Form.Item name="keyword" label="Tìm kiếm" style={{ marginBottom: 0 }}>
            <Input placeholder="Mã bút toán, mã GD, số tài khoản..." allowClear />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="entryType" label="Vế ghi nhận" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả (Debit / Credit)"
              allowClear
              options={[
                { label: 'Ghi Nợ (Debit)', value: 'DEBIT' },
                { label: 'Ghi Có (Credit)', value: 'CREDIT' },
              ]}
            />
          </Form.Item>
        </Col>
      </AppFilter>

      <AppTable<LedgerEntryItem>
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
