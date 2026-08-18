import React from 'react'
import { Layout, Typography } from 'antd'
import { APP_CONFIG } from '@/constants/common'

const { Footer } = Layout
const { Text } = Typography

export const AppFooter: React.FC = () => {
  return (
    <Footer
      style={{
        textAlign: 'center',
        padding: '16px 24px',
        background: 'transparent',
        fontSize: 13,
      }}
    >
      <Text type="secondary">
        {APP_CONFIG.NAME} v{APP_CONFIG.SYSTEM_VERSION} © {new Date().getFullYear()} Mini Banking Ledger & Payment Gateway
      </Text>
    </Footer>
  )
}
