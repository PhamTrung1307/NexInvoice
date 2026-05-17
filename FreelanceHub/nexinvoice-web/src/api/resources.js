import { http, unwrap } from './http'

const API_V1 = '/api/v1'

const routes = {
  auth: `${API_V1}/Auth`,
  clients: `${API_V1}/Clients`,
  dashboard: `${API_V1}/Dashboard`,
  invoices: `${API_V1}/Invoices`,
  notifications: `${API_V1}/Notifications`,
  payments: `${API_V1}/payments`,
  contracts: `${API_V1}/contracts`,
  reports: `${API_V1}/reports`,
  settings: `${API_V1}/settings`,
  projects: `${API_V1}/Projects`,
  projectTasks: (projectId) => `${API_V1}/projects/${projectId}/tasks`,
  invoicePayments: (invoiceId) => `${API_V1}/invoices/${invoiceId}/payments`,
  tasks: `${API_V1}/Tasks`,
}

export const authApi = {
  login: (payload) => http.post(`${routes.auth}/login`, payload).then(unwrap),
  logout: (payload) => http.post(`${routes.auth}/logout`, payload).then(unwrap),
}

export const dashboardApi = {
  summary: () => http.get(`${routes.dashboard}/summary`).then(unwrap),
}

export const clientsApi = {
  list: (params) => http.get(routes.clients, { params }).then(unwrap),
  detail: (id) => http.get(`${routes.clients}/${id}`).then(unwrap),
  create: (payload) => http.post(routes.clients, payload).then(unwrap),
  update: (id, payload) => http.put(`${routes.clients}/${id}`, payload).then(unwrap),
  remove: (id) => http.delete(`${routes.clients}/${id}`).then(unwrap),
}

export const projectsApi = {
  list: (params) => http.get(routes.projects, { params }).then(unwrap),
  detail: (id) => http.get(`${routes.projects}/${id}`).then(unwrap),
  status: (id, status) => http.patch(`${routes.projects}/${id}/status`, { status }).then(unwrap),
}

export const tasksApi = {
  byProject: (projectId) => http.get(routes.projectTasks(projectId)).then(unwrap),
  detail: (id) => http.get(`${routes.tasks}/${id}`).then(unwrap),
  status: (id, status) => http.patch(`${routes.tasks}/${id}/status`, { status }).then(unwrap),
  priority: (id, priority) => http.patch(`${routes.tasks}/${id}/priority`, { priority }).then(unwrap),
}

export const invoicesApi = {
  list: (params) => http.get(routes.invoices, { params }).then(unwrap),
  detail: (id) => http.get(`${routes.invoices}/${id}`).then(unwrap),
  send: (id) => http.patch(`${routes.invoices}/${id}/send`).then(unwrap),
  cancel: (id) => http.patch(`${routes.invoices}/${id}/cancel`).then(unwrap),
  markPaid: (id) => http.patch(`${routes.invoices}/${id}/mark-paid`).then(unwrap),
  pdfUrl: (id) => `${http.defaults.baseURL}${routes.invoices}/${id}/pdf`,
}

export const paymentsApi = {
  create: (payload) => http.post(routes.payments, payload).then(unwrap),
  byInvoice: (invoiceId) => http.get(routes.invoicePayments(invoiceId)).then(unwrap),
  confirm: (id) => http.patch(`${routes.payments}/${id}/confirm`).then(unwrap),
  reject: (id, payload) => http.patch(`${routes.payments}/${id}/reject`, payload).then(unwrap),
}

export const contractsApi = {
  list: (params) => http.get(routes.contracts, { params }).then(unwrap),
  detail: (id) => http.get(`${routes.contracts}/${id}`).then(unwrap),
  create: (payload) => http.post(routes.contracts, payload).then(unwrap),
  update: (id, payload) => http.put(`${routes.contracts}/${id}`, payload).then(unwrap),
  remove: (id) => http.delete(`${routes.contracts}/${id}`).then(unwrap),
  upload: (id, file) => {
    const formData = new FormData()
    formData.append('file', file)
    return http.post(`${routes.contracts}/${id}/upload`, formData).then(unwrap)
  },
  downloadUrl: (id) => `${http.defaults.baseURL}${routes.contracts}/${id}/download`,
  approve: (id) => http.patch(`${routes.contracts}/${id}/approve`).then(unwrap),
  reject: (id, payload) => http.patch(`${routes.contracts}/${id}/reject`, payload).then(unwrap),
}

export const reportsApi = {
  revenue: (params) => http.get(`${routes.reports}/revenue`, { params }).then(unwrap),
  invoiceStatus: (params) => http.get(`${routes.reports}/invoice-status`, { params }).then(unwrap),
  projectProgress: (params) => http.get(`${routes.reports}/project-progress`, { params }).then(unwrap),
  customerRevenue: (params) => http.get(`${routes.reports}/customer-revenue`, { params }).then(unwrap),
}

export const settingsApi = {
  company: () => http.get(`${routes.settings}/company`).then(unwrap),
  updateCompany: (payload) => http.put(`${routes.settings}/company`, payload).then(unwrap),
  preferences: () => http.get(`${routes.settings}/preferences`).then(unwrap),
  updatePreferences: (payload) => http.put(`${routes.settings}/preferences`, payload).then(unwrap),
}

export const notificationsApi = {
  list: () => http.get(routes.notifications).then(unwrap),
  read: (id) => http.patch(`${routes.notifications}/${id}/read`).then(unwrap),
  readAll: () => http.patch(`${routes.notifications}/read-all`).then(unwrap),
}
