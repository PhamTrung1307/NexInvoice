const statusConfig = {
  project: {
    Draft: { label: 'Bản nháp', tone: 'neutral' },
    InProgress: { label: 'Đang thực hiện', tone: 'info' },
    WaitingFeedback: { label: 'Chờ phản hồi', tone: 'warning' },
    Completed: { label: 'Hoàn thành', tone: 'success' },
    Cancelled: { label: 'Đã hủy', tone: 'danger' },
  },
  workItem: {
    Todo: { label: 'Việc cần làm', tone: 'neutral' },
    InProgress: { label: 'Đang làm', tone: 'info' },
    InReview: { label: 'Đang review', tone: 'purple' },
    Done: { label: 'Hoàn thành', tone: 'success' },
    Blocked: { label: 'Bị chặn', tone: 'danger' },
    Cancelled: { label: 'Đã hủy', tone: 'danger' },
  },
  invoice: {
    Draft: { label: 'Bản nháp', tone: 'neutral' },
    Sent: { label: 'Đã gửi', tone: 'info' },
    PartiallyPaid: { label: 'Thanh toán một phần', tone: 'warning' },
    Paid: { label: 'Đã thanh toán', tone: 'success' },
    Overdue: { label: 'Quá hạn', tone: 'warning' },
    Cancelled: { label: 'Đã hủy', tone: 'danger' },
  },
  payment: {
    Pending: { label: 'Chờ xác nhận', tone: 'warning' },
    Confirmed: { label: 'Đã xác nhận', tone: 'success' },
    Rejected: { label: 'Đã từ chối', tone: 'danger' },
  },
  contract: {
    Draft: { label: 'Bản nháp', tone: 'neutral' },
    Sent: { label: 'Đã gửi', tone: 'info' },
    Approved: { label: 'Đã phê duyệt', tone: 'success' },
    Rejected: { label: 'Đã từ chối', tone: 'danger' },
    Expired: { label: 'Hết hạn', tone: 'warning' },
  },
  notification: {
    Read: { label: 'Đã đọc', tone: 'neutral' },
    Unread: { label: 'Chưa đọc', tone: 'info' },
  },
}

const numericFallbacks = {
  project: {
    1: 'Draft',
    2: 'InProgress',
    3: 'WaitingFeedback',
    4: 'Completed',
    5: 'Cancelled',
  },
  invoice: {
    1: 'Draft',
    2: 'Sent',
    3: 'PartiallyPaid',
    4: 'Paid',
    5: 'Overdue',
    6: 'Cancelled',
  },
  payment: {
    1: 'Pending',
    2: 'Confirmed',
    3: 'Rejected',
  },
  workItem: {
    1: 'Todo',
    2: 'InProgress',
    3: 'InReview',
    4: 'Done',
    5: 'Cancelled',
  },
  contract: {
    1: 'Draft',
    2: 'Sent',
    3: 'Approved',
    4: 'Rejected',
    5: 'Expired',
  },
}

export function StatusBadge({ type, status, children }) {
  const normalized = normalizeStatus(type, status)
  const config = statusConfig[type]?.[normalized]
  const tone = config?.tone ?? 'neutral'
  const label = children ?? config?.label ?? status ?? 'Không rõ'

  return <span className={`status-badge tone-${tone}`}>{label}</span>
}

function normalizeStatus(type, status) {
  if (status === null || status === undefined) return undefined

  const raw = String(status)
  const mapped = numericFallbacks[type]?.[raw]
  if (mapped) return mapped

  return raw.replace(/\s+/g, '')
}
