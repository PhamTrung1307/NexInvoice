export function formatCurrency(value) {
  return new Intl.NumberFormat('vi-VN', {
    style: 'currency',
    currency: 'VND',
    maximumFractionDigits: 0,
  }).format(Number(value ?? 0))
}

export function formatCompactCurrency(value) {
  const amount = Number(value ?? 0)
  const absolute = Math.abs(amount)

  if (absolute >= 1_000_000_000) {
    return `${formatCompactNumber(amount / 1_000_000_000)} tỷ đ`
  }

  if (absolute >= 1_000_000) {
    return `${formatCompactNumber(amount / 1_000_000)} triệu đ`
  }

  return formatCurrency(amount)
}

function formatCompactNumber(value) {
  return new Intl.NumberFormat('vi-VN', {
    minimumFractionDigits: 0,
    maximumFractionDigits: 2,
  }).format(value)
}

export function formatDate(value) {
  if (!value) return 'Chưa có'
  return new Intl.DateTimeFormat('vi-VN').format(new Date(value))
}

export function formatDateOnly(value) {
  if (!value) return 'Chưa có'
  const [year, month, day] = value.split('-')
  return `${day}/${month}/${year}`
}

export function statusText(value) {
  const map = {
    1: 'Nháp',
    2: 'Đang hoạt động',
    3: 'Tạm dừng',
    4: 'Hoàn tất',
    5: 'Đã hủy',
    6: 'Đã hủy',
    Pending: 'Đang chờ',
    Confirmed: 'Đã xác nhận',
    Rejected: 'Bị từ chối',
  }

  return map[value] ?? value ?? 'Không rõ'
}
