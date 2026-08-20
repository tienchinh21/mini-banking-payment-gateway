import React, { useState } from 'react'
import {
  Form,
  Input,
  Select,
  Typography,
  Col,
  Tag,
  Alert,
  Tabs,
  Button,
  Card,
  Row,
  Statistic,
  message,
  Space,
} from 'antd'
import {
  CheckCircleOutlined,
  CloseCircleOutlined,
  AuditOutlined,
  TransactionOutlined,
  SyncOutlined,
} from '@ant-design/icons'
import { useQuery, useMutation } from '@tanstack/react-query'
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
import { ledgerService } from '../services/ledgerService'
import type { LedgerEntryItem, LedgerTransactionItem, LedgerReconcileResult } from '../types'

const { Text } = Typography

export const LedgerListPage: React.FC = () => {
  const [activeTab, setActiveTab] = useState('entries')
  const [entryFilterForm] = Form.useForm()
  const [txnFilterForm] = Form.useForm()

  // 1. Entries table hook
  const {
    queryParams: entryParams,
    pagination: entryPagination,
    setTotal: setEntryTotal,
    setKeyword: setEntryKeyword,
    setFilters: setEntryFilters,
    handleTableChange: handleEntryTableChange,
    handleReset: handleEntryReset,
  } = useTable<LedgerEntryItem>({
    defaultPageSize: 10,
  })

  // Query Entries
  const {
    data: entriesData,
    isLoading: entriesLoading,
    refetch: refetchEntries,
  } = useQuery({
    queryKey: ['ledger-entries', entryParams],
    queryFn: async () => {
      const res = await ledgerService.getEntries(entryParams)
      setEntryTotal(res.meta.totalItems)
      return res
    },
  })

  // 2. Transactions table hook
  const {
    queryParams: txnParams,
    pagination: txnPagination,
    setTotal: setTxnTotal,
    setKeyword: setTxnKeyword,
    handleTableChange: handleTxnTableChange,
    handleReset: handleTxnReset,
  } = useTable<LedgerTransactionItem>({
    defaultPageSize: 10,
  })

  // Query Transactions
  const {
    data: txnData,
    isLoading: txnLoading,
    refetch: refetchTxns,
  } = useQuery({
    queryKey: ['ledger-transactions', txnParams],
    queryFn: async () => {
      const res = await ledgerService.getTransactions(txnParams)
      setTxnTotal(res.meta.totalItems)
      return res
    },
  })

  // 3. Reconcile Mutation & State
  const [reconcileResult, setReconcileResult] = useState<LedgerReconcileResult | null>(null)

  const reconcileMutation = useMutation({
    mutationFn: () => ledgerService.reconcile(),
    onSuccess: (result) => {
      setReconcileResult(result)
      if (result.isBalanced) {
        message.success('Đối soát hoàn tất: Sổ cái cân bằng 100%!')
      } else {
        message.warning('Phát hiện chênh lệch giữa vế Nợ và Có!')
      }
    },
    onError: (err: any) => {
      message.error(err?.message || 'Lỗi khi thực hiện đối soát sổ cái')
    },
  })

  // Columns for Entries
  const entryColumns: AppTableColumns<LedgerEntryItem> = [
    {
      title: 'Mã bút toán',
      dataIndex: 'id',
      key: 'id',
      width: 140,
      render: (id) => <Text code>{id}</Text>,
    },
    {
      title: 'Mã giao dịch (Txn)',
      dataIndex: 'transactionId',
      key: 'transactionId',
      width: 170,
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
      width: 240,
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
      width: 160,
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
      width: 160,
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

  // Columns for Transactions
  const txnColumns: AppTableColumns<LedgerTransactionItem> = [
    {
      title: 'Mã tham chiếu (Reference ID)',
      dataIndex: 'referenceId',
      key: 'referenceId',
      width: 200,
      render: (ref) => <Text copyable strong>{ref}</Text>,
    },
    {
      title: 'Loại giao dịch',
      dataIndex: 'type',
      key: 'type',
      width: 160,
      render: (type) => <StatusTag status={String(type)} />,
    },
    {
      title: 'Mô tả nghiệp vụ',
      dataIndex: 'description',
      key: 'description',
      width: 280,
    },
    {
      title: 'Số bút toán liên kết',
      key: 'entriesCount',
      width: 160,
      align: 'center',
      render: (_, record) => (
        <Tag color="blue">{record.entries?.length ?? 'N/A'} bút toán</Tag>
      ),
    },
    {
      title: 'Trạng thái',
      dataIndex: 'status',
      key: 'status',
      width: 130,
      align: 'center',
      render: (status) => <StatusTag status={String(status)} />,
    },
    {
      title: 'Thời gian ghi nhận',
      dataIndex: 'createdAt',
      key: 'createdAt',
      width: 170,
      render: (date) => formatDate(date),
    },
  ]

  return (
    <PageContainer
      title="Sổ cái kép (Double-Entry Ledger)"
      subTitle="Nhật ký bút toán kế toán bất biến đảm bảo nguyên lý Tổng Nợ (Debit) = Tổng Có (Credit)"
      extra={
        <Space>
          <Button
            type="primary"
            icon={<SyncOutlined spin={reconcileMutation.isPending} />}
            loading={reconcileMutation.isPending}
            onClick={() => {
              setActiveTab('reconcile')
              reconcileMutation.mutate()
            }}
          >
            Chạy đối soát (Reconcile)
          </Button>
        </Space>
      }
    >
      <Alert
        message="Bất biến kế toán: sum(Debit) == sum(Credit)"
        description="Mọi biến động số dư trong hệ thống Mini Banking đều tạo ra các cặp bút toán Debit & Credit đối ứng cân bằng trong cùng 1 database transaction."
        type="info"
        showIcon
        icon={<CheckCircleOutlined style={{ color: '#52c41a' }} />}
        style={{ marginBottom: 16 }}
      />

      <Tabs
        activeKey={activeTab}
        onChange={setActiveTab}
        items={[
          {
            key: 'entries',
            label: (
              <span>
                <AuditOutlined /> Nhật ký Bút toán (Entries)
              </span>
            ),
            children: (
              <>
                <AppFilter
                  form={entryFilterForm}
                  onSearch={(values) => {
                    setEntryKeyword(values.keyword || '')
                    setEntryFilters({
                      entryType: values.entryType,
                      accountType: values.accountType,
                    })
                  }}
                  onReset={() => {
                    entryFilterForm.resetFields()
                    handleEntryReset()
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

                  <Col xs={24} sm={12} md={6}>
                    <Form.Item name="accountType" label="Loại tài khoản" style={{ marginBottom: 0 }}>
                      <Select
                        placeholder="Tất cả loại TK"
                        allowClear
                        options={[
                          { label: 'Ví người dùng (WalletAccount)', value: 'WalletAccount' },
                          { label: 'Platform Clearing', value: 'PlatformClearing' },
                          { label: 'Merchant Settlement', value: 'MerchantSettlement' },
                        ]}
                      />
                    </Form.Item>
                  </Col>
                </AppFilter>

                <AppTable<LedgerEntryItem>
                  rowKey="id"
                  columns={entryColumns}
                  dataSource={entriesData?.items || []}
                  loading={entriesLoading}
                  pagination={entryPagination}
                  onChange={handleEntryTableChange}
                  onRefresh={() => refetchEntries()}
                />
              </>
            ),
          },
          {
            key: 'transactions',
            label: (
              <span>
                <TransactionOutlined /> Giao dịch Sổ cái (Transactions)
              </span>
            ),
            children: (
              <>
                <AppFilter
                  form={txnFilterForm}
                  onSearch={(values) => {
                    setTxnKeyword(values.keyword || '')
                  }}
                  onReset={() => {
                    txnFilterForm.resetFields()
                    handleTxnReset()
                  }}
                >
                  <Col xs={24} sm={12} md={8}>
                    <Form.Item name="keyword" label="Tìm kiếm" style={{ marginBottom: 0 }}>
                      <Input placeholder="Mã tham chiếu, nội dung..." allowClear />
                    </Form.Item>
                  </Col>
                </AppFilter>

                <AppTable<LedgerTransactionItem>
                  rowKey="id"
                  columns={txnColumns}
                  dataSource={txnData?.items || []}
                  loading={txnLoading}
                  pagination={txnPagination}
                  onChange={handleTxnTableChange}
                  onRefresh={() => refetchTxns()}
                />
              </>
            ),
          },
          {
            key: 'reconcile',
            label: (
              <span>
                <SyncOutlined /> Đối soát Sổ cái (Reconcile)
              </span>
            ),
            children: (
              <div>
                <Card
                  title="Kết quả kiểm toán đối soát cân bằng"
                  bordered={false}
                  extra={
                    <Button
                      type="primary"
                      icon={<SyncOutlined spin={reconcileMutation.isPending} />}
                      loading={reconcileMutation.isPending}
                      onClick={() => reconcileMutation.mutate()}
                    >
                      Chạy lại đối soát
                    </Button>
                  }
                  style={{ marginBottom: 24 }}
                >
                  {reconcileResult ? (
                    <>
                      <Alert
                        message={
                          reconcileResult.isBalanced
                            ? 'Sổ cái cân bằng hoàn hảo (BALANCED)'
                            : 'Phát hiện chênh lệch số dư (DISCREPANCY)'
                        }
                        description={`Thời điểm kiểm tra: ${formatDate(reconcileResult.checkedAt)}`}
                        type={reconcileResult.isBalanced ? 'success' : 'error'}
                        showIcon
                        icon={
                          reconcileResult.isBalanced ? (
                            <CheckCircleOutlined />
                          ) : (
                            <CloseCircleOutlined />
                          )
                        }
                        style={{ marginBottom: 24 }}
                      />

                      <Row gutter={[16, 16]}>
                        <Col xs={24} sm={12} md={6}>
                          <Card bordered>
                            <Statistic
                              title="Tổng vế Ghi Nợ (Debit)"
                              value={reconcileResult.totalDebit}
                              formatter={(val) => (
                                <MoneyDisplay
                                  amount={Number(val)}
                                  currency="VND"
                                  colorType="expense"
                                  bold
                                />
                              )}
                            />
                          </Card>
                        </Col>

                        <Col xs={24} sm={12} md={6}>
                          <Card bordered>
                            <Statistic
                              title="Tổng vế Ghi Có (Credit)"
                              value={reconcileResult.totalCredit}
                              formatter={(val) => (
                                <MoneyDisplay
                                  amount={Number(val)}
                                  currency="VND"
                                  colorType="income"
                                  bold
                                />
                              )}
                            />
                          </Card>
                        </Col>

                        <Col xs={24} sm={12} md={6}>
                          <Card bordered>
                            <Statistic
                              title="Số tài khoản đã quét"
                              value={reconcileResult.totalAccountsChecked}
                              suffix="tài khoản"
                            />
                          </Card>
                        </Col>

                        <Col xs={24} sm={12} md={6}>
                          <Card bordered>
                            <Statistic
                              title="Tổng bút toán đã quét"
                              value={reconcileResult.totalEntriesChecked}
                              suffix="bút toán"
                            />
                          </Card>
                        </Col>
                      </Row>
                    </>
                  ) : (
                    <div style={{ textAlign: 'center', padding: '32px 0' }}>
                      <Text type="secondary" style={{ display: 'block', marginBottom: 16 }}>
                        Nhấn nút bên dưới để bắt đầu quét đối soát tính toàn vẹn và cân bằng tổng Debit = Credit trên toàn hệ thống.
                      </Text>
                      <Button
                        type="primary"
                        size="large"
                        icon={<SyncOutlined spin={reconcileMutation.isPending} />}
                        loading={reconcileMutation.isPending}
                        onClick={() => reconcileMutation.mutate()}
                      >
                        Bắt đầu đối soát ngay
                      </Button>
                    </div>
                  )}
                </Card>
              </div>
            ),
          },
        ]}
      />
    </PageContainer>
  )
}
