import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { Link, useNavigate, useParams } from 'react-router-dom'
import { clientsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'

const emptyClient = {
  fullName: '',
  email: '',
  phoneNumber: '',
  companyName: '',
  address: '',
  status: 1,
}

export function ClientFormPage() {
  const { id } = useParams()
  const isEdit = Boolean(id)
  const navigate = useNavigate()
  const queryClient = useQueryClient()
  const [form, setForm] = useState(emptyClient)
  const [errors, setErrors] = useState({})

  const query = useQuery({
    queryKey: ['client', id],
    queryFn: () => clientsApi.detail(id),
    enabled: isEdit,
  })

  useEffect(() => {
    if (query.data) {
      setForm({
        fullName: query.data.fullName ?? '',
        email: query.data.email ?? '',
        phoneNumber: query.data.phoneNumber ?? '',
        companyName: query.data.companyName ?? '',
        address: query.data.address ?? '',
        status: query.data.status ?? 1,
      })
    }
  }, [query.data])

  const save = useMutation({
    mutationFn: (payload) => isEdit ? clientsApi.update(id, payload) : clientsApi.create(payload),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['clients'] })
      navigate('/clients')
    },
  })

  function submit(event) {
    event.preventDefault()

    const nextErrors = validateClient(form)
    setErrors(nextErrors)

    if (Object.keys(nextErrors).length > 0) return

    save.mutate({ ...form, status: Number(form.status) })
  }

  function updateField(field, value) {
    setForm({ ...form, [field]: value })
    if (errors[field]) {
      setErrors({ ...errors, [field]: undefined })
    }
  }

  if (query.isLoading) return <LoadingState />
  if (query.isError) return <ErrorState message={getErrorMessage(query.error)} />

  return (
    <section>
      <PageHeader
        title={isEdit ? 'Cập nhật khách hàng' : 'Tạo mới khách hàng'}
        description="Nhập thông tin khách hàng bằng tiếng Việt"
        action={<Link className="button secondary" to="/clients">Hủy</Link>}
      />

      <form className="form-card form-shell" onSubmit={submit} noValidate>
        <div className="form-card-header">
          <div>
            <h3>{isEdit ? 'Thông tin khách hàng' : 'Khách hàng mới'}</h3>
            <p>Các trường có dấu * là bắt buộc.</p>
          </div>
        </div>

        <div className="form-grid two-columns">
          <label className="field">
            <span>Họ tên <strong>*</strong></span>
            <input
              value={form.fullName}
              onChange={(event) => updateField('fullName', event.target.value)}
              aria-invalid={Boolean(errors.fullName)}
              aria-describedby={errors.fullName ? 'fullName-error' : undefined}
              required
            />
            {errors.fullName ? <small className="field-error" id="fullName-error">{errors.fullName}</small> : null}
          </label>

          <label className="field">
            <span>Email <strong>*</strong></span>
            <input
              type="email"
              value={form.email}
              onChange={(event) => updateField('email', event.target.value)}
              aria-invalid={Boolean(errors.email)}
              aria-describedby={errors.email ? 'email-error' : undefined}
              required
            />
            {errors.email ? <small className="field-error" id="email-error">{errors.email}</small> : null}
          </label>

          <label className="field">
            <span>Điện thoại</span>
            <input value={form.phoneNumber} onChange={(event) => updateField('phoneNumber', event.target.value)} />
          </label>

          <label className="field">
            <span>Công ty</span>
            <input value={form.companyName} onChange={(event) => updateField('companyName', event.target.value)} />
          </label>

          <label className="field full-span">
            <span>Địa chỉ</span>
            <textarea value={form.address} onChange={(event) => updateField('address', event.target.value)} />
          </label>

          <label className="field">
            <span>Trạng thái</span>
            <select value={form.status} onChange={(event) => updateField('status', event.target.value)}>
              <option value={1}>Đang hoạt động</option>
              <option value={2}>Ngưng hoạt động</option>
              <option value={3}>Lưu trữ</option>
            </select>
          </label>
        </div>

        {save.isError ? <div className="form-error">{getErrorMessage(save.error)}</div> : null}

        <div className="form-actions">
          <Link className="button secondary" to="/clients">Hủy</Link>
          <button type="submit" disabled={save.isPending}>
            {save.isPending ? 'Đang lưu...' : isEdit ? 'Cập nhật' : 'Tạo mới'}
          </button>
        </div>
      </form>
    </section>
  )
}

function validateClient(form) {
  const errors = {}

  if (!form.fullName.trim()) {
    errors.fullName = 'Vui lòng nhập họ tên.'
  }

  if (!form.email.trim()) {
    errors.email = 'Vui lòng nhập email.'
  } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(form.email)) {
    errors.email = 'Email không hợp lệ.'
  }

  return errors
}
