export function LoadingState({ text = 'Đang tải dữ liệu...' }) {
  return (
    <div className="ui-state loading-state" role="status" aria-live="polite">
      <span className="state-icon spinner" aria-hidden="true" />
      <div>
        <strong>{text}</strong>
        <p>Vui lòng chờ trong giây lát.</p>
      </div>
    </div>
  )
}

export function ErrorState({ message = 'Không thể tải dữ liệu', onRetry }) {
  return (
    <div className="ui-state error-state" role="alert">
      <span className="state-icon" aria-hidden="true">!</span>
      <div>
        <strong>{message}</strong>
        <p>Vui lòng thử lại hoặc kiểm tra kết nối của bạn.</p>
        {onRetry ? <button type="button" className="secondary" onClick={onRetry}>Làm mới</button> : null}
      </div>
    </div>
  )
}

export function EmptyState({ title = 'Không có dữ liệu', text = 'Chưa có dữ liệu để hiển thị.', action }) {
  return (
    <div className="ui-state empty-state">
      <span className="state-icon" aria-hidden="true">+</span>
      <div>
        <strong>{title}</strong>
        <p>{text}</p>
        {action ? <div className="state-action">{action}</div> : null}
      </div>
    </div>
  )
}

export function SkeletonCard({ lines = 3 }) {
  return (
    <div className="skeleton-card" aria-hidden="true">
      <span />
      <strong />
      {Array.from({ length: lines }).map((_, index) => <p key={index} />)}
    </div>
  )
}

export function SkeletonTable({ rows = 5, columns = 5 }) {
  return (
    <div className="skeleton-table" style={{ '--skeleton-columns': columns }} aria-hidden="true">
      <div className="skeleton-table-header">
        {Array.from({ length: columns }).map((_, index) => <span key={index} />)}
      </div>
      {Array.from({ length: rows }).map((_, rowIndex) => (
        <div className="skeleton-table-row" key={rowIndex}>
          {Array.from({ length: columns }).map((_, columnIndex) => <span key={columnIndex} />)}
        </div>
      ))}
    </div>
  )
}
