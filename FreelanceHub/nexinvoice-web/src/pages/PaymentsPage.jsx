import { useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { invoicesApi, paymentsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCurrency, formatDateOnly } from '../utils/format'

export function PaymentsPage() {
  const queryClient = useQueryClient()
  const [selectedPayment, setSelectedPayment] = useState(null)
  const [modalType, setModalType] = useState(null)
  const [rejectReason, setRejectReason] = useState('')
  const [toast, setToast] = useState(null)

  const currentUser = JSON.parse(localStorage.getItem('currentUser') ?? '{}')
  const roles = currentUser.roles ?? currentUser.Roles ?? []
  const isAdmin = roles.includes('Admin')

  const invoicesQuery = useQuery({
    queryKey: ['invoices-for-payments'],
    queryFn: () => invoicesApi.list({ page: 1, pageSize: 20 }),
  })
  const firstInvoiceId = invoicesQuery.data?.items?.[0]?.id
  const paymentsQuery = useQuery({
    queryKey: ['payments', firstInvoiceId],
    queryFn: () => paymentsApi.byInvoice(firstInvoiceId),
    enabled: Boolean(firstInvoiceId),
  })

  const refreshPayments = () => {
    queryClient.invalidateQueries({ queryKey: ['payments', firstInvoiceId] })
    queryClient.invalidateQueries({ queryKey: ['invoices-for-payments'] })
  }

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

  function openConfirmModal(payment) {
    setSelectedPayment(payment)
    setModalType('confirm')
    setToast(null)
  }

  function openRejectModal(payment) {
    setSelectedPayment(payment)
    setRejectReason('')
    setModalType('reject')
    setToast(null)
  }

  function closeModal() {
    setSelectedPayment(null)
    setModalType(null)
    setRejectReason('')
  }

  function submitReject(event) {
    event.preventDefault()
    rejectMutation.mutate({ id: selectedPayment.id, reason: rejectReason })
  }

  if (invoicesQuery.isLoading) return <LoadingState />
  if (invoicesQuery.isError) return <ErrorState message={getErrorMessage(invoicesQuery.error)} />
  if (!firstInvoiceId) return <EmptyState text="Chưa có hóa đơn để xem thanh toán." />

  const payments = paymentsQuery.data ?? []
  const isSubmitting = confirmMutation.isPending || rejectMutation.isPending

  return (
    <section>
      <PageHeader title="Thanh toán" description="Theo dõi các khoản thanh toán theo hóa đơn gần nhất" />

      {toast ? (
        <div className={`toast-message ${toast.type === 'error' ? 'error' : 'success'}`} role="status">
          {toast.message}
        </div>
      ) : null}

      <div className="data-table-card">
        <div className="table-toolbar">
          <div>
            <h3>Lịch sử thanh toán</h3>
            <p>{payments.length} khoản thanh toán của hóa đơn gần nhất</p>
          </div>
          <div className="table-filters">
            <input type="search" placeholder="Tìm phương thức, trạng thái..." aria-label="Tìm kiếm thanh toán" />
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
                      <td>{formatDateOnly(payment.paymentDate)}</td>
                      <td>
                        {canReview ? (
                          <div className="row-actions">
                            <button type="button" className="table-action" onClick={() => openConfirmModal(payment)}>
                              Xác nhận
                            </button>
                            <button type="button" className="table-action danger" onClick={() => openRejectModal(payment)}>
                              Từ chối
                            </button>
                          </div>
                        ) : (
                          <span className="muted">Không có thao tác</span>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>

      {modalType === 'confirm' && selectedPayment ? (
        <div className="modal-backdrop" role="presentation">
          <div className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="confirm-payment-title">
            <div className="modal-header">
              <div>
                <h3 id="confirm-payment-title">Xác nhận thanh toán</h3>
                <p>Bạn có chắc chắn muốn xác nhận khoản thanh toán này?</p>
              </div>
            </div>
            <div className="payment-review-summary">
              <span>Hóa đơn</span>
              <strong>{selectedPayment.invoiceNumber}</strong>
              <span>Số tiền</span>
              <strong>{formatCurrency(selectedPayment.amount)}</strong>
            </div>
            <div className="modal-footer">
              <button type="button" className="secondary" onClick={closeModal} disabled={isSubmitting}>Hủy</button>
              <button type="button" onClick={() => confirmMutation.mutate(selectedPayment.id)} disabled={isSubmitting}>
                {confirmMutation.isPending ? 'Đang xác nhận...' : 'Xác nhận'}
              </button>
            </div>
          </div>
        </div>
      ) : null}

      {modalType === 'reject' && selectedPayment ? (
        <div className="modal-backdrop" role="presentation">
          <form className="modal-panel" role="dialog" aria-modal="true" aria-labelledby="reject-payment-title" onSubmit={submitReject}>
            <div className="modal-header">
              <div>
                <h3 id="reject-payment-title">Từ chối thanh toán</h3>
                <p>Nhập lý do để lưu lại lịch sử xử lý thanh toán.</p>
              </div>
            </div>
            <label className="field">
              <span>Lý do từ chối <strong>*</strong></span>
              <textarea
                value={rejectReason}
                onChange={(event) => setRejectReason(event.target.value)}
                placeholder="Ví dụ: Minh chứng thanh toán không hợp lệ"
                required
              />
            </label>
            <div className="modal-footer">
              <button type="button" className="secondary" onClick={closeModal} disabled={isSubmitting}>Hủy</button>
              <button type="submit" className="danger-button" disabled={isSubmitting || !rejectReason.trim()}>
                {rejectMutation.isPending ? 'Đang từ chối...' : 'Từ chối'}
              </button>
            </div>
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
