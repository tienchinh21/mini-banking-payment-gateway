import React from 'react'
import { Card, Form, Input, Button, Switch, Typography, message, Row, Col } from 'antd'
import { SaveOutlined } from '@ant-design/icons'
import { PageContainer } from '@/components/core'
import { APP_CONFIG } from '@/constants/common'

const { Text } = Typography

export const SettingsPage: React.FC = () => {
  const [form] = Form.useForm()

  const handleSave = () => {
    message.success('Lưu cấu hình hệ thống thành công!')
  }

  return (
    <PageContainer
      title="Cấu hình hệ thống"
      subTitle="Thiết lập tham số thanh toán, webhook retry và bảo mật"
    >
      <Row gutter={[24, 24]}>
        <Col xs={24} lg={16}>
          <Form
            form={form}
            layout="vertical"
            initialValues={{
              systemName: APP_CONFIG.NAME,
              defaultCurrency: 'VND',
              maxDebitPerTxn: 50000000,
              idempotencyTtlSeconds: 86400,
              enableHmacValidation: true,
              enableRateLimiter: true,
              webhookMaxRetries: 5,
              outboxBatchSize: 50,
            }}
            onFinish={handleSave}
          >
            <Card title="Tham số thanh toán & Kế toán" style={{ marginBottom: 20 }}>
              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item name="defaultCurrency" label="Đơn vị tiền tệ mặc định">
                    <Input disabled />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    name="maxDebitPerTxn"
                    label="Hạn mức trừ tiền tối đa / giao dịch (VND)"
                    rules={[{ required: true }]}
                  >
                    <Input type="number" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item
                    name="idempotencyTtlSeconds"
                    label="Thời gian lưu trữ Idempotency Key (giây)"
                  >
                    <Input type="number" />
                  </Form.Item>
                </Col>
              </Row>
            </Card>

            <Card title="Bảo mật & Message Broker" style={{ marginBottom: 20 }}>
              <Form.Item
                name="enableHmacValidation"
                label="Bắt buộc xác thực chữ ký HMAC cho Merchant API"
                valuePropName="checked"
              >
                <Switch />
              </Form.Item>

              <Form.Item
                name="enableRateLimiter"
                label="Kích hoạt Redis Rate Limiting & Nonce Protection"
                valuePropName="checked"
              >
                <Switch />
              </Form.Item>

              <Row gutter={16}>
                <Col xs={24} md={12}>
                  <Form.Item name="webhookMaxRetries" label="Số lần retry tối đa cho Webhook">
                    <Input type="number" />
                  </Form.Item>
                </Col>
                <Col xs={24} md={12}>
                  <Form.Item name="outboxBatchSize" label="Outbox Dispatcher Batch Size">
                    <Input type="number" />
                  </Form.Item>
                </Col>
              </Row>
            </Card>

            <Button type="primary" htmlType="submit" icon={<SaveOutlined />} size="large">
              Lưu cấu hình
            </Button>
          </Form>
        </Col>

        <Col xs={24} lg={8}>
          <Card title="Thông tin môi trường">
            <p>
              <Text strong>Phiên bản CMS: </Text>
              <Text>{APP_CONFIG.SYSTEM_VERSION}</Text>
            </p>
            <p>
              <Text strong>Backend API: </Text>
              <Text code>.NET 8 ASP.NET Core</Text>
            </p>
            <p>
              <Text strong>Database: </Text>
              <Text code>PostgreSQL 16</Text>
            </p>
            <p>
              <Text strong>Cache / Lock: </Text>
              <Text code>Redis 7</Text>
            </p>
            <p>
              <Text strong>Message Broker: </Text>
              <Text code>RabbitMQ 3</Text>
            </p>
          </Card>
        </Col>
      </Row>
    </PageContainer>
  )
}
