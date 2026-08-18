import React, { useState } from 'react'
import { Form, Row, Col, Button, Space, theme } from 'antd'
import { SearchOutlined, ReloadOutlined, DownOutlined, UpOutlined } from '@ant-design/icons'
import type { FormInstance } from 'antd'

export interface AppFilterProps {
  form?: FormInstance
  onSearch: (values: any) => void
  onReset?: () => void
  children: React.ReactNode
  initialValues?: Record<string, any>
  showExpand?: boolean
  defaultExpanded?: boolean
  searchButtonText?: string
  resetButtonText?: string
  extraActions?: React.ReactNode
}

export const AppFilter: React.FC<AppFilterProps> = ({
  form: propForm,
  onSearch,
  onReset,
  children,
  initialValues,
  showExpand = false,
  defaultExpanded = false,
  searchButtonText = 'Tìm kiếm',
  resetButtonText = 'Đặt lại',
  extraActions,
}) => {
  const [internalForm] = Form.useForm()
  const form = propForm || internalForm
  const [expanded, setExpanded] = useState<boolean>(defaultExpanded)
  const { token } = theme.useToken()

  const handleFinish = (values: any) => {
    onSearch(values)
  }

  const handleReset = () => {
    form.resetFields()
    if (onReset) {
      onReset()
    } else {
      onSearch(form.getFieldsValue())
    }
  }

  return (
    <div
      style={{
        background: token.colorBgContainer,
        borderRadius: 8,
        padding: '16px 20px',
        marginBottom: 16,
        border: `1px solid ${token.colorBorderSecondary}`,
      }}
    >
      <Form
        form={form}
        layout="horizontal"
        initialValues={initialValues}
        onFinish={handleFinish}
      >
        <Row gutter={[16, 12]} align="middle">
          {children}

          <Col flex="auto" style={{ textAlign: 'right' }}>
            <Space size="small" wrap>
              <Button type="primary" htmlType="submit" icon={<SearchOutlined />}>
                {searchButtonText}
              </Button>

              <Button icon={<ReloadOutlined />} onClick={handleReset}>
                {resetButtonText}
              </Button>

              {showExpand && (
                <Button
                  type="link"
                  onClick={() => setExpanded(!expanded)}
                  style={{ fontSize: 13, padding: 0 }}
                >
                  {expanded ? 'Thu gọn' : 'Mở rộng'}{' '}
                  {expanded ? <UpOutlined /> : <DownOutlined />}
                </Button>
              )}

              {extraActions}
            </Space>
          </Col>
        </Row>
      </Form>
    </div>
  )
}
