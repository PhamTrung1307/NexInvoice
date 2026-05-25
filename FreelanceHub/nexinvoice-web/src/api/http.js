import axios from 'axios'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? '/api/v1'

export const http = axios.create({
  baseURL: API_BASE_URL,
})

let refreshTokenRequest = null

http.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

http.interceptors.response.use(
  (response) => response,
  async (error) => {
    const originalRequest = error.config

    if (
      error.response?.status === 401 &&
      originalRequest &&
      !originalRequest._retry &&
      !originalRequest.skipAuthRefresh &&
      !originalRequest.url?.includes('/auth/login') &&
      !originalRequest.url?.includes('/auth/refresh-token')
    ) {
      originalRequest._retry = true

      try {
        const refreshToken = localStorage.getItem('refreshToken')
        if (!refreshToken) throw error

        refreshTokenRequest ??= http
          .post('/auth/refresh-token', { refreshToken }, { skipAuthRefresh: true })
          .then((response) => response.data?.data ?? response.data)
          .finally(() => {
            refreshTokenRequest = null
          })

        const data = await refreshTokenRequest
        localStorage.setItem('accessToken', data.accessToken)
        localStorage.setItem('refreshToken', data.refreshToken)
        localStorage.setItem(
          'currentUser',
          JSON.stringify({
            id: data.userId,
            email: data.email,
            fullName: data.fullName,
            roles: data.roles,
          }),
        )

        originalRequest.headers.Authorization = `Bearer ${data.accessToken}`
        return http(originalRequest)
      } catch {
        clearSession()
      }
    } else if (error.response?.status === 401 && !originalRequest?.skipAuthRefresh) {
      clearSession()
    }

    return Promise.reject(error)
  },
)

export function clearSession() {
  localStorage.removeItem('accessToken')
  localStorage.removeItem('refreshToken')
  localStorage.removeItem('currentUser')
}

export function unwrap(response) {
  return response.data?.data ?? response.data
}

export function getErrorMessage(error) {
  return (
    error.response?.data?.message ??
    error.response?.data?.errors?.[0] ??
    'Đã xảy ra lỗi. Vui lòng thử lại.'
  )
}
