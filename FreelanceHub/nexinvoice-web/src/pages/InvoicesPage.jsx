import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { invoicesApi, projectsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCurrency, formatDateOnly } from '../utils/format'

const today = new Date().toISOString().slice(0, 10)
const emptyInvoice = {
  invoiceNumber: `INV-${Date.now().toString().slice(-6)}`,
  issueDate: today,
  dueDate: '',
  projectId: '',
  taxAmount: 0,
  discountAmount: 0,
  status: 1,
  items: [{ description: 'Dịch vụ freelance', quantity: 1, unitPrice: 1000000 }],
}

export function InvoicesPage() {
  const queryClient = useQueryClient()
  const [modal, setModal] = useState(null)
  const [form, setForm] = useState(emptyInvoice)
  const [toast, setToast] = useState(null)

  const query = useQuery({ queryKey: ['invoices'], queryFn: () => invoicesApi.list({ page: 1, pageSize: 20 }) })
  const projectsQuery = useQuery({ queryKey: ['projects-for-invoices'], queryFn: () => projectsApi.list({ page: 1, pageSize: 100 }) })

  const create = useMutation({
    mutationFn: invoicesApi.create,
    onSuccess: () => {
      setModal(null)
      setForm({ ...emptyInvoice, invoiceNumber: `INV-${Date.now().toString().slice(-6)}` })
      setToast({ type: 'success', message: 'Tạo hóa đơn thành công' })
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  function openCreate() {
    setForm({ ...emptyInvoice, projectId: projectsQuery.data?.items?.[0]?.id ?? '' })
    setModal('form')
  }

  function updateItem(index, field, value) {
    setForm({
      ...form,
      items: form.items.map((item, itemIndex) => itemIndex === index ? { ...item, [field]: value } : item),
    })
  }

  function submit(event) {
    event.preventDefault()
    create.mutate({
      ...form,
      taxAmount: Number(form.taxAmount),
      discountAmount: Number(form.discountAmount),
      status: Number(form.status),
      dueDate: form.dueDate || null,
      items: form.items.map((item) => ({
        description: item.description,
        quantity: Number(item.quantity),
        unitPrice: Number(item.unitPrice),
      })),
    })
  }

  if (query.isLoading) return <LoadingState />
  if (query.isError) return <ErrorState message={getErrorMessage(query.error)} />

  const invoices = query.data?.items ?? []
  const projects = projectsQuery.data?.items ?? []
  const totalItems = query.data?.totalItems ?? invoices.length
  const pageSize = query.data?.pageSize ?? 20

  return (
    <section>
      <PageHeader
        title="Hóa đơn"
        description="Theo dõi hóa đơn, hạn thanh toán và trạng thái"
        action={<button type="button" onClick={openCreate}>Tạo hóa đơn</button>}
      />

      {toast ? <div className={`toast-message ${toast.type}`}>{toast.message}</div> : null}

      <div className="data-table-card">
        <div className="table-toolbar">
          <div>
            <h3>Danh sách hóa đơn</h3>
            <p>{totalItems} hóa đơn trong hệ thống</p>
          </div>
          <div className="table-filters">
            <input type="search" placeholder="Tìm số hóa đơn, khách hàng, dự án..." aria-label="Tìm kiếm hóa đơn" />
          </div>
        </div>

        {invoices.length === 0 ? <EmptyState text="Chưa có hóa đơn." /> : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Số hóa đơn</th>
                  <th>Khách hàng</th>
                  <th>Dự án</th>
                  <th>Trạng thái</th>
                  <th>Hạn</th>
                  <th>Tổng tiền</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {invoices.map((invoice) => (
                  <tr key={invoice.id}>
                    <td><strong className="table-primary">{invoice.invoiceNumber}</strong></td>
                    <td>{invoice.clientName}</td>
                    <td>{invoice.projectName ?? '-'}</td>
                    <td><StatusBadge type="invoice" status={invoice.status} /></td>
                    <td>{formatDateOnly(invoice.dueDate)}</td>
                    <td><strong>{formatCurrency(invoice.totalAmount)}</strong></td>
                    <td className="row-actions">
                      <Link className="table-action" to={`/invoices/${invoice.id}`}>Chi tiết</Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="table-pagination">
          <span>Hiển thị {invoices.length} / {totalItems}</span>
          <div>
            <button type="button" className="secondary" disabled>Trước</button>
            <span>Trang 1</span>
            <button type="button" className="secondary" disabled={invoices.length < pageSize}>Sau</button>
          </div>
        </div>
      </div>

      {modal === 'form' ? (
        <div className="modal-backdrop">
          <form className="modal-panel" onSubmit={submit}>
            <div className="modal-header"><h3>Tạo hóa đơn</h3></div>
            <div className="form-grid two-columns">
              <label className="field"><span>Số hóa đơn *</span><input required value={form.invoiceNumber} onChange={(e) => setForm({ ...form, invoiceNumber: e.target.value })} /></label>
              <label className="field"><span>Dự án *</span><select required value={form.projectId} onChange={(e) => setForm({ ...form, projectId: e.target.value })}>{projects.map((project) => <option key={project.id} value={project.id}>{project.name}</option>)}</select></label>
              <label className="field"><span>Ngày phát hành *</span><input required type="date" value={form.issueDate} onChange={(e) => setForm({ ...form, issueDate: e.target.value })} /></label>
              <label className="field"><span>Ngày đến hạn</span><input type="date" value={form.dueDate} onChange={(e) => setForm({ ...form, dueDate: e.target.value })} /></label>
              <label className="field"><span>Thuế</span><input type="number" min="0" value={form.taxAmount} onChange={(e) => setForm({ ...form, taxAmount: e.target.value })} /></label>
              <label className="field"><span>Giảm giá</span><input type="number" min="0" value={form.discountAmount} onChange={(e) => setForm({ ...form, discountAmount: e.target.value })} /></label>
              <label className="field full-span"><span>Mô tả dòng *</span><input required value={form.items[0].description} onChange={(e) => updateItem(0, 'description', e.target.value)} /></label>
              <label className="field"><span>Số lượng *</span><input required type="number" min="1" value={form.items[0].quantity} onChange={(e) => updateItem(0, 'quantity', e.target.value)} /></label>
              <label className="field"><span>Đơn giá *</span><input required type="number" min="0" value={form.items[0].unitPrice} onChange={(e) => updateItem(0, 'unitPrice', e.target.value)} /></label>
            </div>
            <div className="modal-footer"><button type="button" className="secondary" onClick={() => setModal(null)}>Hủy</button><button type="submit" disabled={create.isPending}>Lưu</button></div>
          </form>
        </div>
      ) : null}
    </section>
  )
}
