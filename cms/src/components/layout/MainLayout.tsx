import React, { useState } from 'react'
import { Layout } from 'antd'
import { Outlet } from 'react-router-dom'
import { AppHeader } from './AppHeader'
import { AppSider } from './AppSider'
import { AppFooter } from './AppFooter'
import { STORAGE_KEYS } from '@/constants/common'
import { storage } from '@/utils/storage'

const { Content } = Layout

export const MainLayout: React.FC = () => {
  const [collapsed, setCollapsed] = useState<boolean>(() => {
    return storage.get<boolean>(STORAGE_KEYS.SIDEBAR_COLLAPSED) ?? false
  })

  const handleToggleCollapse = () => {
    setCollapsed((prev) => {
      const next = !prev
      storage.set(STORAGE_KEYS.SIDEBAR_COLLAPSED, next)
      return next
    })
  }

  return (
    <Layout style={{ minHeight: '100vh' }}>
      <AppSider collapsed={collapsed} onCollapse={setCollapsed} />
      <Layout style={{ display: 'flex', flexDirection: 'column' }}>
        <AppHeader
          collapsed={collapsed}
          onToggleCollapse={handleToggleCollapse}
        />
        <Content
          style={{
            flex: 1,
            display: 'flex',
            flexDirection: 'column',
          }}
        >
          <Outlet />
        </Content>
        <AppFooter />
      </Layout>
    </Layout>
  )
}
