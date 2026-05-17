import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { notificationsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { EmptyState, ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'
import { StatusBadge } from '../components/StatusBadge'
import { formatDate } from '../utils/format'

export function NotificationsPage() {
  const queryClient = useQueryClient()
  const query = useQuery({ queryKey: ['notifications'], queryFn: notificationsApi.list })
  const read = useMutation({
    mutationFn: notificationsApi.read,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  })
  const readAll = useMutation({
    mutationFn: notificationsApi.readAll,
    onSuccess: () => queryClient.invalidateQueries({ queryKey: ['notifications'] }),
  })

  if (query.isLoading) return <LoadingState />
  if (query.isError) return <ErrorState message={getErrorMessage(query.error)} />

  const notifications = query.data ?? []

  return (
    <section>
      <PageHeader
        title="Thông báo"
        description="Các cập nhật liên quan đến công việc, hóa đơn và thanh toán"
        action={<button type="button" onClick={() => readAll.mutate()}>Đánh dấu tất cả đã đọc</button>}
      />

      <div className="data-table-card">
        <div className="table-toolbar">
          <div>
            <h3>Danh sách thông báo</h3>
            <p>{notifications.length} thông báo trong hộp thư</p>
          </div>
          <div className="table-filters">
            <input type="search" placeholder="Tìm tiêu đề hoặc nội dung..." aria-label="Tìm kiếm thông báo" />
          </div>
        </div>

        {notifications.length === 0 ? <EmptyState text="Bạn chưa có thông báo." /> : (
          <div className="table-wrap">
            <table>
              <thead>
                <tr>
                  <th>Thông báo</th>
                  <th>Trạng thái</th>
                  <th>Thời gian</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {notifications.map((item) => (
                  <tr key={item.id}>
                    <td>
                      <strong className="table-primary">{item.title}</strong>
                      <span className="table-description">{item.message}</span>
                    </td>
                    <td>
                      <StatusBadge type="notification" status={item.isRead ? 'Read' : 'Unread'} />
                    </td>
                    <td>{formatDate(item.createdAt)}</td>
                    <td className="row-actions">
                      {!item.isRead ? <button className="table-action" type="button" onClick={() => read.mutate(item.id)}>Đã đọc</button> : null}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </section>
  )
}
