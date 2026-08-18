import React from 'react'
import { Breadcrumb } from 'antd'
import { Link, useLocation } from 'react-router-dom'
import { HomeOutlined } from '@ant-design/icons'
import type { BreadcrumbRouteItem } from '@/types/common'

export interface AppBreadcrumbProps {
  items?: BreadcrumbRouteItem[]
  homeHref?: string
  showHomeIcon?: boolean
}

export const AppBreadcrumb: React.FC<AppBreadcrumbProps> = ({
  items,
  homeHref = '/',
  showHomeIcon = true,
}) => {
  const location = useLocation()

  // Default auto-breadcrumb based on path if no explicit items passed
  const breadcrumbItems = React.useMemo(() => {
    if (items && items.length > 0) {
      return items.map((item, index) => {
        const isLast = index === items.length - 1
        return {
          title: item.path && !isLast ? (
            <Link to={item.path}>
              {item.icon && <span style={{ marginRight: 6 }}>{item.icon}</span>}
              {item.title}
            </Link>
          ) : (
            <span>
              {item.icon && <span style={{ marginRight: 6 }}>{item.icon}</span>}
              {item.title}
            </span>
          ),
        }
      })
    }

    // Auto generate from pathname
    const pathSnippets = location.pathname.split('/').filter((i) => i)
    const generated: { title: React.ReactNode }[] = []

    if (showHomeIcon) {
      generated.push({
        title: (
          <Link to={homeHref}>
            <HomeOutlined style={{ marginRight: 4 }} />
            Trang chủ
          </Link>
        ),
      })
    }

    pathSnippets.forEach((snippet, index) => {
      const url = `/${pathSnippets.slice(0, index + 1).join('/')}`
      const isLast = index === pathSnippets.length - 1
      const title = snippet.charAt(0).toUpperCase() + snippet.slice(1)

      generated.push({
        title: isLast ? <span>{title}</span> : <Link to={url}>{title}</Link>,
      })
    })

    return generated
  }, [items, location.pathname, homeHref, showHomeIcon])

  return (
    <Breadcrumb
      items={breadcrumbItems}
      style={{
        margin: '12px 0 16px 0',
      }}
    />
  )
}
