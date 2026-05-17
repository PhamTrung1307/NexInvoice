import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link } from 'react-router-dom'
import { clientsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { formatDate } from '../utils/format'

export function ClientsPage() {
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['clients'], queryFn: () => clientsApi.list({ page: 1, pageSize: 20 }) })
  const remove = useMutation({
    mutationFn: clientsApi.remove,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['clients'] }),
  })

  if (query.isLoading) return <LoadingState />
  if (query.isError) return <ErrorState message={getErrorMessage(query.error)} />

  const clients = query.data?.items ?? []
  const totalItems = query.data?.totalItems ?? clients.length
  const pageSize = query.data?.pageSize ?? 20

  return (
    <section>
      <PageHeader
        title="Khách hàng"
        description="Quản lý hồ sơ khách hàng và thông tin liên hệ"
        action={<Link className="button" to="/clients/new">Thêm khách hàng</Link>}
      />

      <div className="data-table-card">
        <div className="table-toolbar">
          <div>
            <h3>Danh sách khách hàng</h3>
            <p>{totalItems} khách hàng trong hệ thống</p>
          </div>
          <div className="table-filters">
            <input type="search" placeholder="Tìm theo tên, email, công ty..." aria-label="Tìm kiếm khách hàng" />
          </div>
        </div>

        {clients.length === 0 ? <EmptyState text="Chưa có khách hàng." /> : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Họ tên</th>
                  <th>Email</th>
                  <th>Điện thoại</th>
                  <th>Công ty</th>
                  <th>Ngày tạo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {clients.map((client) => (
                  <tr key={client.id}>
                    <td>
                      <strong className="table-primary">{client.fullName}</strong>
                    </td>
                    <td>{client.email}</td>
                    <td>{client.phoneNumber ?? '-'}</td>
                    <td>{client.companyName ?? '-'}</td>
                    <td>{formatDate(client.createdAt)}</td>
                    <td className="row-actions">
                      <Link className="table-action" to={`/clients/${client.id}/edit`}>Sửa</Link>
                      <button className="table-action danger" type="button" onClick={() => remove.mutate(client.id)}>Xóa</button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}

        <div className="table-pagination">
          <span>Hiển thị {clients.length} / {totalItems}</span>
          <div>
            <button type="button" className="secondary" disabled>Trước</button>
            <span>Trang 1</span>
            <button type="button" className="secondary" disabled={clients.length < pageSize}>Sau</button>
          </div>
        </div>
      </div>
    </section>
  )
}
