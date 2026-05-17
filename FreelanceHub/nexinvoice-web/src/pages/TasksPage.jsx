import { useState } from 'react'
import { useQuery } from '@tanstack/react-query'
import { projectsApi, tasksApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatDateOnly } from '../utils/format'

export function TasksPage() {
  const [projectId, setProjectId] = useState('')
  const projectsQuery = useQuery({ queryKey: ['projects-for-tasks'], queryFn: () => projectsApi.list({ page: 1, pageSize: 100 }) })
  const tasksQuery = useQuery({
    queryKey: ['tasks', projectId],
    queryFn: () => tasksApi.byProject(projectId),
    enabled: Boolean(projectId),
  })

  if (projectsQuery.isLoading) return <LoadingState />
  if (projectsQuery.isError) return <ErrorState message={getErrorMessage(projectsQuery.error)} />

  const projects = projectsQuery.data?.items ?? []
  const tasks = tasksQuery.data ?? []

  return (
    <section>
      <PageHeader title="Công việc" description="Chọn dự án để xem danh sách công việc" />

      <div className="data-table-card">
        <div className="table-toolbar">
          <div>
            <h3>Danh sách công việc</h3>
            <p>{projectId ? `${tasks.length} công việc trong dự án` : 'Chọn dự án để tải dữ liệu'}</p>
          </div>
          <div className="table-filters">
            <select value={projectId} onChange={(event) => setProjectId(event.target.value)}>
              <option value="">Chọn dự án</option>
              {projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}
            </select>
          </div>
        </div>

        {!projectId ? <EmptyState text="Vui lòng chọn một dự án." /> : tasksQuery.isLoading ? <LoadingState /> : tasksQuery.isError ? <ErrorState message={getErrorMessage(tasksQuery.error)} /> : tasks.length === 0 ? <EmptyState text="Chưa có công việc trong dự án này." /> : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Công việc</th>
                  <th>Trạng thái</th>
                  <th>Ưu tiên</th>
                  <th>Hạn</th>
                  <th>Người phụ trách</th>
                </tr>
              </thead>
              <tbody>
                {tasks.map((task) => (
                  <tr key={task.id}>
                    <td><strong className="table-primary">{task.title}</strong></td>
                    <td><StatusBadge type="workItem" status={task.status} /></td>
                    <td><span className="status-badge tone-neutral">{task.priority}</span></td>
                    <td>{formatDateOnly(task.dueDate)}</td>
                    <td>{task.assignedToName ?? '-'}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}
