import type { ThemeConfig } from 'antd'

export const lightTheme: ThemeConfig = {
  token: {
    colorPrimary: '#1677ff',
    colorSuccess: '#52c41a',
    colorWarning: '#faad14',
    colorError: '#ff4d4f',
    colorInfo: '#1677ff',
    borderRadius: 6,
    wireframe: false,
    fontFamily:
      "-apple-system, BlinkMacSystemFont, 'Segoe UI', Roboto, 'Helvetica Neue', Arial, 'Noto Sans', sans-serif",
    fontSize: 14,
    colorBgContainer: '#ffffff',
    colorBgLayout: '#f5f7fa',
  },
  components: {
    Layout: {
      headerBg: '#ffffff',
      headerHeight: 64,
      headerPadding: '0 24px',
      siderBg: '#001529',
      bodyBg: '#f5f7fa',
    },
    Card: {
      paddingLG: 20,
      borderRadiusLG: 8,
      boxShadowTertiary: '0 1px 2px 0 rgba(0, 0, 0, 0.03), 0 1px 6px -1px rgba(0, 0, 0, 0.02), 0 2px 4px 0 rgba(0, 0, 0, 0.02)',
    },
    Table: {
      headerBg: '#fafafa',
      headerColor: '#1f1f1f',
      headerSplitColor: '#f0f0f0',
      rowHoverBg: '#f6faff',
      borderRadiusLG: 8,
    },
    Button: {
      borderRadius: 6,
      controlHeight: 34,
    },
    Input: {
      controlHeight: 34,
      borderRadius: 6,
    },
    Select: {
      controlHeight: 34,
      borderRadius: 6,
    },
    Breadcrumb: {
      fontSize: 13,
      separatorMargin: 8,
    },
    Modal: {
      borderRadiusLG: 10,
    },
  },
}
