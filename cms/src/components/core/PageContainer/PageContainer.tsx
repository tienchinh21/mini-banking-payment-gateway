import React from 'react'
import { Card, Typography, Space, Spin } from 'antd'
import { AppBreadcrumb } from '../AppBreadcrumb/AppBreadcrumb'
import type { BreadcrumbRouteItem } from '@/types/common'

const { Title, Text } = Typography

export interface PageContainerProps {
  title?: React.ReactNode
  subTitle?: React.ReactNode
  breadcrumbs?: BreadcrumbRouteItem[] | false
  extra?: React.ReactNode
  children: React.ReactNode
  loading?: boolean
  contained?: boolean
  style?: React.CSSProperties
  className?: string
}

export const PageContainer: React.FC<PageContainerProps> = ({
  title,
  subTitle,
  breadcrumbs,
  extra,
  children,
  loading = false,
  contained = true,
  style,
  className,
}) => {
  return (
    <div
      className={className}
      style={{
        padding: '0 24px 24px 24px',
        minHeight: '100%',
        display: 'flex',
        flexDirection: 'column',
        ...style,
      }}
    >
      {breadcrumbs !== false && <AppBreadcrumb items={breadcrumbs} />}

      {(title || extra) && (
        <div
          style={{
            display: 'flex',
            alignItems: 'center',
            justifyContent: 'space-between',
            marginBottom: 16,
            flexWrap: 'wrap',
            gap: 12,
          }}
        >
          <div>
            {typeof title === 'string' ? (
              <Title level={4} style={{ margin: 0 }}>
                {title}
              </Title>
            ) : (
              title
            )}
            {subTitle && (
              <Text type="secondary" style={{ fontSize: 13, display: 'block', marginTop: 4 }}>
                {subTitle}
              </Text>
            )}
          </div>
          {extra && <Space size="middle">{extra}</Space>}
        </div>
      )}

      <Spin spinning={loading}>
        {contained ? (
          <Card
            bordered={false}
            style={{
              borderRadius: 8,
              boxShadow: '0 1px 3px 0 rgba(0, 0, 0, 0.05)',
            }}
            bodyStyle={{ padding: 20 }}
          >
            {children}
          </Card>
        ) : (
          children
        )}
      </Spin>
    </div>
  )
}
