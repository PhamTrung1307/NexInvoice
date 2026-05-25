import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { clientsApi, projectsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCurrency, formatDateOnly } from '../utils/format'

const emptyProject = {
  name: '',
  description: '',
  startDate: '',
  endDate: '',
  budget: 0,
  clientId: '',
  status: 2,
}

export function ProjectsPage() {
  const queryClient = useQueryClient()
  const [modal, setModal] = useState(null)
  const [selected, setSelected] = useState(null)
  const [form, setForm] = useState(emptyProject)
  const [toast, setToast] = useState(null)

  const query = useQuery({ queryKey: ['projects'], queryFn: () => projectsApi.list({ page: 1, pageSize: 20 }) })
  const clientsQuery = useQuery({ queryKey: ['clients-for-projects'], queryFn: () => clientsApi.list({ page: 1, pageSize: 100 }) })

  const save = useMutation({
    mutationFn: (payload) => selected ? projectsApi.update(selected.id, payload) : projectsApi.create(payload),
    onSuccess: () => {
      closeModal()
      setToast({ type: 'success', message: selected ? 'Cập nhật dự án thành công' : 'Tạo dự án thành công' })
      queryClient.invalidateQueries({ queryKey: ['projects'] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  const remove = useMutation({
    mutationFn: projectsApi.remove,
    onSuccess: () => {
      setToast({ type: 'success', message: 'Xóa dự án thành công' })
      queryClient.invalidateQueries({ queryKey: ['projects'] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  function openCreate() {
    setSelected(null)
    setForm({ ...emptyProject, clientId: clientsQuery.data?.items?.[0]?.id ?? '' })
    setModal('form')
  }

  function openEdit(project) {
    setSelected(project)
    setForm({
      name: project.name ?? '',
      description: project.description ?? '',
      startDate: project.startDate ?? '',
      endDate: project.endDate ?? '',
      budget: project.budget ?? 0,
      clientId: project.clientId ?? '',
      status: project.status ?? 2,
    })
    setModal('form')
  }

  function closeModal() {
    setModal(null)
    setSelected(null)
  }

  function submit(event) {
    event.preventDefault()
    save.mutate({
      ...form,
      budget: Number(form.budget),
      status: Number(form.status),
      startDate: form.startDate || null,
      endDate: form.endDate || null,
    })
  }

  if (query.isLoading) return <LoadingState />
  if (query.isError) return <ErrorState message={getErrorMessage(query.error)} />

  const projects = query.data?.items ?? []
  const clients = clientsQuery.data?.items ?? []
  const totalItems = query.data?.totalItems ?? projects.length
  const pageSize = query.data?.pageSize ?? 20

  return (
    <section>
      <PageHeader
        title="Dự án"
        description="Theo dõi tiến độ dự án theo khách hàng"
        action={<button type="button" onClick={openCreate}>Tạo dự án</button>}
      />

      {toast ? <div className={`toast-message ${toast.type}`}>{toast.message}</div> : null}

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
                      <button type="button" className="table-action" onClick={() => openEdit(project)}>Sửa</button>
                      <button type="button" className="table-action danger" onClick={() => remove.mutate(project.id)}>Xóa</button>
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

      {modal === 'form' ? (
        <div className="modal-backdrop">
          <form className="modal-panel" onSubmit={submit}>
            <div className="modal-header"><h3>{selected ? 'Cập nhật dự án' : 'Tạo dự án'}</h3></div>
            <div className="form-grid two-columns">
              <label className="field"><span>Tên dự án *</span><input required value={form.name} onChange={(e) => setForm({ ...form, name: e.target.value })} /></label>
              <label className="field"><span>Khách hàng *</span><select required value={form.clientId} onChange={(e) => setForm({ ...form, clientId: e.target.value })}>{clients.map((client) => <option key={client.id} value={client.id}>{client.fullName ?? client.name}</option>)}</select></label>
              <label className="field"><span>Trạng thái</span><select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}><option value={1}>Bản nháp</option><option value={2}>Đang chạy</option><option value={3}>Tạm dừng</option><option value={4}>Hoàn thành</option><option value={5}>Đã hủy</option></select></label>
              <label className="field"><span>Ngân sách</span><input type="number" min="0" value={form.budget} onChange={(e) => setForm({ ...form, budget: e.target.value })} /></label>
              <label className="field"><span>Ngày bắt đầu</span><input type="date" value={form.startDate ?? ''} onChange={(e) => setForm({ ...form, startDate: e.target.value })} /></label>
              <label className="field"><span>Ngày kết thúc</span><input type="date" value={form.endDate ?? ''} onChange={(e) => setForm({ ...form, endDate: e.target.value })} /></label>
              <label className="field full-span"><span>Mô tả</span><textarea value={form.description ?? ''} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
            </div>
            <div className="modal-footer"><button type="button" className="secondary" onClick={closeModal}>Hủy</button><button type="submit" disabled={save.isPending}>Lưu</button></div>
          </form>
        </div>
      ) : null}
    </section>
  )
}
