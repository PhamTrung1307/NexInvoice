import { useQuery } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { projectsApi, tasksApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { formatCurrency, formatDateOnly } from '../utils/format'

export function ProjectDetailPage() {
  const { id } = useParams()
  const projectQuery = useQuery({ queryKey: ['project', id], queryFn: () => projectsApi.detail(id) })
  const tasksQuery = useQuery({ queryKey: ['project-tasks', id], queryFn: () => tasksApi.byProject(id) })

  if (projectQuery.isLoading) return <LoadingState />
  if (projectQuery.isError) return <ErrorState message={getErrorMessage(projectQuery.error)} />

  const project = projectQuery.data
  const tasks = tasksQuery.data ?? []

  return (
    <section>
      <PageHeader title={project.name} description={`Khách hàng: ${project.clientName}`} />
      <div className="metric-grid">
        <article className="metric-card"><span>Ngân sách</span><strong>{formatCurrency(project.budget)}</strong></article>
        <article className="metric-card"><span>Tiến độ</span><strong>{project.progressPercentage}%</strong></article>
        <article className="metric-card"><span>Công việc</span><strong>{project.completedTasks}/{project.totalTasks}</strong></article>
        <article className="metric-card"><span>Hạn hoàn thành</span><strong>{formatDateOnly(project.endDate)}</strong></article>
      </div>
      <article className="panel">
        <h3>Công việc trong dự án</h3>
        {tasksQuery.isLoading ? <LoadingState /> : (
          <div className="simple-list">
            {tasks.map((task) => (
              <div key={task.id}>
                <span>{task.title}</span>
                <strong>Ưu tiên {task.priority}</strong>
              </div>
            ))}
          </div>
        )}
      </article>
    </section>
  )
}
