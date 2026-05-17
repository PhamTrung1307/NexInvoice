import { Navigate, Route, Routes } from 'react-router-dom'
import { AppLayout } from './components/AppLayout.jsx'
import { RequireAuth } from './components/RequireAuth.jsx'
import { ClientFormPage } from './pages/ClientFormPage.jsx'
import { ClientsPage } from './pages/ClientsPage.jsx'
import { ContractsPage } from './pages/ContractsPage.jsx'
import { DashboardPage } from './pages/DashboardPage.jsx'
import { InvoiceDetailPage } from './pages/InvoiceDetailPage.jsx'
import { InvoicesPage } from './pages/InvoicesPage.jsx'
import { LoginPage } from './pages/LoginPage.jsx'
import { NotificationsPage } from './pages/NotificationsPage.jsx'
import { PaymentsPage } from './pages/PaymentsPage.jsx'
import { ProjectDetailPage } from './pages/ProjectDetailPage.jsx'
import { ProjectsPage } from './pages/ProjectsPage.jsx'
import { ReportsPage } from './pages/ReportsPage.jsx'
import { SettingsPage } from './pages/SettingsPage.jsx'
import { TasksPage } from './pages/TasksPage.jsx'

export default function App() {
  return (
    <Routes>
      <Route path="/login" element={<LoginPage />} />
      <Route
        element={
          <RequireAuth>
            <AppLayout />
          </RequireAuth>
        }
      >
        <Route index element={<Navigate to="/dashboard" replace />} />
        <Route path="/dashboard" element={<DashboardPage />} />
        <Route path="/clients" element={<ClientsPage />} />
        <Route path="/clients/new" element={<ClientFormPage />} />
        <Route path="/clients/:id/edit" element={<ClientFormPage />} />
        <Route path="/projects" element={<ProjectsPage />} />
        <Route path="/projects/:id" element={<ProjectDetailPage />} />
        <Route path="/tasks" element={<TasksPage />} />
        <Route path="/contracts" element={<ContractsPage />} />
        <Route path="/invoices" element={<InvoicesPage />} />
        <Route path="/invoices/:id" element={<InvoiceDetailPage />} />
        <Route path="/payments" element={<PaymentsPage />} />
        <Route path="/notifications" element={<NotificationsPage />} />
        <Route path="/reports" element={<ReportsPage />} />
        <Route path="/settings" element={<SettingsPage />} />
      </Route>
      <Route path="*" element={<Navigate to="/dashboard" replace />} />
    </Routes>
  )
}
