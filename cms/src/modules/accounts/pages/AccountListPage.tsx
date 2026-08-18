import React, { useState } from 'react'
import { Form, Input, Select, Button, Modal, InputNumber, message, Typography, Col } from 'antd'
import { PlusOutlined, LockOutlined, UnlockOutlined, EyeOutlined } from '@ant-design/icons'
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
import type { WalletAccountItem, TopUpFormData } from '../types'

const { Text } = Typography

export const AccountListPage: React.FC = () => {
  const queryClient = useQueryClient()
  const [filterForm] = Form.useForm()
  const [topUpModalVisible, setTopUpModalVisible] = useState(false)
  const [topUpForm] = Form.useForm()

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

  // Fetch accounts query
  const { data, isLoading, refetch } = useQuery({
    queryKey: ['accounts-list', queryParams],
    queryFn: async () => {
      const res = await accountService.getAccounts(queryParams)
      setTotal(res.meta.totalItems)
      return res
    },
  })

  // Mutation for Top-up
  const topUpMutation = useMutation({
    mutationFn: (values: TopUpFormData) => accountService.topUp(values),
    onSuccess: () => {
      message.success('Nạp tiền vào ví thành công!')
      setTopUpModalVisible(false)
      topUpForm.resetFields()
      queryClient.invalidateQueries({ queryKey: ['accounts-list'] })
    },
  })

  // Mutation for Freeze / Unfreeze
  const freezeMutation = useMutation({
    mutationFn: ({ id, status }: { id: string; status: 'ACTIVE' | 'FROZEN' }) =>
      accountService.toggleFreeze(id, status),
    onSuccess: () => {
      message.success('Cập nhật trạng thái ví thành công!')
      queryClient.invalidateQueries({ queryKey: ['accounts-list'] })
    },
  })

  const columns: AppTableColumns<WalletAccountItem> = [
    {
      title: 'Số tài khoản ví',
      dataIndex: 'accountNumber',
      key: 'accountNumber',
      width: 170,
      render: (accNo) => <Text copyable strong>{accNo}</Text>,
    },
    {
      title: 'Chủ tài khoản',
      dataIndex: 'customerName',
      key: 'customerName',
      width: 180,
    },
    {
      title: 'Email',
      dataIndex: 'email',
      key: 'email',
      width: 200,
    },
    {
      title: 'Số điện thoại',
      dataIndex: 'phone',
      key: 'phone',
      width: 140,
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
        <MoneyDisplay amount={val} currency={record.currency} />
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

  const handleSearchSubmit = (values: any) => {
    setKeyword(values.keyword || '')
    setFilters({
      status: values.status,
    })
  }

  const handleFilterReset = () => {
    filterForm.resetFields()
    handleReset()
  }

  return (
    <PageContainer
      title="Quản lý Tài khoản & Ví"
      subTitle="Danh sách ví khách hàng, tra cứu số dư và nạp tiền tài khoản"
      extra={
        <Button
          type="primary"
          icon={<PlusOutlined />}
          onClick={() => setTopUpModalVisible(true)}
        >
          Nạp tiền ví (Top-up)
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
            <Input placeholder="Số ví, tên khách hàng, email, SĐT..." allowClear />
          </Form.Item>
        </Col>

        <Col xs={24} sm={12} md={6}>
          <Form.Item name="status" label="Trạng thái" style={{ marginBottom: 0 }}>
            <Select
              placeholder="Tất cả trạng thái"
              allowClear
              options={[
                { label: 'Hoạt động', value: 'ACTIVE' },
                { label: 'Tạm khóa', value: 'FROZEN' },
                { label: 'Đã đóng', value: 'CLOSED' },
              ]}
            />
          </Form.Item>
        </Col>
      </AppFilter>

      {/* Core Table */}
      <AppTable<WalletAccountItem>
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
                  label: 'Xem chi tiết',
                  icon: <EyeOutlined />,
                  onClick: () => message.info(`Xem chi tiết tài khoản ${record.accountNumber}`),
                },
                {
                  key: 'freeze',
                  label: record.status === 'ACTIVE' ? 'Khóa ví' : 'Mở khóa',
                  danger: record.status === 'ACTIVE',
                  icon: record.status === 'ACTIVE' ? <LockOutlined /> : <UnlockOutlined />,
                  confirm: {
                    title: `Xác nhận ${record.status === 'ACTIVE' ? 'khóa' : 'mở khóa'} ví?`,
                    description: `Tài khoản: ${record.accountNumber}`,
                  },
                  onClick: () =>
                    freezeMutation.mutate({
                      id: record.id,
                      status: record.status === 'ACTIVE' ? 'FROZEN' : 'ACTIVE',
                    }),
                },
              ]}
            />
          ),
        }}
      />

      {/* Top-up Modal */}
      <Modal
        title="Nạp tiền vào ví tài khoản"
        open={topUpModalVisible}
        onCancel={() => setTopUpModalVisible(false)}
        onOk={() => topUpForm.submit()}
        confirmLoading={topUpMutation.isPending}
        destroyOnClose
      >
        <Form
          form={topUpForm}
          layout="vertical"
          onFinish={(values) => topUpMutation.mutate(values)}
          initialValues={{ amount: 500000, description: 'Nạp tiền tài khoản ví demo' }}
        >
          <Form.Item
            name="accountNumber"
            label="Số tài khoản ví"
            rules={[{ required: true, message: 'Vui lòng nhập số tài khoản ví' }]}
          >
            <Input placeholder="Ví dụ: WA-8801928371" />
          </Form.Item>

          <Form.Item
            name="amount"
            label="Số tiền nạp (VND)"
            rules={[
              { required: true, message: 'Vui lòng nhập số tiền' },
              { type: 'number', min: 10000, message: 'Số tiền tối thiểu là 10,000 VND' },
            ]}
          >
            <InputNumber
              style={{ width: '100%' }}
              formatter={(value) => `${value}`.replace(/\B(?=(\d{3})+(?!\d))/g, ',')}
              parser={(value) => (value ? Number(value.replace(/\$\s?|(,*)/g, '')) : 0)}
              placeholder="Nhập số tiền"
            />
          </Form.Item>

          <Form.Item name="description" label="Ghi chú">
            <Input.TextArea rows={3} placeholder="Nội dung nạp tiền" />
          </Form.Item>
        </Form>
      </Modal>
    </PageContainer>
  )
}
