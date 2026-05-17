import axios from 'axios'

const API_BASE_URL = import.meta.env.VITE_API_BASE_URL ?? 'https://localhost:7121'

export const http = axios.create({
  baseURL: API_BASE_URL,
})

http.interceptors.request.use((config) => {
  const token = localStorage.getItem('accessToken')

  if (token) {
    config.headers.Authorization = `Bearer ${token}`
  }

  return config
})

http.interceptors.response.use(
  (response) => response,
  (error) => {
    if (error.response?.status === 401) {
      localStorage.removeItem('accessToken')
      localStorage.removeItem('refreshToken')
      localStorage.removeItem('currentUser')
    }

    return Promise.reject(error)
  },
)

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
