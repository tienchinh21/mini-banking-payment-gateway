import React from 'react'
import { ConfigProvider, App as AntdApp } from 'antd'
import viVN from 'antd/locale/vi_VN'
import { QueryClientProvider } from '@tanstack/react-query'
import { queryClient } from '@/config/queryClient'
import { lightTheme } from '@/config/theme'
import { AuthProvider } from '@/modules/auth/context/AuthContext'
import { AppRouter } from '@/routes'

export const App: React.FC = () => {
  return (
    <QueryClientProvider client={queryClient}>
      <ConfigProvider theme={lightTheme} locale={viVN}>
        <AntdApp>
          <AuthProvider>
            <AppRouter />
          </AuthProvider>
        </AntdApp>
      </ConfigProvider>
    </QueryClientProvider>
  )
}

export default App
