import { lazy } from 'react'
import { Navigate, type RouteObject } from 'react-router-dom'
import { MainLayout } from '@/components/layout/MainLayout'

const DashboardPage = lazy(() =>
  import('@/modules/dashboard/pages/DashboardPage').then((m) => ({ default: m.DashboardPage }))
)
const AccountListPage = lazy(() =>
  import('@/modules/accounts/pages/AccountListPage').then((m) => ({ default: m.AccountListPage }))
)
const PaymentListPage = lazy(() =>
  import('@/modules/payments/pages/PaymentListPage').then((m) => ({ default: m.PaymentListPage }))
)
const LedgerListPage = lazy(() =>
  import('@/modules/ledger/pages/LedgerListPage').then((m) => ({ default: m.LedgerListPage }))
)
const MerchantListPage = lazy(() =>
  import('@/modules/merchants/pages/MerchantListPage').then((m) => ({ default: m.MerchantListPage }))
)
const AuditLogPage = lazy(() =>
  import('@/modules/audit/pages/AuditLogPage').then((m) => ({ default: m.AuditLogPage }))
)
const SettingsPage = lazy(() =>
  import('@/modules/settings/pages/SettingsPage').then((m) => ({ default: m.SettingsPage }))
)

export const routesConfig: RouteObject[] = [
  {
    path: '/',
    element: <MainLayout />,
    children: [
      {
        index: true,
        element: <Navigate to="/dashboard" replace />,
      },
      {
        path: 'dashboard',
        element: <DashboardPage />,
      },
      {
        path: 'accounts',
        element: <AccountListPage />,
      },
      {
        path: 'payments',
        element: <PaymentListPage />,
      },
      {
        path: 'ledger',
        element: <LedgerListPage />,
      },
      {
        path: 'merchants',
        element: <MerchantListPage />,
      },
      {
        path: 'audit-logs',
        element: <AuditLogPage />,
      },
      {
        path: 'settings',
        element: <SettingsPage />,
      },
    ],
  },
]
