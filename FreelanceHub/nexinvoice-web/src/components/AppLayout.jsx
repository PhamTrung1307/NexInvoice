import { useMemo, useState } from 'react'
import { NavLink, Outlet, useLocation, useNavigate } from 'react-router-dom'

const navItems = [
  { label: 'Tổng quan', path: '/dashboard', icon: 'TQ' },
  { label: 'Khách hàng', path: '/clients', icon: 'KH' },
  { label: 'Dự án', path: '/projects', icon: 'DA' },
  { label: 'Công việc', path: '/tasks', icon: 'CV' },
  { label: 'Hợp đồng', path: '/contracts', icon: 'HD' },
  { label: 'Hóa đơn', path: '/invoices', icon: 'HĐ' },
  { label: 'Thanh toán', path: '/payments', icon: 'TT' },
  { label: 'Thông báo', path: '/notifications', icon: 'TB' },
  { label: 'Báo cáo', path: '/reports', icon: 'BC' },
  { label: 'Cài đặt', path: '/settings', icon: 'CD' },
]

const pageTitles = [
  { path: '/dashboard', title: 'Tổng quan', description: 'Theo dõi hoạt động kinh doanh và dòng tiền' },
  { path: '/clients', title: 'Khách hàng', description: 'Quản lý hồ sơ và trạng thái khách hàng' },
  { path: '/projects', title: 'Dự án', description: 'Theo dõi tiến độ và công việc theo dự án' },
  { path: '/tasks', title: 'Công việc', description: 'Quản lý công việc và mức độ ưu tiên' },
  { path: '/invoices', title: 'Hóa đơn', description: 'Theo dõi hóa đơn, hạn thanh toán và trạng thái' },
  { path: '/payments', title: 'Thanh toán', description: 'Kiểm soát thanh toán và đối soát doanh thu' },
  { path: '/notifications', title: 'Thông báo', description: 'Cập nhật các sự kiện quan trọng trong hệ thống' },
  { path: '/contracts', title: 'Hợp đồng', description: 'Quản lý hợp đồng khách hàng' },
  { path: '/reports', title: 'Báo cáo', description: 'Theo dõi doanh thu, hóa đơn và tiến độ dự án' },
  { path: '/settings', title: 'Cài đặt', description: 'Quản lý thông tin hệ thống và tài khoản' },
]

export function AppLayout() {
  const navigate = useNavigate()
  const location = useLocation()
  const [isSidebarOpen, setIsSidebarOpen] = useState(false)
  const [isUserMenuOpen, setIsUserMenuOpen] = useState(false)
  const currentUser = JSON.parse(localStorage.getItem('currentUser') ?? '{}')
  const displayName = currentUser.fullName ?? currentUser.email ?? 'Người dùng'
  const userEmail = currentUser.email ?? 'Chưa có email'
  const initials = getInitials(displayName)
  const pageTitle = useMemo(() => getPageTitle(location.pathname), [location.pathname])

  function logout() {
    localStorage.removeItem('accessToken')
    localStorage.removeItem('refreshToken')
    localStorage.removeItem('currentUser')
    navigate('/login')
  }

  return (
    <div className={`app-shell min-h-screen ${isSidebarOpen ? 'sidebar-open' : ''}`}>
      <button
        type="button"
        className="sidebar-backdrop"
        aria-label="Đóng menu"
        onClick={() => setIsSidebarOpen(false)}
      />

      <aside className="sidebar">
        <div className="brand">
          <span className="brand-mark">NI</span>
          <div>
            <strong>NexInvoice</strong>
            <small>Business Management System</small>
          </div>
        </div>

        <nav className="sidebar-nav" aria-label="Điều hướng chính">
          {navItems.map((item) =>
            item.disabled ? (
              <span key={item.path} className="sidebar-link disabled" aria-disabled="true">
                <span className="nav-icon">{item.icon}</span>
                <span>{item.label}</span>
              </span>
            ) : (
              <NavLink
                key={item.path}
                to={item.path}
                className={({ isActive }) => `sidebar-link${isActive ? ' active' : ''}`}
                onClick={() => setIsSidebarOpen(false)}
              >
                <span className="nav-icon">{item.icon}</span>
                <span>{item.label}</span>
              </NavLink>
            ),
          )}
        </nav>
      </aside>

      <div className="main-shell">
        <header className="topbar">
          <div className="topbar-title">
            <button
              type="button"
              className="icon-button mobile-menu-button"
              aria-label="Mở menu"
              onClick={() => setIsSidebarOpen(true)}
            >
              <span />
              <span />
              <span />
            </button>
            <div>
              <h1>{pageTitle.title}</h1>
              <p>{pageTitle.description}</p>
            </div>
          </div>

          <div className="topbar-actions">
            <button
              type="button"
              className="icon-button notification-button"
              aria-label="Thông báo"
              onClick={() => navigate('/notifications')}
            >
              <span className="notification-dot" />
              TB
            </button>

            <div className="user-menu">
              <button
                type="button"
                className="user-trigger"
                aria-expanded={isUserMenuOpen}
                onClick={() => setIsUserMenuOpen((value) => !value)}
              >
                <span className="avatar">{initials}</span>
                <span className="user-summary">
                  <strong>{displayName}</strong>
                  <small>{userEmail}</small>
                </span>
              </button>

              {isUserMenuOpen ? (
                <div className="user-dropdown">
                  <div className="user-dropdown-header">
                    <span className="avatar">{initials}</span>
                    <div>
                      <strong>{displayName}</strong>
                      <small>{userEmail}</small>
                    </div>
                  </div>
                  <button type="button" className="dropdown-action" onClick={logout}>
                    Đăng xuất
                  </button>
                </div>
              ) : null}
            </div>
          </div>
        </header>

        <main className="content">
          <Outlet />
        </main>
      </div>
    </div>
  )
}

function getPageTitle(pathname) {
  const match = pageTitles
    .filter((item) => pathname === item.path || pathname.startsWith(`${item.path}/`))
    .sort((a, b) => b.path.length - a.path.length)[0]

  return match ?? {
    title: 'NexInvoice',
    description: 'Quản lý khách hàng, dự án, hóa đơn và thanh toán',
  }
}

function getInitials(value) {
  return value
    .trim()
    .split(/\s+/)
    .slice(0, 2)
    .map((part) => part[0])
    .join('')
    .toUpperCase()
}
