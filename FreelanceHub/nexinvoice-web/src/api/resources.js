import { http, unwrap } from './http'

const routes = {
  auth: '/auth',
  clients: '/clients',
  dashboard: '/dashboard',
  invoices: '/invoices',
  notifications: '/notifications',
  payments: '/payments',
  contracts: '/contracts',
  reports: '/reports',
  settings: '/settings',
  projects: '/projects',
  projectTasks: (projectId) => `/projects/${projectId}/tasks`,
  invoicePayments: (invoiceId) => `/invoices/${invoiceId}/payments`,
  tasks: '/tasks',
}

export const authApi = {
  login: (payload) => http.post(`${routes.auth}/login`, payload).then(unwrap),
  refresh: (payload) => http.post(`${routes.auth}/refresh-token`, payload).then(unwrap),
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
  create: (payload) => http.post(routes.projects, payload).then(unwrap),
  update: (id, payload) => http.put(`${routes.projects}/${id}`, payload).then(unwrap),
  remove: (id) => http.delete(`${routes.projects}/${id}`).then(unwrap),
  status: (id, status) => http.patch(`${routes.projects}/${id}/status`, { status }).then(unwrap),
}

export const tasksApi = {
  byProject: (projectId) => http.get(routes.projectTasks(projectId)).then(unwrap),
  create: (projectId, payload) => http.post(routes.projectTasks(projectId), payload).then(unwrap),
  detail: (id) => http.get(`${routes.tasks}/${id}`).then(unwrap),
  update: (id, payload) => http.put(`${routes.tasks}/${id}`, payload).then(unwrap),
  remove: (id) => http.delete(`${routes.tasks}/${id}`).then(unwrap),
  status: (id, status) => http.patch(`${routes.tasks}/${id}/status`, { status }).then(unwrap),
  priority: (id, priority) => http.patch(`${routes.tasks}/${id}/priority`, { priority }).then(unwrap),
  uploadAttachment: (id, file) => {
    const formData = new FormData()
    formData.append('file', file)
    return http.post(`${routes.tasks}/${id}/attachments`, formData).then(unwrap)
  },
}

export const invoicesApi = {
  list: (params) => http.get(routes.invoices, { params }).then(unwrap),
  detail: (id) => http.get(`${routes.invoices}/${id}`).then(unwrap),
  create: (payload) => http.post(routes.invoices, payload).then(unwrap),
  update: (id, payload) => http.put(`${routes.invoices}/${id}`, payload).then(unwrap),
  send: (id) => http.patch(`${routes.invoices}/${id}/send`).then(unwrap),
  cancel: (id) => http.patch(`${routes.invoices}/${id}/cancel`).then(unwrap),
  markPaid: (id) => http.patch(`${routes.invoices}/${id}/mark-paid`).then(unwrap),
  pdfUrl: (id) => buildAbsoluteUrl(`${routes.invoices}/${id}/pdf`),
}

export const paymentsApi = {
  create: (payload) => http.post(routes.payments, payload).then(unwrap),
  byInvoice: (invoiceId) => http.get(routes.invoicePayments(invoiceId)).then(unwrap),
  uploadProof: (id, file) => {
    const formData = new FormData()
    formData.append('file', file)
    return http.post(`${routes.payments}/${id}/proof`, formData).then(unwrap)
  },
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
  downloadUrl: (id) => buildAbsoluteUrl(`${routes.contracts}/${id}/download`),
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

function buildAbsoluteUrl(path) {
  const base = String(http.defaults.baseURL ?? '').replace(/\/$/, '')
  return `${base}${path}`
}
