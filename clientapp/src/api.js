const base = '/api/v1'
async function req(path, opts = {}) {
  const res = await fetch(base + path, {
    headers: { 'Content-Type': 'application/json' }, credentials: 'same-origin',
    ...opts, body: opts.body ? JSON.stringify(opts.body) : undefined
  })
  const text = await res.text(); const data = text ? JSON.parse(text) : null
  if (!res.ok) throw new Error(data?.error || `Lỗi ${res.status}`)
  return { data, cache: res.headers.get('X-Cache') }
}
export const api = {
  dashboard: () => req('/dashboard'),
  types: () => req('/types'),
  contracts: (status, q) => req(`/contracts?${status != null ? `status=${status}&` : ''}${q ? `q=${encodeURIComponent(q)}` : ''}`),
  contract: (id) => req(`/contracts/${id}`),
  create: (b) => req('/contracts', { method: 'POST', body: b }),
  send: (id) => req(`/contracts/${id}/send`, { method: 'POST' }),
  cancel: (id) => req(`/contracts/${id}/cancel`, { method: 'POST' }),
  signCks: (id, pid) => req(`/contracts/${id}/parties/${pid}/sign-cks`, { method: 'POST' }),
  otp: (id, pid) => req(`/contracts/${id}/parties/${pid}/otp`, { method: 'POST' }),
  signOtp: (id, pid, code) => req(`/contracts/${id}/parties/${pid}/sign-otp`, { method: 'POST', body: { code } })
}
export const fmtMoney = (n) => (n ?? 0).toLocaleString('vi-VN') + ' ₫'
export const fmtDate = (s) => s ? new Date(s).toLocaleDateString('vi-VN') : '—'
export const fmtDateTime = (s) => s ? new Date(s).toLocaleString('vi-VN') : '—'
export const STATUS = ['Nháp', 'Đã gửi ký', 'Ký một phần', 'Hoàn tất', 'Đã hủy']
export const ROLES = ['Bên A', 'Bên B', 'Người làm chứng']
