import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { projectsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCurrency, formatDateOnly } from '../utils/format'

export function ProjectsPage() {
  const query = useQuery({ queryKey: ['projects'], queryFn: () => projectsApi.list({ page: 1, pageSize: 20 }) })

  if (query.isLoading) return <LoadingState />
  if (query.isError) return <ErrorState message={getErrorMessage(query.error)} />

  const projects = query.data?.items ?? []
  const totalItems = query.data?.totalItems ?? projects.length
  const pageSize = query.data?.pageSize ?? 20

  return (
    <section>
      <PageHeader title="Dự án" description="Theo dõi tiến độ dự án theo khách hàng" />

      <div className="data-table-card">
        <div className="table-toolbar">
          <div>
            <h3>Danh sách dự án</h3>
            <p>{totalItems} dự án trong hệ thống</p>
          </div>
          <div className="table-filters">
            <input type="search" placeholder="Tìm tên dự án hoặc khách hàng..." aria-label="Tìm kiếm dự án" />
          </div>
        </div>

        {projects.length === 0 ? <EmptyState text="Chưa có dự án." /> : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Dự án</th>
                  <th>Khách hàng</th>
                  <th>Trạng thái</th>
                  <th>Ngân sách</th>
                  <th>Tiến độ</th>
                  <th>Hạn</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {projects.map((project) => (
                  <tr key={project.id}>
                    <td><strong className="table-primary">{project.name}</strong></td>
                    <td>{project.clientName}</td>
                    <td><StatusBadge type="project" status={project.status} /></td>
                    <td>{formatCurrency(project.budget)}</td>
                    <td>
                      <div className="table-progress">
                        <span>{project.progressPercentage ?? 0}%</span>
                        <div className="progress"><span style={{ width: `${project.progressPercentage ?? 0}%` }} /></div>
                      </div>
                    </td>
                    <td>{formatDateOnly(project.endDate)}</td>
                    <td className="row-actions">
                      <Link className="table-action" to={`/projects/${project.id}`}>Chi tiết</Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="table-pagination">
          <span>Hiển thị {projects.length} / {totalItems}</span>
          <div>
            <button type="button" className="secondary" disabled>Trước</button>
            <span>Trang 1</span>
            <button type="button" className="secondary" disabled={projects.length < pageSize}>Sau</button>
          </div>
        </div>
      </div>
    </section>
  )
}
