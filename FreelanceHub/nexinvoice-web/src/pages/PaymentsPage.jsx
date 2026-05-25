import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { invoicesApi, paymentsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCurrency, formatDateOnly } from '../utils/format'

const today = new Date().toISOString().slice(0, 10)

export function PaymentsPage() {
  const queryClient = useQueryClient()
  const [invoiceId, setInvoiceId] = useState('')
  const [selectedPayment, setSelectedPayment] = useState(null)
  const [modalType, setModalType] = useState(null)
  const [rejectReason, setRejectReason] = useState('')
  const [paymentForm, setPaymentForm] = useState({ amount: 0, method: 1, paymentDate: today, transactionReference: '' })
  const [proofFile, setProofFile] = useState(null)
  const [toast, setToast] = useState(null)

  const currentUser = JSON.parse(localStorage.getItem('currentUser') ?? '{}')
  const roles = currentUser.roles ?? currentUser.Roles ?? []
  const isAdmin = roles.includes('Admin')

  const invoicesQuery = useQuery({
    queryKey: ['invoices-for-payments'],
    queryFn: () => invoicesApi.list({ page: 1, pageSize: 100 }),
  })
  const selectedInvoiceId = invoiceId || invoicesQuery.data?.items?.[0]?.id
  const paymentsQuery = useQuery({
    queryKey: ['payments', selectedInvoiceId],
    queryFn: () => paymentsApi.byInvoice(selectedInvoiceId),
    enabled: Boolean(selectedInvoiceId),
  })

  const refreshPayments = () => {
    queryClient.invalidateQueries({ queryKey: ['payments', selectedInvoiceId] })
    queryClient.invalidateQueries({ queryKey: ['invoices-for-payments'] })
  }

  const createMutation = useMutation({
    mutationFn: (payload) => paymentsApi.create(payload),
    onSuccess: () => {
      closeModal()
      setToast({ type: 'success', message: 'Tạo thanh toán thành công' })
      refreshPayments()
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  const uploadMutation = useMutation({
    mutationFn: ({ id, file }) => paymentsApi.uploadProof(id, file),
    onSuccess: () => {
      closeModal()
      setToast({ type: 'success', message: 'Tải minh chứng thành công' })
      refreshPayments()
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  const confirmMutation = useMutation({
    mutationFn: paymentsApi.confirm,
    onSuccess: () => {
      closeModal()
      setToast({ type: 'success', message: 'Xác nhận thanh toán thành công' })
      refreshPayments()
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  const rejectMutation = useMutation({
    mutationFn: ({ id, reason }) => paymentsApi.reject(id, { reason }),
    onSuccess: () => {
      closeModal()
      setToast({ type: 'success', message: 'Từ chối thanh toán thành công' })
      refreshPayments()
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  function openCreateModal() {
    setPaymentForm({ amount: 0, method: 1, paymentDate: today, transactionReference: '' })
    setModalType('create')
    setToast(null)
  }

  function closeModal() {
    setSelectedPayment(null)
    setModalType(null)
    setRejectReason('')
    setProofFile(null)
  }

  function submitCreate(event) {
    event.preventDefault()
    createMutation.mutate({
      invoiceId: selectedInvoiceId,
      amount: Number(paymentForm.amount),
      method: Number(paymentForm.method),
      paymentDate: paymentForm.paymentDate,
      transactionReference: paymentForm.transactionReference || null,
    })
  }

  function submitReject(event) {
    event.preventDefault()
    rejectMutation.mutate({ id: selectedPayment.id, reason: rejectReason })
  }

  function submitProof(event) {
    event.preventDefault()
    uploadMutation.mutate({ id: selectedPayment.id, file: proofFile })
  }

  if (invoicesQuery.isLoading) return <LoadingState />
  if (invoicesQuery.isError) return <ErrorState message={getErrorMessage(invoicesQuery.error)} />
  if (!selectedInvoiceId) return <EmptyState text="Chưa có hóa đơn để xem thanh toán." />

  const invoices = invoicesQuery.data?.items ?? []
  const payments = paymentsQuery.data ?? []
  const isSubmitting = confirmMutation.isPending || rejectMutation.isPending || createMutation.isPending || uploadMutation.isPending

  return (
    <section>
      <PageHeader
        title="Thanh toán"
        description="Theo dõi các khoản thanh toán theo hóa đơn"
        action={<button type="button" onClick={openCreateModal}>Tạo thanh toán</button>}
      />

      {toast ? <div className={`toast-message ${toast.type === 'error' ? 'error' : 'success'}`} role="status">{toast.message}</div> : null}

      <div className="data-table-card">
        <div className="table-toolbar">
          <div>
            <h3>Lịch sử thanh toán</h3>
            <p>{payments.length} khoản thanh toán của hóa đơn đã chọn</p>
          </div>
          <div className="table-filters">
            <select value={selectedInvoiceId} onChange={(event) => setInvoiceId(event.target.value)}>
              {invoices.map((invoice) => <option key={invoice.id} value={invoice.id}>{invoice.invoiceNumber}</option>)}
            </select>
          </div>
        </div>

        {paymentsQuery.isLoading ? <LoadingState /> : paymentsQuery.isError ? <ErrorState message={getErrorMessage(paymentsQuery.error)} /> : payments.length === 0 ? <EmptyState text="Chưa có khoản thanh toán." /> : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Hóa đơn</th>
                  <th>Số tiền</th>
                  <th>Phương thức</th>
                  <th>Trạng thái</th>
                  <th>Minh chứng</th>
                  <th>Ngày thanh toán</th>
                  <th>Thao tác</th>
                </tr>
              </thead>
              <tbody>
                {payments.map((payment) => {
                  const canReview = isAdmin && normalizePaymentStatus(payment.status) === 'Pending'
                  return (
                    <tr key={payment.id}>
                      <td><strong className="table-primary">{payment.invoiceNumber}</strong></td>
                      <td><strong>{formatCurrency(payment.amount)}</strong></td>
                      <td>{payment.method}</td>
                      <td><StatusBadge type="payment" status={payment.status} /></td>
                      <td>{payment.proofFileName ?? '-'}</td>
                      <td>{formatDateOnly(payment.paymentDate)}</td>
                      <td>
                        <div className="row-actions">
                          {normalizePaymentStatus(payment.status) === 'Pending' ? <button type="button" className="table-action" onClick={() => { setSelectedPayment(payment); setModalType('proof') }}>Upload proof</button> : null}
                          {canReview ? <button type="button" className="table-action" onClick={() => { setSelectedPayment(payment); setModalType('confirm') }}>Xác nhận</button> : null}
                          {canReview ? <button type="button" className="table-action danger" onClick={() => { setSelectedPayment(payment); setModalType('reject') }}>Từ chối</button> : null}
                        </div>
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modalType === 'create' ? (
        <div className="modal-backdrop">
          <form className="modal-panel" onSubmit={submitCreate}>
            <div className="modal-header"><h3>Tạo thanh toán</h3></div>
            <div className="form-grid two-columns">
              <label className="field"><span>Số tiền *</span><input required type="number" min="1" value={paymentForm.amount} onChange={(e) => setPaymentForm({ ...paymentForm, amount: e.target.value })} /></label>
              <label className="field"><span>Phương thức</span><select value={paymentForm.method} onChange={(e) => setPaymentForm({ ...paymentForm, method: e.target.value })}><option value={1}>BankTransfer</option><option value={2}>Cash</option><option value={3}>Momo</option><option value={4}>PayPal</option></select></label>
              <label className="field"><span>Ngày thanh toán *</span><input required type="date" value={paymentForm.paymentDate} onChange={(e) => setPaymentForm({ ...paymentForm, paymentDate: e.target.value })} /></label>
              <label className="field"><span>Mã giao dịch</span><input value={paymentForm.transactionReference} onChange={(e) => setPaymentForm({ ...paymentForm, transactionReference: e.target.value })} /></label>
            </div>
            <div className="modal-footer"><button type="button" className="secondary" onClick={closeModal}>Hủy</button><button type="submit" disabled={isSubmitting}>Lưu</button></div>
          </form>
        </div>
      ) : null}

      {modalType === 'proof' && selectedPayment ? (
        <div className="modal-backdrop">
          <form className="modal-panel" onSubmit={submitProof}>
            <div className="modal-header"><h3>Tải minh chứng thanh toán</h3></div>
            <label className="field"><span>Tệp minh chứng *</span><input required type="file" accept=".jpg,.jpeg,.png,.pdf,.docx,.xlsx" onChange={(e) => setProofFile(e.target.files?.[0] ?? null)} /></label>
            <div className="modal-footer"><button type="button" className="secondary" onClick={closeModal}>Hủy</button><button type="submit" disabled={isSubmitting || !proofFile}>Tải lên</button></div>
          </form>
        </div>
      ) : null}

      {modalType === 'confirm' && selectedPayment ? (
        <div className="modal-backdrop">
          <div className="modal-panel" role="dialog" aria-modal="true">
            <div className="modal-header"><h3>Xác nhận thanh toán</h3></div>
            <div className="payment-review-summary"><span>Hóa đơn</span><strong>{selectedPayment.invoiceNumber}</strong><span>Số tiền</span><strong>{formatCurrency(selectedPayment.amount)}</strong></div>
            <div className="modal-footer"><button type="button" className="secondary" onClick={closeModal}>Hủy</button><button type="button" onClick={() => confirmMutation.mutate(selectedPayment.id)} disabled={isSubmitting}>Xác nhận</button></div>
          </div>
        </div>
      ) : null}

      {modalType === 'reject' && selectedPayment ? (
        <div className="modal-backdrop">
          <form className="modal-panel" onSubmit={submitReject}>
            <div className="modal-header"><h3>Từ chối thanh toán</h3></div>
            <label className="field"><span>Lý do từ chối *</span><textarea required value={rejectReason} onChange={(event) => setRejectReason(event.target.value)} /></label>
            <div className="modal-footer"><button type="button" className="secondary" onClick={closeModal}>Hủy</button><button type="submit" className="danger-button" disabled={isSubmitting || !rejectReason.trim()}>Từ chối</button></div>
          </form>
        </div>
      ) : null}
    </section>
  )
}

function normalizePaymentStatus(status) {
  if (status === 1 || status === '1') return 'Pending'
  if (status === 2 || status === '2') return 'Confirmed'
  if (status === 3 || status === '3') return 'Rejected'
  return String(status ?? '').replace(/\s+/g, '')
}
