import { useQuery } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { invoicesApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatCurrency, formatDateOnly } from '../utils/format'

export function InvoicesPage() {
  const query = useQuery({ queryKey: ['invoices'], queryFn: () => invoicesApi.list({ page: 1, pageSize: 20 }) })

  if (query.isLoading) return <LoadingState />
  if (query.isError) return <ErrorState message={getErrorMessage(query.error)} />

  const invoices = query.data?.items ?? []
  const totalItems = query.data?.totalItems ?? invoices.length
  const pageSize = query.data?.pageSize ?? 20

  return (
    <section>
      <PageHeader title="Hóa đơn" description="Theo dõi hóa đơn, hạn thanh toán và trạng thái" />

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
    </section>
  )
}
