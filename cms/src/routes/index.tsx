import React, { Suspense } from 'react'
import { createBrowserRouter, RouterProvider, Link } from 'react-router-dom'
import { Spin, Result, Button } from 'antd'
import { routesConfig } from './routes.config'

const PageLoading: React.FC = () => (
  <div
    style={{
      height: '60vh',
      display: 'flex',
      alignItems: 'center',
      justifyContent: 'center',
    }}
  >
    <Spin size="large" tip="Đang tải trang..." />
  </div>
)

const NotFoundPage: React.FC = () => (
  <Result
    status="404"
    title="404"
    subTitle="Xin lỗi, trang bạn truy cập không tồn tại hoặc đã bị xóa."
    extra={
      <Link to="/dashboard">
        <Button type="primary">Trở về Trang chủ</Button>
      </Link>
    }
  />
)

const router = createBrowserRouter([
  ...routesConfig,
  {
    path: '*',
    element: <NotFoundPage />,
  },
])

export const AppRouter: React.FC = () => {
  return (
    <Suspense fallback={<PageLoading />}>
      <RouterProvider router={router} />
    </Suspense>
  )
}
