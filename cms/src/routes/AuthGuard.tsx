import React from 'react'
import { Navigate, useLocation, Outlet } from 'react-router-dom'
import { Spin } from 'antd'
import { useAuth } from '@/modules/auth'

/**
 * Protects routes requiring authentication.
 * Redirects unauthenticated users to /login preserving the requested path.
 */
export const AuthGuard: React.FC<{ children?: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div
        style={{
          height: '100vh',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 16,
          background: '#f8fafc',
        }}
      >
        <Spin size="large" />
        <span style={{ color: '#8c8c8c', fontSize: 14 }}>Đang kiểm tra phiên đăng nhập...</span>
      </div>
    )
  }

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location }} replace />
  }

  return children ? <>{children}</> : <Outlet />
}

/**
 * For guest-only routes like /login.
 * Redirects authenticated users to /dashboard.
 */
export const GuestGuard: React.FC<{ children?: React.ReactNode }> = ({ children }) => {
  const { isAuthenticated, isLoading } = useAuth()
  const location = useLocation()

  if (isLoading) {
    return (
      <div
        style={{
          height: '100vh',
          display: 'flex',
          flexDirection: 'column',
          alignItems: 'center',
          justifyContent: 'center',
          gap: 16,
          background: '#f8fafc',
        }}
      >
        <Spin size="large" />
        <span style={{ color: '#8c8c8c', fontSize: 14 }}>Đang khởi tạo ứng dụng...</span>
      </div>
    )
  }

  if (isAuthenticated) {
    const from = (location.state as any)?.from?.pathname || '/dashboard'
    return <Navigate to={from} replace />
  }

  return children ? <>{children}</> : <Outlet />
}

export default AuthGuard
