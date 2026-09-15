import { useEffect, useState } from 'react'
import { actualizarUsuario, crearUsuario, eliminarUsuario, getRoles, getUsuarios } from '../api'
import type { Rol, Usuario } from '../types'
import { useAuth } from '../context/AuthContext'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

interface FormState {
  id: number | null
  rolId: number | null
  nombre: string
  email: string
  password: string
  activo: boolean
}

function formVacio(rolPorDefecto: number | null): FormState {
  return { id: null, rolId: rolPorDefecto, nombre: '', email: '', password: '', activo: true }
}

export default function Usuarios() {
  const { usuario: usuarioActual } = useAuth()
  const [items, setItems] = useState<Usuario[]>([])
  const [roles, setRoles] = useState<Rol[]>([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [mostrarForm, setMostrarForm] = useState(false)
  const [form, setForm] = useState<FormState>(formVacio(null))
  const [guardando, setGuardando] = useState(false)

  async function cargar() {
    setCargando(true)
    setError(null)
    try {
      const [usuarios, listaRoles] = await Promise.all([getUsuarios(), getRoles()])
      setItems(usuarios)
      setRoles(listaRoles)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargar()
  }, [])

  function abrirNuevo() {
    setForm(formVacio(roles[0]?.id ?? null))
    setMostrarForm(true)
  }

  function abrirEditar(usuario: Usuario) {
    setForm({
      id: usuario.id,
      rolId: usuario.rolId,
      nombre: usuario.nombre,
      email: usuario.email,
      password: '',
      activo: usuario.activo,
    })
    setMostrarForm(true)
  }

  async function guardar() {
    if (!form.nombre.trim() || !form.email.trim() || form.rolId === null) return
    if (form.id === null && !form.password.trim()) return

    setGuardando(true)
    setError(null)
    try {
      if (form.id === null) {
        await crearUsuario({
          rolId: form.rolId,
          nombre: form.nombre.trim(),
          email: form.email.trim(),
          password: form.password,
        })
      } else {
        await actualizarUsuario({
          id: form.id,
          rolId: form.rolId,
          nombre: form.nombre.trim(),
          email: form.email.trim(),
          activo: form.activo,
          password: form.password.trim() || null,
        })
      }
      setMostrarForm(false)
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setGuardando(false)
    }
  }

  async function eliminar(usuario: Usuario) {
    if (!confirm(`¿Desactivar a "${usuario.nombre}"?`)) return
    setError(null)
    try {
      await eliminarUsuario(usuario.id)
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const formValido = form.nombre.trim() && form.email.trim() && form.rolId !== null && (form.id !== null || form.password.trim())

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Usuarios</h1>
          <p className="page-subtitle">Quién puede entrar a la app y qué rol/permisos tiene</p>
        </div>
        <button className="btn btn-primary" onClick={abrirNuevo}>
          + Nuevo usuario
        </button>
      </div>

      <ErrorAlert message={error} />

      {mostrarForm && (
        <div className="card">
          <h2>{form.id === null ? 'Nuevo usuario' : 'Editar usuario'}</h2>
          <div className="form-grid">
            <div className="field">
              <label>Nombre</label>
              <input value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
            </div>
            <div className="field">
              <label>Email</label>
              <input
                type="email"
                value={form.email}
                onChange={(e) => setForm({ ...form, email: e.target.value })}
              />
            </div>
            <div className="field">
              <label>Rol</label>
              <select
                value={form.rolId ?? ''}
                onChange={(e) => setForm({ ...form, rolId: e.target.value ? Number(e.target.value) : null })}
              >
                <option value="" disabled>
                  Selecciona un rol…
                </option>
                {roles.map((rol) => (
                  <option key={rol.id} value={rol.id}>
                    {rol.nombre}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label>{form.id === null ? 'Contraseña' : 'Nueva contraseña (opcional)'}</label>
              <input
                type="password"
                value={form.password}
                onChange={(e) => setForm({ ...form, password: e.target.value })}
                placeholder={form.id === null ? '' : 'Dejar en blanco para no cambiarla'}
              />
            </div>
            {form.id !== null && (
              <div className="field field-checkbox">
                <input
                  type="checkbox"
                  id="usuario-activo"
                  checked={form.activo}
                  onChange={(e) => setForm({ ...form, activo: e.target.checked })}
                />
                <label htmlFor="usuario-activo">Activo</label>
              </div>
            )}
          </div>
          <div className="form-actions">
            <button className="btn btn-primary" onClick={guardar} disabled={guardando || !formValido}>
              {guardando ? 'Guardando…' : 'Guardar'}
            </button>
            <button className="btn" onClick={() => setMostrarForm(false)} disabled={guardando}>
              Cancelar
            </button>
          </div>
        </div>
      )}

      <div className="card">
        {cargando ? (
          <p className="muted">Cargando…</p>
        ) : items.length === 0 ? (
          <p className="empty-row">No hay usuarios todavía.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Email</th>
                  <th>Rol</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {items.map((usuario) => (
                  <tr key={usuario.id}>
                    <td>{usuario.nombre}</td>
                    <td>{usuario.email}</td>
                    <td>{usuario.rolNombre}</td>
                    <td>
                      <span className={`badge ${usuario.activo ? 'badge-success' : 'badge-neutral'}`}>
                        {usuario.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td>
                      <button className="btn btn-sm" onClick={() => abrirEditar(usuario)}>
                        Editar
                      </button>{' '}
                      {usuario.id !== usuarioActual?.id && (
                        <button className="btn btn-sm btn-danger" onClick={() => eliminar(usuario)}>
                          Desactivar
                        </button>
                      )}
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
