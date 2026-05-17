import { useMemo, useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { reportsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCompactCurrency, formatCurrency } from '../utils/format'

const pageSizeOptions = [10, 20, 50]

export function ReportsPage() {
  const [filters, setFilters] = useState({ fromDate: '', toDate: '' })
  const [monthlyPage, setMonthlyPage] = useState(1)
  const [monthlyPageSize, setMonthlyPageSize] = useState(10)
  const [projectPage, setProjectPage] = useState(1)
  const [projectPageSize, setProjectPageSize] = useState(10)
  const [customerPage, setCustomerPage] = useState(1)
  const [customerPageSize, setCustomerPageSize] = useState(10)
  const params = useMemo(() => compact(filters), [filters])

  const revenueQuery = useQuery({ queryKey: ['reports', 'revenue', params], queryFn: () => reportsApi.revenue(params) })
  const invoiceQuery = useQuery({ queryKey: ['reports', 'invoice-status', params], queryFn: () => reportsApi.invoiceStatus(params) })
  const projectQuery = useQuery({ queryKey: ['reports', 'project-progress', params], queryFn: () => reportsApi.projectProgress(params) })
  const customerQuery = useQuery({ queryKey: ['reports', 'customer-revenue', params], queryFn: () => reportsApi.customerRevenue(params) })

  if (revenueQuery.isLoading) return <LoadingState />
  if (revenueQuery.isError) return <ErrorState message={getErrorMessage(revenueQuery.error)} />

  const revenue = revenueQuery.data
  const invoiceItems = invoiceQuery.data?.items ?? []
  const projects = projectQuery.data?.projects ?? []
  const customers = (customerQuery.data?.topCustomers ?? []).slice(0, 10)
  const monthlyRows = revenue.monthlyRevenue ?? []

  const monthly = paginate(monthlyRows, monthlyPage, monthlyPageSize)
  const projectRows = paginate(projects, projectPage, projectPageSize)
  const customerRows = paginate(customers, customerPage, customerPageSize)

  return (
    <section className="reports-page">
      <PageHeader title="Báo cáo" description="Theo dõi doanh thu, hóa đơn và tiến độ dự án" />

      <div className="report-filter-card">
        <div>
          <h3>Bộ lọc báo cáo</h3>
          <p>Chọn khoảng thời gian để xem số liệu kinh doanh</p>
        </div>
        <div className="report-filter-grid">
          <label className="field">
            <span>Từ ngày</span>
            <input type="date" value={filters.fromDate} onChange={(e) => setFilters({ ...filters, fromDate: e.target.value })} />
          </label>
          <label className="field">
            <span>Đến ngày</span>
            <input type="date" value={filters.toDate} onChange={(e) => setFilters({ ...filters, toDate: e.target.value })} />
          </label>
        </div>
      </div>

      <div className="report-stat-grid">
        <StatCard label="Tổng doanh thu" value={revenue.totalRevenue} />
        <StatCard label="Doanh thu đã thanh toán" value={revenue.paidRevenue} />
        <StatCard label="Doanh thu chờ thanh toán" value={revenue.pendingRevenue} />
        <StatCard label="Hóa đơn quá hạn" value={revenue.overdueInvoiceCount} isNumber />
      </div>

      <div className="reports-grid">
        <section className="dashboard-panel report-panel wide">
          <PanelTitle title="Doanh thu theo tháng" description="Dữ liệu phục vụ biểu đồ doanh thu" />
          {monthlyRows.length ? (
            <>
              <ReportTable>
                <thead><tr><th>Tháng</th><th className="text-right">Doanh thu</th></tr></thead>
                <tbody>{monthly.items.map((item) => <tr key={item.month}><td>{item.month}</td><td className="text-right" title={formatCurrency(item.revenue)}>{formatCompactCurrency(item.revenue)}</td></tr>)}</tbody>
              </ReportTable>
              <Pagination page={monthly.page} totalPages={monthly.totalPages} pageSize={monthlyPageSize} onPageChange={setMonthlyPage} onPageSizeChange={(value) => { setMonthlyPageSize(value); setMonthlyPage(1) }} />
            </>
          ) : <EmptyState text="Chưa có dữ liệu doanh thu." />}
        </section>

        <section className="dashboard-panel report-panel">
          <PanelTitle title="Trạng thái hóa đơn" description="Tổng hợp số lượng và giá trị" />
          {invoiceQuery.isLoading ? <LoadingState /> : invoiceItems.length ? (
            <div className="report-status-list">
              {invoiceItems.map((item) => (
                <div className="report-status-row" key={item.status}>
                  <div><StatusBadge type="invoice" status={item.status} /><span>{item.count} hóa đơn</span></div>
                  <strong title={formatCurrency(item.amount)}>{formatCompactCurrency(item.amount)}</strong>
                </div>
              ))}
            </div>
          ) : <EmptyState text="Chưa có dữ liệu hóa đơn." />}
        </section>
      </div>

      <div className="reports-grid">
        <section className="dashboard-panel report-panel wide">
          <PanelTitle title="Tiến độ dự án" description="Các dự án gần đây và mức hoàn thành" />
          {projectQuery.isLoading ? <LoadingState /> : projects.length ? (
            <>
              <ReportTable>
                <thead><tr><th>Dự án</th><th>Trạng thái</th><th>Tiến độ</th><th>Công việc</th></tr></thead>
                <tbody>{projectRows.items.map((project) => <tr key={project.projectId}><td><strong className="table-primary">{project.projectName}</strong></td><td><StatusBadge type="project" status={project.status} /></td><td>{project.progressPercentage}%</td><td>{project.completedTasks}/{project.totalTasks}</td></tr>)}</tbody>
              </ReportTable>
              <Pagination page={projectRows.page} totalPages={projectRows.totalPages} pageSize={projectPageSize} onPageChange={setProjectPage} onPageSizeChange={(value) => { setProjectPageSize(value); setProjectPage(1) }} />
            </>
          ) : <EmptyState text="Chưa có dữ liệu dự án." />}
        </section>

        <section className="dashboard-panel report-panel">
          <PanelTitle title="Khách hàng theo doanh thu" description="Top 10 khách hàng đã thanh toán" />
          {customerQuery.isLoading ? <LoadingState /> : customers.length ? (
            <>
              <ReportTable compact>
                <thead><tr><th>Khách hàng</th><th className="text-right">Doanh thu</th><th className="text-right">Hóa đơn</th></tr></thead>
                <tbody>{customerRows.items.map((customer) => <tr key={customer.customerId}><td><strong className="table-primary">{customer.customerName}</strong></td><td className="text-right" title={formatCurrency(customer.revenue)}>{formatCompactCurrency(customer.revenue)}</td><td className="text-right">{customer.invoiceCount}</td></tr>)}</tbody>
              </ReportTable>
              <Pagination page={customerRows.page} totalPages={customerRows.totalPages} pageSize={customerPageSize} onPageChange={setCustomerPage} onPageSizeChange={(value) => { setCustomerPageSize(value); setCustomerPage(1) }} />
            </>
          ) : <EmptyState text="Chưa có dữ liệu khách hàng." />}
        </section>
      </div>
    </section>
  )
}

function StatCard({ label, value, isNumber = false }) {
  const fullValue = isNumber ? String(value ?? 0) : formatCurrency(value)
  const displayValue = isNumber ? String(value ?? 0) : formatCompactCurrency(value)

  return (
    <article className="dashboard-stat-card report-stat-card" title={fullValue}>
      <div className="stat-card-top">{label}</div>
      <strong>{displayValue}</strong>
    </article>
  )
}

function PanelTitle({ title, description }) {
  return <div className="panel-heading"><div><h3>{title}</h3><p>{description}</p></div></div>
}

function ReportTable({ children, compact = false }) {
  return <div className={`report-table-wrap${compact ? ' compact' : ''}`}><table>{children}</table></div>
}

function Pagination({ page, totalPages, pageSize, onPageChange, onPageSizeChange }) {
  return (
    <div className="table-pagination report-pagination">
      <select value={pageSize} onChange={(event) => onPageSizeChange(Number(event.target.value))} aria-label="Số dòng mỗi trang">
        {pageSizeOptions.map((option) => <option key={option} value={option}>{option} dòng</option>)}
      </select>
      <div>
        <button type="button" className="secondary" onClick={() => onPageChange(Math.max(1, page - 1))} disabled={page <= 1}>Trước</button>
        <span>Trang {page} / {totalPages}</span>
        <button type="button" className="secondary" onClick={() => onPageChange(Math.min(totalPages, page + 1))} disabled={page >= totalPages}>Sau</button>
      </div>
    </div>
  )
}

function paginate(items, page, pageSize) {
  const totalPages = Math.max(1, Math.ceil(items.length / pageSize))
  const safePage = Math.min(Math.max(1, page), totalPages)
  const start = (safePage - 1) * pageSize
  return {
    items: items.slice(start, start + pageSize),
    page: safePage,
    totalPages,
  }
}

function compact(value) {
  return Object.fromEntries(Object.entries(value).filter(([, item]) => item !== '' && item !== null && item !== undefined))
}
