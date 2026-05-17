import { useMemo, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { clientsApi, contractsApi, projectsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCurrency, formatDateOnly } from '../utils/format'

const emptyForm = {
  contractNumber: '',
  title: '',
  description: '',
  status: '1',
  startDate: '',
  endDate: '',
  amount: 0,
  clientId: '',
  projectId: '',
}

const statuses = [
  ['1', 'Bản nháp'],
  ['2', 'Đã gửi'],
  ['3', 'Đã phê duyệt'],
  ['4', 'Đã từ chối'],
  ['5', 'Hết hạn'],
]

export function ContractsPage() {
  const queryClient = useQueryClient()
  const [filters, setFilters] = useState({ search: '', status: '' })
  const [modal, setModal] = useState(null)
  const [form, setForm] = useState(emptyForm)
  const [selected, setSelected] = useState(null)
  const [rejectReason, setRejectReason] = useState('')
  const [toast, setToast] = useState(null)

  const contractsQuery = useQuery({
    queryKey: ['contracts', filters],
    queryFn: () => contractsApi.list({ page: 1, pageSize: 20, ...compact(filters) }),
  })
  const clientsQuery = useQuery({ queryKey: ['clients-for-contracts'], queryFn: () => clientsApi.list({ page: 1, pageSize: 100 }) })
  const projectsQuery = useQuery({ queryKey: ['projects-for-contracts'], queryFn: () => projectsApi.list({ page: 1, pageSize: 100 }) })

  const clients = clientsQuery.data?.items ?? []
  const projects = projectsQuery.data?.items ?? []
  const contracts = contractsQuery.data?.items ?? []

  const saveMutation = useMutation({
    mutationFn: (payload) => selected ? contractsApi.update(selected.id, payload) : contractsApi.create(payload),
    onSuccess: () => {
      closeModal()
      setToast({ type: 'success', message: selected ? 'Cập nhật hợp đồng thành công' : 'Tạo hợp đồng thành công' })
      queryClient.invalidateQueries({ queryKey: ['contracts'] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })
  const simpleAction = useMutation({
    mutationFn: ({ action, contract, file, reason }) => {
      if (action === 'upload') return contractsApi.upload(contract.id, file)
      if (action === 'approve') return contractsApi.approve(contract.id)
      if (action === 'reject') return contractsApi.reject(contract.id, { reason })
      return contractsApi.remove(contract.id)
    },
    onSuccess: (_, variables) => {
      closeModal()
      const messages = {
        upload: 'Tải hợp đồng lên thành công',
        approve: 'Phê duyệt hợp đồng thành công',
        reject: 'Từ chối hợp đồng thành công',
        delete: 'Xóa hợp đồng thành công',
      }
      setToast({ type: 'success', message: messages[variables.action] })
      queryClient.invalidateQueries({ queryKey: ['contracts'] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  const filteredProjects = useMemo(
    () => projects.filter((project) => !form.clientId || project.clientId === form.clientId),
    [projects, form.clientId],
  )

  function openCreate() {
    setSelected(null)
    setForm({ ...emptyForm, clientId: clients[0]?.id ?? '' })
    setModal('form')
  }

  function openEdit(contract) {
    setSelected(contract)
    setForm({
      contractNumber: contract.contractNumber,
      title: contract.title,
      description: contract.description ?? '',
      status: String(contract.status),
      startDate: contract.startDate ?? '',
      endDate: contract.endDate ?? '',
      amount: contract.amount,
      clientId: contract.clientId,
      projectId: contract.projectId ?? '',
    })
    setModal('form')
  }

  function closeModal() {
    setModal(null)
    setSelected(null)
    setRejectReason('')
  }

  function submitForm(event) {
    event.preventDefault()
    saveMutation.mutate({
      ...form,
      status: Number(form.status),
      amount: Number(form.amount),
      projectId: form.projectId || null,
      startDate: form.startDate || null,
      endDate: form.endDate || null,
    })
  }

  if (contractsQuery.isLoading) return <LoadingState />
  if (contractsQuery.isError) return <ErrorState message={getErrorMessage(contractsQuery.error)} />

  return (
    <section>
      <PageHeader
        title="Hợp đồng"
        description="Quản lý hợp đồng khách hàng"
        action={<button type="button" onClick={openCreate}>Tạo mới</button>}
      />

      {toast ? <div className={`toast-message ${toast.type}`}>{toast.message}</div> : null}

      <div className="data-table-card">
        <div className="table-toolbar">
          <div>
            <h3>Danh sách hợp đồng</h3>
            <p>{contractsQuery.data?.totalItems ?? 0} hợp đồng trong hệ thống</p>
          </div>
          <div className="table-filters">
            <input value={filters.search} onChange={(e) => setFilters({ ...filters, search: e.target.value })} placeholder="Tìm số hợp đồng, tiêu đề..." />
            <select value={filters.status} onChange={(e) => setFilters({ ...filters, status: e.target.value })}>
              <option value="">Tất cả trạng thái</option>
              {statuses.map(([value, label]) => <option key={value} value={value}>{label}</option>)}
            </select>
          </div>
        </div>

        {contracts.length === 0 ? <EmptyState text="Chưa có hợp đồng." /> : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Hợp đồng</th>
                  <th>Khách hàng</th>
                  <th>Giá trị</th>
                  <th>Trạng thái</th>
                  <th>Thời hạn</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {contracts.map((contract) => (
                  <tr key={contract.id}>
                    <td>
                      <strong className="table-primary">{contract.contractNumber}</strong>
                      <span className="table-description">{contract.title}</span>
                    </td>
                    <td>{contract.clientName}</td>
                    <td>{formatCurrency(contract.amount)}</td>
                    <td><StatusBadge type="contract" status={contract.status} /></td>
                    <td>{formatDateOnly(contract.startDate)} - {formatDateOnly(contract.endDate)}</td>
                    <td>
                      <div className="row-actions">
                        <button type="button" className="table-action" onClick={() => openEdit(contract)}>Sửa</button>
                        <label className="table-action">
                          Tải lên
                          <input className="sr-only" type="file" accept=".pdf,.docx" onChange={(e) => e.target.files?.[0] && simpleAction.mutate({ action: 'upload', contract, file: e.target.files[0] })} />
                        </label>
                        {contract.fileName ? <a className="table-action" href={contractsApi.downloadUrl(contract.id)}>Tải xuống</a> : null}
                        <button type="button" className="table-action" onClick={() => simpleAction.mutate({ action: 'approve', contract })}>Duyệt</button>
                        <button type="button" className="table-action danger" onClick={() => { setSelected(contract); setModal('reject') }}>Từ chối</button>
                        <button type="button" className="table-action danger" onClick={() => simpleAction.mutate({ action: 'delete', contract })}>Xóa</button>
                      </div>
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
          <form className="modal-panel" onSubmit={submitForm}>
            <div className="modal-header"><h3>{selected ? 'Cập nhật hợp đồng' : 'Tạo hợp đồng'}</h3></div>
            <div className="form-grid two-columns">
              <label className="field"><span>Số hợp đồng *</span><input required value={form.contractNumber} onChange={(e) => setForm({ ...form, contractNumber: e.target.value })} /></label>
              <label className="field"><span>Tiêu đề *</span><input required value={form.title} onChange={(e) => setForm({ ...form, title: e.target.value })} /></label>
              <label className="field"><span>Khách hàng *</span><select required value={form.clientId} onChange={(e) => setForm({ ...form, clientId: e.target.value, projectId: '' })}>{clients.map((client) => <option key={client.id} value={client.id}>{client.name ?? client.fullName}</option>)}</select></label>
              <label className="field"><span>Dự án</span><select value={form.projectId} onChange={(e) => setForm({ ...form, projectId: e.target.value })}><option value="">Không chọn</option>{filteredProjects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</select></label>
              <label className="field"><span>Trạng thái</span><select value={form.status} onChange={(e) => setForm({ ...form, status: e.target.value })}>{statuses.map(([value, label]) => <option key={value} value={value}>{label}</option>)}</select></label>
              <label className="field"><span>Giá trị</span><input type="number" min="0" value={form.amount} onChange={(e) => setForm({ ...form, amount: e.target.value })} /></label>
              <label className="field"><span>Ngày bắt đầu</span><input type="date" value={form.startDate} onChange={(e) => setForm({ ...form, startDate: e.target.value })} /></label>
              <label className="field"><span>Ngày kết thúc</span><input type="date" value={form.endDate} onChange={(e) => setForm({ ...form, endDate: e.target.value })} /></label>
              <label className="field full-span"><span>Mô tả</span><textarea value={form.description} onChange={(e) => setForm({ ...form, description: e.target.value })} /></label>
            </div>
            <div className="modal-footer"><button type="button" className="secondary" onClick={closeModal}>Hủy</button><button type="submit" disabled={saveMutation.isPending}>Lưu</button></div>
          </form>
        </div>
      ) : null}

      {modal === 'reject' && selected ? (
        <div className="modal-backdrop">
          <form className="modal-panel" onSubmit={(e) => { e.preventDefault(); simpleAction.mutate({ action: 'reject', contract: selected, reason: rejectReason }) }}>
            <div className="modal-header"><h3>Từ chối hợp đồng</h3></div>
            <label className="field"><span>Lý do từ chối *</span><textarea required value={rejectReason} onChange={(e) => setRejectReason(e.target.value)} /></label>
            <div className="modal-footer"><button type="button" className="secondary" onClick={closeModal}>Hủy</button><button type="submit" className="danger-button" disabled={!rejectReason.trim()}>Từ chối</button></div>
          </form>
        </div>
      ) : null}
    </section>
  )
}

function compact(value) {
  return Object.fromEntries(Object.entries(value).filter(([, item]) => item !== '' && item !== null && item !== undefined))
}
