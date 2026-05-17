import { useQuery } from '@tanstack/react-query'
import { dashboardApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, SkeletonCard } from '../components/States'
import { formatCurrency, formatDate } from '../utils/format'

export function DashboardPage() {
  const query = useQuery({ queryKey: ['dashboard-summary'], queryFn: dashboardApi.summary })

  if (query.isLoading) return <DashboardSkeleton />
  if (query.isError) return <ErrorState message={getErrorMessage(query.error)} />

  const data = query.data ?? {}
  const monthlyRevenue = data.monthlyRevenue ?? []
  const projectStats = data.projectStatusStatistics ?? []
  const recentActivities = data.recentActivities ?? data.activities ?? []
  const pendingPayments = data.pendingPayments ?? data.pendingPaymentItems ?? []
  const overdueInvoices = data.overdueInvoices ?? data.overdueInvoiceItems ?? []
  const maxMonthlyRevenue = Math.max(...monthlyRevenue.map((item) => Number(item.revenue ?? 0)), 0)
  const totalProjectStatuses = projectStats.reduce((sum, item) => sum + Number(item.count ?? 0), 0)

  const cards = [
    { label: 'Tổng khách hàng', value: data.totalClients ?? 0, icon: 'KH', note: 'Hồ sơ khách hàng' },
    { label: 'Tổng dự án', value: data.totalProjects ?? 0, icon: 'DA', note: 'Dự án đang quản lý' },
    { label: 'Tổng hóa đơn', value: data.totalInvoices ?? 0, icon: 'HD', note: 'Hóa đơn đã tạo' },
    { label: 'Doanh thu', value: formatCurrency(data.totalRevenue), icon: 'DT', note: 'Doanh thu đã ghi nhận', highlight: true },
    { label: 'Hóa đơn quá hạn', value: data.overdueInvoicesCount ?? 0, icon: 'QH', note: 'Cần theo dõi', tone: 'warning' },
    { label: 'Thanh toán chờ xác nhận', value: data.pendingPaymentsCount ?? 0, icon: 'TT', note: 'Khoản cần xử lý', tone: 'primary' },
  ]

  return (
    <section className="dashboard-page">
      <div className="dashboard-hero">
        <div>
          <span className="eyebrow">Tổng quan</span>
          <h2>Điều hành tài chính và dự án trong một màn hình</h2>
          <p>Theo dõi khách hàng, dự án, hóa đơn và thanh toán theo thời gian gần nhất.</p>
        </div>
        <div className="hero-summary">
          <span>Doanh thu</span>
          <strong>{formatCurrency(data.totalRevenue)}</strong>
        </div>
      </div>

      <div className="dashboard-metric-grid">
        {cards.map((card) => (
          <article className={`dashboard-stat-card ${card.highlight ? 'highlight' : ''}`} key={card.label}>
            <div className="stat-card-top">
              <span className={`stat-icon ${card.tone ?? ''}`}>{card.icon}</span>
              <span>{card.label}</span>
            </div>
            <strong>{card.value}</strong>
            <p>{card.note}</p>
          </article>
        ))}
      </div>

      <div className="dashboard-grid">
        <article className="dashboard-panel revenue-panel">
          <div className="panel-heading">
            <div>
              <h3>Doanh thu theo tháng</h3>
              <p>Biểu đồ doanh thu dựa trên dữ liệu hóa đơn và thanh toán.</p>
            </div>
          </div>

          {monthlyRevenue.length > 0 ? (
            <div className="revenue-list">
              {monthlyRevenue.map((item) => {
                const revenue = Number(item.revenue ?? 0)
                const width = maxMonthlyRevenue > 0 ? Math.max((revenue / maxMonthlyRevenue) * 100, 4) : 0

                return (
                  <div className="revenue-row" key={`${item.year}-${item.month}`}>
                    <span className="revenue-label">Tháng {item.month}/{item.year}</span>
                    <div className="revenue-track">
                      <span style={{ width: `${width}%` }} />
                    </div>
                    <strong>{formatCurrency(revenue)}</strong>
                  </div>
                )
              })}
            </div>
          ) : (
            <EmptyState title="Chưa có dữ liệu doanh thu" text="Khi có hóa đơn hoặc thanh toán, biểu đồ sẽ hiển thị tại đây." />
          )}
        </article>

        <article className="dashboard-panel">
          <div className="panel-heading">
            <div>
              <h3>Thống kê trạng thái dự án</h3>
              <p>Tỷ trọng dự án theo từng trạng thái xử lý.</p>
            </div>
          </div>

          {projectStats.length > 0 ? (
            <div className="status-list">
              {projectStats.map((item) => {
                const count = Number(item.count ?? 0)
                const width = totalProjectStatuses > 0 ? Math.max((count / totalProjectStatuses) * 100, 6) : 0

                return (
                  <div className="status-row" key={item.status}>
                    <div>
                      <span>{projectStatusLabel(item.status)}</span>
                      <strong>{count}</strong>
                    </div>
                    <div className="progress">
                      <span style={{ width: `${width}%` }} />
                    </div>
                  </div>
                )
              })}
            </div>
          ) : (
            <EmptyState title="Chưa có thống kê dự án" text="Dữ liệu sẽ xuất hiện khi dự án được tạo trong hệ thống." />
          )}
        </article>
      </div>

      <div className="dashboard-grid compact">
        <article className="dashboard-panel">
          <div className="panel-heading">
            <div>
              <h3>Hoạt động gần đây</h3>
              <p>Các thay đổi mới nhất trong hệ thống.</p>
            </div>
          </div>
          <DashboardList
            items={recentActivities}
            emptyTitle="Chưa có hoạt động gần đây"
            emptyText="Các thao tác mới sẽ được hiển thị khi hệ thống ghi nhận dữ liệu."
            renderItem={(item) => (
              <>
                <strong>{item.title ?? item.message ?? 'Hoạt động hệ thống'}</strong>
                <span>{formatDate(item.createdAt ?? item.date)}</span>
              </>
            )}
          />
        </article>

        <article className="dashboard-panel">
          <div className="panel-heading">
            <div>
              <h3>Thanh toán chờ xác nhận</h3>
              <p>{data.pendingPaymentsCount ?? 0} khoản thanh toán đang chờ xử lý.</p>
            </div>
          </div>
          <DashboardList
            items={pendingPayments}
            emptyTitle="Không có thanh toán chờ xác nhận"
            emptyText="Các khoản thanh toán mới sẽ xuất hiện tại đây khi cần xác nhận."
            renderItem={(item) => (
              <>
                <strong>{item.invoiceNumber ?? item.clientName ?? 'Thanh toán chờ xác nhận'}</strong>
                <span>{formatCurrency(item.amount)} · {formatDate(item.createdAt ?? item.paymentDate)}</span>
              </>
            )}
          />
        </article>

        <article className="dashboard-panel">
          <div className="panel-heading">
            <div>
              <h3>Hóa đơn quá hạn</h3>
              <p>{data.overdueInvoicesCount ?? 0} hóa đơn cần theo dõi.</p>
            </div>
          </div>
          <DashboardList
            items={overdueInvoices}
            emptyTitle="Không có hóa đơn quá hạn"
            emptyText="Những hóa đơn quá hạn sẽ được liệt kê tại đây."
            renderItem={(item) => (
              <>
                <strong>{item.invoiceNumber ?? item.clientName ?? 'Hóa đơn quá hạn'}</strong>
                <span>{formatCurrency(item.totalAmount)} · Hạn {formatDate(item.dueDate)}</span>
              </>
            )}
          />
        </article>
      </div>
    </section>
  )
}

function DashboardList({ items, emptyTitle, emptyText, renderItem }) {
  if (!items?.length) {
    return <EmptyState title={emptyTitle} text={emptyText} />
  }

  return (
    <div className="dashboard-list">
      {items.slice(0, 5).map((item, index) => (
        <div className="dashboard-list-item" key={item.id ?? `${item.title ?? item.invoiceNumber ?? 'item'}-${index}`}>
          <span className="list-dot" />
          <div>{renderItem(item)}</div>
        </div>
      ))}
    </div>
  )
}

function DashboardSkeleton() {
  return (
    <section className="dashboard-page">
      <div className="dashboard-hero skeleton-block" />
      <div className="dashboard-metric-grid">
        {Array.from({ length: 6 }).map((_, index) => <SkeletonCard key={index} lines={1} />)}
      </div>
      <div className="dashboard-grid">
        <div className="dashboard-panel skeleton-panel" />
        <div className="dashboard-panel skeleton-panel" />
      </div>
    </section>
  )
}

function projectStatusLabel(value) {
  const labels = {
    1: 'Bản nháp',
    2: 'Đang thực hiện',
    3: 'Chờ phản hồi',
    4: 'Hoàn thành',
    5: 'Đã hủy',
    Draft: 'Bản nháp',
    Active: 'Đang thực hiện',
    InProgress: 'Đang thực hiện',
    WaitingFeedback: 'Chờ phản hồi',
    OnHold: 'Chờ phản hồi',
    Completed: 'Hoàn thành',
    Cancelled: 'Đã hủy',
  }

  return labels[value] ?? `Trạng thái ${value}`
}
