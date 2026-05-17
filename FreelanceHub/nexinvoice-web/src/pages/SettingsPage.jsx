import { useEffect, useState } from 'react'
import { useMutation, useQuery, useQueryClient } from '@tanstack/react-query'
import { settingsApi } from '../api/resources'
import { getErrorMessage } from '../api/http'
import { ErrorState, LoadingState } from '../components/States'
import { PageHeader } from '../components/PageHeader'

const companyDefaults = {
  companyName: '',
  taxCode: '',
  email: '',
  phone: '',
  address: '',
  website: '',
  logoUrl: '',
}

const preferenceDefaults = {
  currency: 'VND',
  dateFormat: 'dd/MM/yyyy',
  timeZone: 'Asia/Ho_Chi_Minh',
  invoicePrefix: 'INV',
  defaultTaxRate: 10,
  paymentReminderDays: 3,
}

export function SettingsPage() {
  const queryClient = useQueryClient()
  const [company, setCompany] = useState(companyDefaults)
  const [preferences, setPreferences] = useState(preferenceDefaults)
  const [toast, setToast] = useState(null)

  const companyQuery = useQuery({ queryKey: ['settings', 'company'], queryFn: settingsApi.company })
  const preferencesQuery = useQuery({ queryKey: ['settings', 'preferences'], queryFn: settingsApi.preferences })

  useEffect(() => {
    if (companyQuery.data) setCompany(normalizeForm(companyDefaults, companyQuery.data))
  }, [companyQuery.data])

  useEffect(() => {
    if (preferencesQuery.data) setPreferences(normalizeForm(preferenceDefaults, preferencesQuery.data))
  }, [preferencesQuery.data])

  const companyMutation = useMutation({
    mutationFn: settingsApi.updateCompany,
    onSuccess: () => {
      setToast({ type: 'success', message: 'Cập nhật thông tin công ty thành công' })
      queryClient.invalidateQueries({ queryKey: ['settings', 'company'] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  const preferenceMutation = useMutation({
    mutationFn: settingsApi.updatePreferences,
    onSuccess: () => {
      setToast({ type: 'success', message: 'Cập nhật cấu hình hệ thống thành công' })
      queryClient.invalidateQueries({ queryKey: ['settings', 'preferences'] })
    },
    onError: (error) => setToast({ type: 'error', message: getErrorMessage(error) }),
  })

  if (companyQuery.isLoading || preferencesQuery.isLoading) return <LoadingState />
  if (companyQuery.isError) return <ErrorState message={getErrorMessage(companyQuery.error)} />
  if (preferencesQuery.isError) return <ErrorState message={getErrorMessage(preferencesQuery.error)} />

  return (
    <section>
      <PageHeader title="Cài đặt" description="Quản lý thông tin hệ thống và tài khoản" />

      {toast ? <div className={`toast-message ${toast.type}`}>{toast.message}</div> : null}

      <div className="two-column">
        <form className="form-card" onSubmit={(event) => { event.preventDefault(); companyMutation.mutate(company) }}>
          <div className="form-card-header">
            <div>
              <h3>Thông tin công ty</h3>
              <p>Thông tin hiển thị trên hợp đồng, hóa đơn và báo cáo</p>
            </div>
          </div>
          <div className="form-grid">
            <Field label="Tên công ty" required value={company.companyName} onChange={(value) => setCompany({ ...company, companyName: value })} />
            <Field label="Mã số thuế" value={company.taxCode} onChange={(value) => setCompany({ ...company, taxCode: value })} />
            <Field label="Email" type="email" value={company.email} onChange={(value) => setCompany({ ...company, email: value })} />
            <Field label="Số điện thoại" value={company.phone} onChange={(value) => setCompany({ ...company, phone: value })} />
            <Field label="Địa chỉ" value={company.address} onChange={(value) => setCompany({ ...company, address: value })} />
            <Field label="Website" value={company.website} onChange={(value) => setCompany({ ...company, website: value })} />
            <Field label="Logo URL" value={company.logoUrl} onChange={(value) => setCompany({ ...company, logoUrl: value })} />
          </div>
          <div className="form-actions"><button type="submit" disabled={companyMutation.isPending}>Lưu</button></div>
        </form>

        <form className="form-card" onSubmit={(event) => { event.preventDefault(); preferenceMutation.mutate({ ...preferences, defaultTaxRate: Number(preferences.defaultTaxRate), paymentReminderDays: Number(preferences.paymentReminderDays) }) }}>
          <div className="form-card-header">
            <div>
              <h3>Cấu hình hệ thống</h3>
              <p>Thiết lập định dạng và mặc định vận hành</p>
            </div>
          </div>
          <div className="form-grid">
            <Field label="Tiền tệ" required value={preferences.currency} onChange={(value) => setPreferences({ ...preferences, currency: value })} />
            <Field label="Định dạng ngày" required value={preferences.dateFormat} onChange={(value) => setPreferences({ ...preferences, dateFormat: value })} />
            <Field label="Múi giờ" required value={preferences.timeZone} onChange={(value) => setPreferences({ ...preferences, timeZone: value })} />
            <Field label="Tiền tố hóa đơn" required value={preferences.invoicePrefix} onChange={(value) => setPreferences({ ...preferences, invoicePrefix: value })} />
            <Field label="Thuế mặc định (%)" type="number" min="0" value={preferences.defaultTaxRate} onChange={(value) => setPreferences({ ...preferences, defaultTaxRate: value })} />
            <Field label="Ngày nhắc thanh toán" type="number" min="0" value={preferences.paymentReminderDays} onChange={(value) => setPreferences({ ...preferences, paymentReminderDays: value })} />
          </div>
          <div className="form-actions"><button type="submit" disabled={preferenceMutation.isPending}>Lưu</button></div>
        </form>
      </div>
    </section>
  )
}

function Field({ label, required, value, onChange, type = 'text', min }) {
  return (
    <label className="field">
      <span>{label}{required ? <strong>*</strong> : null}</span>
      <input required={required} type={type} min={min} value={value ?? ''} onChange={(event) => onChange(event.target.value)} />
    </label>
  )
}

function normalizeForm(defaults, data) {
  return Object.fromEntries(Object.keys(defaults).map((key) => [key, data[key] ?? defaults[key]]))
}
