import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { useParams } from 'react-router-dom'
import { invoicesApi, paymentsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { formatCurrency, formatDateOnly } from '../utils/format'

export function InvoiceDetailPage() {
  const { id } = useParams()
  const queryClient = useQueryClient()
  const invoiceQuery = useQuery({ queryKey: ['invoice', id], queryFn: () => invoicesApi.detail(id) })
  const paymentsQuery = useQuery({ queryKey: ['invoice-payments', id], queryFn: () => paymentsApi.byInvoice(id) })
  const action = useMutation({
    mutationFn: (fn) => fn(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['invoice', id] })
      queryClient.invalidateQueries({ queryKey: ['invoices'] })
    },
  })

  if (invoiceQuery.isLoading) return <LoadingState />
  if (invoiceQuery.isError) return <ErrorState message={getErrorMessage(invoiceQuery.error)} />

  const invoice = invoiceQuery.data

  return (
    <section>
      <PageHeader title={`Hóa đơn ${invoice.invoiceNumber}`} description={`${invoice.clientName} · ${invoice.projectName ?? 'Không có dự án'}`} />
      <div className="toolbar">
        <button type="button" onClick={() => action.mutate(invoicesApi.send)}>Gửi</button>
        <button type="button" onClick={() => action.mutate(invoicesApi.markPaid)}>Đánh dấu đã thanh toán</button>
        <button type="button" onClick={() => action.mutate(invoicesApi.cancel)}>Hủy</button>
        <a className="button secondary" href={invoicesApi.pdfUrl(id)} target="_blank" rel="noreferrer">Tải PDF</a>
      </div>
      {action.isError ? <div className="form-error">{getErrorMessage(action.error)}</div> : null}
      <div className="metric-grid">
        <article className="metric-card"><span>Ngày phát hành</span><strong>{formatDateOnly(invoice.issueDate)}</strong></article>
        <article className="metric-card"><span>Ngày đến hạn</span><strong>{formatDateOnly(invoice.dueDate)}</strong></article>
        <article className="metric-card"><span>Tạm tính</span><strong>{formatCurrency(invoice.subtotal)}</strong></article>
        <article className="metric-card"><span>Tổng cộng</span><strong>{formatCurrency(invoice.totalAmount)}</strong></article>
      </div>
      <article className="panel">
        <h3>Dòng chi tiết</h3>
        <div className="simple-list">{invoice.items?.map((item) => (
          <div key={item.id}><span>{item.description}</span><strong>{formatCurrency(item.amount)}</strong></div>
        ))}</div>
      </article>
      <article className="panel">
        <h3>Lịch sử thanh toán</h3>
        {paymentsQuery.isLoading ? <LoadingState /> : (
          <div className="simple-list">{(paymentsQuery.data ?? []).map((payment) => (
            <div key={payment.id}><span>{formatDateOnly(payment.paymentDate)} · {payment.method}</span><strong>{formatCurrency(payment.amount)}</strong></div>
          ))}</div>
        )}
      </article>
    </section>
  )
}
