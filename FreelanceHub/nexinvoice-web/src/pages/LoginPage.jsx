import { useState } from 'react'
import { useMutation } from '@tanstack/react-query'
import { useLocation, useNavigate } from 'react-router-dom'
import { authApi } from '../api/resources'
import { getErrorMessage } from '../api/http'

export function LoginPage() {
  const navigate = useNavigate()
  const location = useLocation()
  const [form, setForm] = useState({ email: '', password: '' })

  const mutation = useMutation({
    mutationFn: authApi.login,
    onSuccess: (data) => {
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
      navigate(location.state?.from?.pathname ?? '/dashboard', { replace: true })
    },
  })

  function submit(event) {
    event.preventDefault()
    mutation.mutate(form)
  }

  return (
    <main className="login-page">
      <form className="login-card" onSubmit={submit}>
        <div className="login-brand">
          <span className="login-brand-mark">NI</span>
          <div>
            <strong>NexInvoice</strong>
            <small>Business Management System</small>
          </div>
        </div>

        <div className="login-heading">
          <h1>Đăng nhập vào hệ thống</h1>
          <p>Vui lòng nhập thông tin tài khoản của bạn</p>
        </div>

        <label className="field">
          Email
          <input
            value={form.email}
            onChange={(event) => setForm({ ...form, email: event.target.value })}
            type="email"
            autoComplete="email"
            placeholder="name@company.com"
            required
          />
        </label>

        <label className="field">
          Mật khẩu
          <input
            value={form.password}
            onChange={(event) => setForm({ ...form, password: event.target.value })}
            type="password"
            autoComplete="current-password"
            placeholder="Nhập mật khẩu"
            required
          />
        </label>

        <div className="login-options">
          <label className="checkbox-field">
            <input type="checkbox" />
            <span>Ghi nhớ đăng nhập</span>
          </label>
          <a href="/login">Quên mật khẩu?</a>
        </div>

        {mutation.isError ? <div className="form-error">{getErrorMessage(mutation.error)}</div> : null}

        <button className="login-submit" type="submit" disabled={mutation.isPending}>
          {mutation.isPending ? 'Đang đăng nhập...' : 'Đăng nhập'}
        </button>
      </form>
    </main>
  )
}
