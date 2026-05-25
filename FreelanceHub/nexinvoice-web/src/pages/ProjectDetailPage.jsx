import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { projectsApi, tasksApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCurrency, formatDateOnly } from '../utils/format'

export function ProjectDetailPage() {
  const { id } = useParams()
  const queryClient = useQueryClient()
  const [modal, setModal] = useState(null)
  const [form, setForm] = useState({ title: '', description: '', status: 1, priority: 2, dueDate: '' })
  const [toast, setToast] = useState(null)
  const projectQuery = useQuery({ queryKey: ['project', id], queryFn: () => projectsApi.detail(id) })
  const tasksQuery = useQuery({ queryKey: ['project-tasks', id], queryFn: () => tasksApi.byProject(id) })

  const createTask = useMutation({
    mutationFn: (payload) => tasksApi.create(id, payload),
    onSuccess: () => {
      setModal(null)
      setForm({ title: '', description: '', status: 1, priority: 2, dueDate: '' })
      setToast({ type: 'success', message: 'Tạo công việc thành công' })
      queryClient.invalidateQueries({ queryKey: ['project-tasks', id] })
      queryClient.invalidateQueries({ queryKey: ['project', id] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  if (projectQuery.isLoading) return <LoadingState />
  if (projectQuery.isError) return <ErrorState message={getErrorMessage(projectQuery.error)} />

  const project = projectQuery.data
  const tasks = tasksQuery.data ?? []

  function submit(event) {
    event.preventDefault()
    createTask.mutate({ ...form, status: Number(form.status), priority: Number(form.priority), dueDate: form.dueDate || null, assignedToId: null })
  }

  return (
    <section>
      <PageHeader title={project.name} description={`Khách hàng: ${project.clientName}`} action={<button type="button" onClick={() => setModal('task')}>Tạo công việc</button>} />
      {toast ? <div className={`toast-message ${toast.type}`}>{toast.message}</div> : null}
      <div className="metric-grid">
        <article className="metric-card"><span>Ngân sách</span><strong>{formatCurrency(project.budget)}</strong></article>
        <article className="metric-card"><span>Tiến độ</span><strong>{project.progressPercentage}%</strong></article>
        <article className="metric-card"><span>Công việc</span><strong>{project.completedTasks}/{project.totalTasks}</strong></article>
        <article className="metric-card"><span>Hạn hoàn thành</span><strong>{formatDateOnly(project.endDate)}</strong></article>
      </div>
      <article className="panel">
        <h3>Công việc trong dự án</h3>
        {tasksQuery.isLoading ? <LoadingState /> : tasksQuery.isError ? <ErrorState message={getErrorMessage(tasksQuery.error)} /> : tasks.length === 0 ? <EmptyState text="Chưa có công việc." /> : (
          <div className="table-wrap">
            <table>
              <thead><tr><th>Công việc</th><th>Trạng thái</th><th>Ưu tiên</th><th>Hạn</th></tr></thead>
              <tbody>{tasks.map((task) => <tr key={task.id}><td>{task.title}</td><td><StatusBadge type="workItem" status={task.status} /></td><td>{task.priority}</td><td>{formatDateOnly(task.dueDate)}</td></tr>)}</tbody>
            </table>
          </div>
        )}
      </article>

      {modal === 'task' ? (
        <div className="modal-backdrop">
          <form className="modal-panel" onSubmit={submit}>
            <div className="modal-header"><h3>Tạo công việc</h3></div>
            <div className="form-grid two-columns">
              <label className="field"><span>Tiêu đề *</span><input required value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /></label>
              <label className="field"><span>Hạn</span><input type="date" value={form.dueDate} onChange={(e) => setForm({ ...form, dueDate: e.target.value })} /></label>
              <label className="field"><span>Trạng thái</span><select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}><option value={1}>Todo</option><option value={2}>In Progress</option><option value={3}>In Review</option><option value={4}>Done</option></select></label>
              <label className="field"><span>Ưu tiên</span><select value={form.priority} onChange={(e) => setForm({ ...form, priority: e.target.value })}><option value={1}>Low</option><option value={2}>Medium</option><option value={3}>High</option><option value={4}>Urgent</option></select></label>
              <label className="field full-span"><span>Mô tả</span><textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
            </div>
            <div className="modal-footer"><button type="button" className="secondary" onClick={() => setModal(null)}>Hủy</button><button type="submit" disabled={createTask.isPending}>Lưu</button></div>
          </form>
        </div>
      ) : null}
    </section>
  )
}
