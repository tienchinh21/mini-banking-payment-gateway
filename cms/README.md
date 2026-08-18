# Mini Banking Admin CMS

Hệ thống quản trị (CMS/Backoffice) cho dự án **Mini Banking Ledger & Payment Gateway**, được xây dựng trên nền tảng **React 18/19 + TypeScript + Vite + Ant Design v5 + TanStack React Query + Axios**.

---

## 📁 Cấu trúc thư mục chuẩn (`src/`)

```text
cms/src/
├── api/                    # Cấu hình Axios client, interceptor, endpoints
│   ├── client.ts           # Axios instance (gắn JWT token, CorrelationId, xử lý lỗi tập trung)
│   └── endpoints.ts        # Định nghĩa danh sách API endpoints chuẩn
├── components/
│   ├── core/               # Các Core Components dùng chung nhiều lần
│   │   ├── AppTable/       # Core Table (Dynamic scroll Y theo màn hình, fixed cột thao tác, toolbar, pagination)
│   │   ├── AppBreadcrumb/  # Core Breadcrumb (tự động render theo route hoặc truyền custom items)
│   │   ├── PageContainer/  # Layout bao bọc page (Title, Breadcrumb, Actions, Card bọc)
│   │   ├── AppFilter/      # Thanh tìm kiếm & bộ lọc form chuẩn (Search, Reset, Expand)
│   │   ├── StatusTag/      # Tag hiển thị trạng thái chuẩn (Payment, Ledger, Account)
│   │   ├── MoneyDisplay/   # Định dạng tiền tệ VND/USD (+/- màu sắc theo thu/chi)
│   │   └── ActionMenu/     # Menu nút thao tác dạng popconfirm/dropdown
│   └── layout/             # Khung giao diện Admin chuẩn
│       ├── MainLayout.tsx  # Layout chính (Sider + Header + Content + Footer)
│       ├── AppHeader.tsx   # Header (User profile, collapse toggle, notifications)
│       ├── AppSider.tsx    # Sidebar menu với logo và phân cấp trang
│       └── AppFooter.tsx   # Footer thông tin hệ thống
├── config/
│   ├── theme.ts            # Ant Design theme tokens (ConfigProvider)
│   └── queryClient.ts      # Cấu hình TanStack React Query Client
├── constants/
│   ├── common.ts           # Storage keys, Pagination defaults, Date formats
│   └── status.ts           # Mappings màu sắc & nhãn cho trạng thái nghiệp vụ ngân hàng
├── hooks/
│   ├── useTable.ts         # Hook quản lý state Table (pagination, filters, sorter, query params sync)
│   ├── useDebounce.ts      # Hook debounce cho input search
│   └── useDynamicHeight.ts # Hook tự động tính toán chiều cao scroll Y cho Table theo viewport
├── modules/                # Các module nghiệp vụ mẫu hoàn chỉnh
│   ├── dashboard/          # Thống kê số dư, KPI, giao dịch gần nhất
│   ├── accounts/           # Quản lý tài khoản ví, khách hàng, nạp tiền (Top-up), khóa ví
│   ├── payments/           # Quản lý giao dịch Direct Debit, hoàn tiền (Refund)
│   ├── ledger/             # Sổ cái kép (Double-entry ledger journal)
│   ├── merchants/          # Quản lý đối tác Merchant, cấp lại API Key HMAC
│   ├── audit/              # Nhật ký kiểm toán & Trace Correlation ID
│   └── settings/           # Cấu hình hệ thống & tham số ngân hàng
├── routes/
│   ├── index.tsx           # React Router DOM (createBrowserRouter + Suspense + 404 page)
│   └── routes.config.tsx   # Danh sách routes và lazy loaded pages
├── types/
│   ├── api.ts              # ApiResponse<T>, PaginatedResult<T>, ApiError
│   └── common.ts           # TableParams, BreadcrumbRouteItem, StatusTagType
├── utils/
│   ├── format.ts           # formatMoney, formatDate, formatNumber, maskAccountNumber
│   ├── storage.ts          # Type-safe localStorage wrapper
│   └── helper.ts           # CleanParams, copyToClipboard, correlation ID generator
├── App.tsx                 # Root App bọc Antd ConfigProvider + Antd App + ReactQueryProvider
├── main.tsx                # Entry point
└── index.css               # Global reset & custom table scrollbar styles
```

---

## 🚀 Hướng dẫn sử dụng các Core Components

### 1. `AppTable` (Table tự động tính scroll và cố định cột thao tác)

```tsx
import { AppTable, type AppTableColumns, ActionMenu } from '@/components/core'
import { useTable } from '@/hooks/useTable'

const MyPage = () => {
  const { queryParams, pagination, handleTableChange } = useTable()

  const columns: AppTableColumns<ItemType> = [
    { title: 'Mã GD', dataIndex: 'id', key: 'id', width: 160 },
    { title: 'Tên', dataIndex: 'name', key: 'name', width: 200 },
    // ...
  ]

  return (
    <AppTable<ItemType>
      rowKey="id"
      columns={columns}
      dataSource={items}
      loading={isLoading}
      pagination={pagination}
      onChange={handleTableChange}
      // Tự động scroll Y theo màn hình:
      autoHeight={true}
      // Cố định cột thao tác bên phải:
      actionColumn={{
        title: 'Thao tác',
        width: 130,
        fixed: 'right',
        render: (_, record) => (
          <ActionMenu
            items={[
              { key: 'edit', label: 'Sửa', onClick: () => edit(record) },
              {
                key: 'delete',
                label: 'Xóa',
                danger: true,
                confirm: { title: 'Xác nhận xóa bản ghi này?' },
                onClick: () => remove(record.id),
              },
            ]}
          />
        ),
      }}
    />
  )
}
```

### 2. `PageContainer` (Khung bao bọc trang chuẩn)

```tsx
<PageContainer
  title="Quản lý Tài khoản & Ví"
  subTitle="Danh sách ví khách hàng và tra cứu số dư"
  breadcrumbs={[{ title: 'Tài khoản' }]}
  extra={<Button type="primary">Thêm mới</Button>}
>
  {/* Nội dung trang */}
</PageContainer>
```

### 3. `AppFilter` (Bộ lọc & tìm kiếm chuẩn)

```tsx
<AppFilter
  form={filterForm}
  onSearch={(values) => setFilters(values)}
  onReset={() => handleReset()}
>
  <Col xs={24} sm={12} md={8}>
    <Form.Item name="keyword" label="Tìm kiếm">
      <Input placeholder="Nhập từ khóa..." allowClear />
    </Form.Item>
  </Col>
  <Col xs={24} sm={12} md={6}>
    <Form.Item name="status" label="Trạng thái">
      <Select options={statusOptions} allowClear />
    </Form.Item>
  </Col>
</AppFilter>
```

---

## 💻 Chạy dự án ở local

```bash
cd cms
npm install
npm run dev
```

App sẽ chạy ở cổng `http://localhost:5172`.
