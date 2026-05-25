import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { projectsApi, tasksApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatDateOnly } from '../utils/format'

const emptyTask = {
  title: '',
  description: '',
  status: 1,
  priority: 2,
  dueDate: '',
  assignedToId: null,
}

export function TasksPage() {
  const queryClient = useQueryClient()
  const [projectId, setProjectId] = useState('')
  const [form, setForm] = useState(emptyTask)
  const [modal, setModal] = useState(null)
  const [toast, setToast] = useState(null)

  const projectsQuery = useQuery({ queryKey: ['projects-for-tasks'], queryFn: () => projectsApi.list({ page: 1, pageSize: 100 }) })
  const tasksQuery = useQuery({
    queryKey: ['tasks', projectId],
    queryFn: () => tasksApi.byProject(projectId),
    enabled: Boolean(projectId),
  })

  const createTask = useMutation({
    mutationFn: (payload) => tasksApi.create(projectId, payload),
    onSuccess: () => {
      setModal(null)
      setForm(emptyTask)
      setToast({ type: 'success', message: 'Tạo công việc thành công' })
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] })
      queryClient.invalidateQueries({ queryKey: ['project-tasks', projectId] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  const updateStatus = useMutation({
    mutationFn: ({ id, status }) => tasksApi.status(id, Number(status)),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['tasks', projectId] })
      setToast({ type: 'success', message: 'Cập nhật trạng thái công việc thành công' })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  function submit(event) {
    event.preventDefault()
    createTask.mutate({
      ...form,
      status: Number(form.status),
      priority: Number(form.priority),
      dueDate: form.dueDate || null,
      assignedToId: null,
    })
  }

  if (projectsQuery.isLoading) return <LoadingState />
  if (projectsQuery.isError) return <ErrorState message={getErrorMessage(projectsQuery.error)} />

  const projects = projectsQuery.data?.items ?? []
  const tasks = tasksQuery.data ?? []

  return (
    <section>
      <PageHeader
        title="Công việc"
        description="Chọn dự án để xem và tạo công việc"
        action={<button type="button" disabled={!projectId} onClick={() => setModal('form')}>Tạo công việc</button>}
      />

      {toast ? <div className={`toast-message ${toast.type}`}>{toast.message}</div> : null}

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
                  <th>Thao tác</th>
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
                    <td>
                      <select value={task.status} onChange={(e) => updateStatus.mutate({ id: task.id, status: e.target.value })}>
                        <option value={1}>Todo</option>
                        <option value={2}>In Progress</option>
                        <option value={3}>In Review</option>
                        <option value={4}>Done</option>
                        <option value={5}>Cancelled</option>
                      </select>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modal === 'form' ? (
        <div className="modal-backdrop">
          <form className="modal-panel" onSubmit={submit}>
            <div className="modal-header"><h3>Tạo công việc</h3></div>
            <div className="form-grid two-columns">
              <label className="field"><span>Tiêu đề *</span><input required value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /></label>
              <label className="field"><span>Hạn</span><input type="date" value={form.dueDate} onChange={(e) => setForm({ ...form, dueDate: e.target.value })} /></label>
              <label className="field"><span>Trạng thái</span><select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}><option value={1}>Todo</option><option value={2}>In Progress</option><option value={3}>In Review</option><option value={4}>Done</option><option value={5}>Cancelled</option></select></label>
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
