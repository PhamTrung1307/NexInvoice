import { Navigate, useLocation } from 'react-router-dom'

export function RequireAuth({ children }) {
  const location = useLocation()
  const token = localStorage.getItem('accessToken')

  if (!token) {
    return <Navigate to="/login" replace state={{ from: location }} />
  }

  return children
}
