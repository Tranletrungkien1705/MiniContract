import React, { useEffect, useState } from 'react'
import { Routes, Route, NavLink, Outlet } from 'react-router-dom'
import { api, fmtMoney, fmtDate, fmtDateTime, STATUS, ROLES } from './api'

function Badge({ text, css }) { return <span className={`badge ${css || 'secondary'}`}>{text}</span> }
function Flash({ msg }) { return msg ? <div className={`flash ${msg.ok ? 'ok' : 'err'}`}>{msg.text}</div> : null }
function Modal({ title, onClose, wide, children }) {
  return (
    <div className="modal-bg" onClick={onClose}>
      <div className="modal" style={wide ? { maxWidth: 760 } : undefined} onClick={e => e.stopPropagation()}>
        <div className="row" style={{ marginBottom: 12 }}><h2 style={{ flex: 1, margin: 0 }}>{title}</h2>
          <button className="btn gray sm" style={{ flex: 'none' }} onClick={onClose}>Đóng</button></div>
        {children}
      </div>
    </div>
  )
}
function Field({ label, children }) { return <div style={{ flex: 1 }}><label>{label}</label>{children}</div> }

function Layout() {
  return (
    <>
      <nav className="nav"><span className="brand">📝 MiniContract</span>
        <NavLink to="/" end>Tổng quan</NavLink><NavLink to="/contracts">Hợp đồng</NavLink></nav>
      <div className="wrap"><Outlet /></div>
    </>
  )
}

function Dashboard() {
  const [d, setD] = useState(null); const [cache, setCache] = useState('')
  useEffect(() => { api.dashboard().then(r => { setD(r.data); setCache(r.cache) }) }, [])
  if (!d) return <p className="muted">Đang tải…</p>
  return (
    <>
      <h1>Tổng quan hợp đồng {cache && <span className="pill">cache: {cache}</span>}</h1>
      <div className="grid kpis">
        <div className="kpi"><div className="v">{d.total}</div><div className="l">Tổng hợp đồng</div></div>
        <div className="kpi"><div className="v">{d.draft}</div><div className="l">Nháp</div></div>
        <div className="kpi"><div className="v" style={{ color: 'var(--warning)' }}>{d.awaitingSign}</div><div className="l">Chờ ký</div></div>
        <div className="kpi"><div className="v" style={{ color: 'var(--success)' }}>{d.completed}</div><div className="l">Hoàn tất</div></div>
        <div className="kpi"><div className="v" style={{ fontSize: 18, color: 'var(--success)' }}>{fmtMoney(d.totalValue)}</div><div className="l">Giá trị đã ký</div></div>
      </div>
    </>
  )
}

function Contracts() {
  const [rows, setRows] = useState([]); const [status, setStatus] = useState(''); const [q, setQ] = useState('')
  const [open, setOpen] = useState(null); const [creating, setCreating] = useState(false)
  const load = () => api.contracts(status === '' ? null : Number(status), q).then(r => setRows(r.data))
  useEffect(() => { load() }, [status])
  return (
    <>
      <div className="toolbar"><h1 style={{ margin: 0, flex: 'none' }}>Hợp đồng</h1><div className="sp" />
        <select style={{ maxWidth: 160 }} value={status} onChange={e => setStatus(e.target.value)}><option value="">— Trạng thái —</option>{STATUS.map((s, i) => <option key={i} value={i}>{s}</option>)}</select>
        <input style={{ maxWidth: 200 }} placeholder="Tìm tiêu đề/mã…" value={q} onChange={e => setQ(e.target.value)} onKeyDown={e => e.key === 'Enter' && load()} />
        <button className="btn ghost sm" style={{ flex: 'none' }} onClick={load}>Tìm</button>
        <button className="btn sm" style={{ flex: 'none' }} onClick={() => setCreating(true)}>+ Tạo HĐ</button></div>
      <div className="card" style={{ padding: 0, overflow: 'auto' }}>
        <table><thead><tr><th>Mã</th><th>Tiêu đề</th><th>Loại</th><th className="right">Giá trị</th><th className="right">Ký</th><th>Trạng thái</th></tr></thead>
          <tbody>{rows.map(c => (
            <tr key={c.id} style={{ cursor: 'pointer' }} onClick={() => setOpen(c.id)}>
              <td>{c.code}</td><td>{c.title}</td><td>{c.type || '—'}</td><td className="right">{fmtMoney(c.value)}</td>
              <td className="right">{c.signed}/{c.parties}</td><td><Badge text={c.statusText} css={c.statusCss} /></td></tr>))}
            {rows.length === 0 && <tr><td colSpan={6} className="muted" style={{ padding: 20 }}>Không có hợp đồng.</td></tr>}</tbody></table>
      </div>
      {open && <ContractDetail id={open} onClose={() => setOpen(null)} onChanged={load} />}
      {creating && <ContractForm onClose={() => setCreating(false)} onSaved={() => { setCreating(false); load() }} />}
    </>
  )
}

function ContractDetail({ id, onClose, onChanged }) {
  const [c, setC] = useState(null); const [msg, setMsg] = useState(null); const [otpFor, setOtpFor] = useState(null); const [otpCode, setOtpCode] = useState('')
  const load = () => api.contract(id).then(r => setC(r.data))
  useEffect(() => { load() }, [id])
  const flash = (ok, text) => { setMsg({ ok, text }); setTimeout(() => setMsg(null), 3500) }
  const act = async (fn, okmsg) => { try { const r = await fn(); flash(true, okmsg || r.data?.msg || 'OK'); load(); onChanged() } catch (e) { flash(false, e.message) } }
  const reqOtp = async (pid) => { try { const r = await api.otp(id, pid); setOtpFor(pid); setOtpCode(r.data.code); flash(true, `Mã OTP demo: ${r.data.code}`) } catch (e) { flash(false, e.message) } }
  if (!c) return <Modal title="…" onClose={onClose}><p className="muted">Đang tải…</p></Modal>
  return (
    <Modal title={`${c.code} — ${c.title}`} onClose={onClose} wide>
      <Flash msg={msg} />
      <div className="row" style={{ marginBottom: 8 }}><Badge text={c.statusText} css={c.statusCss} /><span className="pill" style={{ flex: 'none' }}>{fmtMoney(c.value)}</span></div>
      {c.body && <div className="card" style={{ background: '#f8fafc', whiteSpace: 'pre-wrap', fontSize: 13, maxHeight: 160, overflow: 'auto' }}>{c.body}</div>}
      <div className="section-t">Các bên tham gia</div>
      <table><thead><tr><th>Bên</th><th>Vai trò</th><th>Ký</th><th></th></tr></thead>
        <tbody>{c.parties.map(p => (
          <tr key={p.id}><td>{p.name}{p.taxCode ? ` (${p.taxCode})` : ''}</td><td>{p.role}</td>
            <td>{p.hasSigned ? <Badge text={`Đã ký · ${fmtDate(p.signedAt)}`} css="success" /> : <span className="muted">Chưa ký</span>}</td>
            <td className="right">{!p.hasSigned && (c.status === 1 || c.status === 2) && (
              <div className="row" style={{ gap: 4, justifyContent: 'flex-end' }}>
                <button className="btn sm" style={{ flex: 'none' }} onClick={() => act(() => api.signCks(id, p.id))}>Ký CKS</button>
                <button className="btn ghost sm" style={{ flex: 'none' }} onClick={() => reqOtp(p.id)}>OTP</button>
              </div>)}</td></tr>))}</tbody></table>
      {otpFor && (
        <div className="card" style={{ background: '#fffbeb', marginTop: 10 }}>
          <div className="row"><Field label={`Nhập OTP cho bên #${otpFor}`}><input value={otpCode} onChange={e => setOtpCode(e.target.value)} /></Field>
            <div style={{ flex: 'none', alignSelf: 'flex-end' }}><button className="btn sm" onClick={() => act(async () => { const r = await api.signOtp(id, otpFor, otpCode); setOtpFor(null); return r })}>Ký OTP</button></div></div>
        </div>
      )}
      {c.signatures.length > 0 && <><div className="section-t">Chữ ký</div><table><tbody>{c.signatures.map((s, i) => <tr key={i}><td>{s.signerName}</td><td>{s.method}</td><td className="muted">{s.certSubject || ''}</td><td>{fmtDateTime(s.signedAt)}</td></tr>)}</tbody></table></>}
      <div className="row" style={{ gap: 6, marginTop: 14 }}>
        {c.status === 0 && <button className="btn sm" onClick={() => act(() => api.send(id), 'Đã gửi ký.')}>Gửi ký</button>}
        {c.status !== 3 && c.status !== 4 && <button className="btn gray sm" onClick={() => act(() => api.cancel(id), 'Đã hủy.')}>Hủy</button>}
      </div>
    </Modal>
  )
}

function ContractForm({ onClose, onSaved }) {
  const [types, setTypes] = useState([])
  const [f, setF] = useState({ title: '', typeId: '', value: 0, body: '', note: '' })
  const [parties, setParties] = useState([{ name: '', taxCode: '', role: 0 }, { name: '', taxCode: '', role: 1 }])
  const [err, setErr] = useState('')
  useEffect(() => { api.types().then(r => setTypes(r.data)) }, [])
  const up = (k, v) => setF({ ...f, [k]: v })
  const setP = (i, k, v) => setParties(parties.map((p, j) => j === i ? { ...p, [k]: v } : p))
  const save = async () => {
    try {
      await api.create({ title: f.title, typeId: f.typeId ? Number(f.typeId) : null, value: Number(f.value), body: f.body, note: f.note,
        parties: parties.filter(p => p.name).map(p => ({ ...p, role: Number(p.role) })) })
      onSaved()
    } catch (e) { setErr(e.message) }
  }
  return (
    <Modal title="Tạo hợp đồng" onClose={onClose} wide>
      {err && <Flash msg={{ ok: false, text: err }} />}
      <div className="row"><Field label="Tiêu đề *"><input value={f.title} onChange={e => up('title', e.target.value)} /></Field>
        <Field label="Loại HĐ"><select value={f.typeId} onChange={e => up('typeId', e.target.value)}><option value="">—</option>{types.map(t => <option key={t.id} value={t.id}>{t.name}</option>)}</select></Field>
        <Field label="Giá trị"><input type="number" value={f.value} onChange={e => up('value', e.target.value)} /></Field></div>
      <Field label="Nội dung"><textarea rows={4} value={f.body} onChange={e => up('body', e.target.value)} /></Field>
      <div className="section-t">Các bên tham gia</div>
      {parties.map((p, i) => (
        <div className="row" key={i} style={{ marginBottom: 6 }}>
          <input placeholder="Tên" value={p.name} onChange={e => setP(i, 'name', e.target.value)} />
          <input placeholder="MST/CCCD" value={p.taxCode} onChange={e => setP(i, 'taxCode', e.target.value)} />
          <select value={p.role} onChange={e => setP(i, 'role', e.target.value)}>{ROLES.map((r, j) => <option key={j} value={j}>{r}</option>)}</select>
          {parties.length > 1 && <button className="btn gray sm" style={{ flex: 'none' }} onClick={() => setParties(parties.filter((_, j) => j !== i))}>×</button>}
        </div>))}
      <button className="btn ghost sm" onClick={() => setParties([...parties, { name: '', taxCode: '', role: 1 }])}>+ Thêm bên</button>
      <div style={{ marginTop: 16 }}><button className="btn" onClick={save}>Tạo (Nháp)</button></div>
    </Modal>
  )
}

export default function App() {
  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Dashboard />} />
        <Route path="contracts" element={<Contracts />} />
      </Route>
    </Routes>
  )
}
