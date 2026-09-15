import { useEffect, useState } from 'react'
import { actualizarRol, crearRol, eliminarRol, getRoles } from '../api'
import type { Rol } from '../types'
import { MODULOS_PERMISOS, PERMISO_TODO, listaPermisos } from '../permisos'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

interface FormState {
  id: number | null
  nombre: string
  claves: Set<string>
}

function formVacio(): FormState {
  return { id: null, nombre: '', claves: new Set() }
}

/** El rol "Administrador" es especial: el backend exige exactamente ese
 * nombre para autorizar la gestión de usuarios y roles, así que no se puede
 * renombrar ni borrar desde aquí. */
const ROL_ADMIN = 'Administrador'

export default function Roles() {
  const [items, setItems] = useState<Rol[]>([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [mostrarForm, setMostrarForm] = useState(false)
  const [form, setForm] = useState<FormState>(formVacio())
  const [guardando, setGuardando] = useState(false)

  async function cargar() {
    setCargando(true)
    setError(null)
    try {
      setItems(await getRoles())
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
    setForm(formVacio())
    setMostrarForm(true)
  }

  function abrirEditar(rol: Rol) {
    const esTodo = (rol.permisos ?? '').trim() === PERMISO_TODO
    setForm({
      id: rol.id,
      nombre: rol.nombre,
      claves: esTodo ? new Set(MODULOS_PERMISOS.map((m) => m.clave)) : new Set(listaPermisos(rol.permisos)),
    })
    setMostrarForm(true)
  }

  function alternarPermiso(clave: string) {
    setForm((actual) => {
      const claves = new Set(actual.claves)
      if (claves.has(clave)) {
        claves.delete(clave)
      } else {
        claves.add(clave)
      }
      return { ...actual, claves }
    })
  }

  async function guardar() {
    if (!form.nombre.trim()) return
    setGuardando(true)
    setError(null)
    try {
      const todosMarcados = MODULOS_PERMISOS.every((m) => form.claves.has(m.clave))
      const permisos = todosMarcados ? PERMISO_TODO : Array.from(form.claves).join(',')

      if (form.id === null) {
        await crearRol({ nombre: form.nombre.trim(), permisos })
      } else {
        await actualizarRol({ id: form.id, nombre: form.nombre.trim(), permisos })
      }
      setMostrarForm(false)
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setGuardando(false)
    }
  }

  async function eliminar(rol: Rol) {
    if (!confirm(`¿Eliminar el rol "${rol.nombre}"?`)) return
    setError(null)
    try {
      await eliminarRol(rol.id)
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  const editandoAdmin = form.id !== null && items.find((r) => r.id === form.id)?.nombre === ROL_ADMIN

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Roles</h1>
          <p className="page-subtitle">Qué puede ver y hacer cada rol dentro de la app</p>
        </div>
        <button className="btn btn-primary" onClick={abrirNuevo}>
          + Nuevo rol
        </button>
      </div>

      <ErrorAlert message={error} />

      {mostrarForm && (
        <div className="card">
          <h2>{form.id === null ? 'Nuevo rol' : 'Editar rol'}</h2>
          <div className="form-grid">
            <div className="field">
              <label>Nombre</label>
              <input
                value={form.nombre}
                onChange={(e) => setForm({ ...form, nombre: e.target.value })}
                disabled={editandoAdmin}
              />
              {editandoAdmin && (
                <small className="muted">
                  El rol "{ROL_ADMIN}" no se puede renombrar: el backend depende de ese nombre exacto para dar
                  acceso a Usuarios y Roles.
                </small>
              )}
            </div>
          </div>

          <div className="field" style={{ marginTop: '0.5rem' }}>
            <label>Pantallas a las que tiene acceso este rol</label>
            <div className="checkbox-grid">
              {MODULOS_PERMISOS.map((modulo) => (
                <div className="field field-checkbox" key={modulo.clave}>
                  <input
                    type="checkbox"
                    id={`permiso-${modulo.clave}`}
                    checked={form.claves.has(modulo.clave)}
                    onChange={() => alternarPermiso(modulo.clave)}
                  />
                  <label htmlFor={`permiso-${modulo.clave}`}>{modulo.etiqueta}</label>
                </div>
              ))}
            </div>
            <small className="muted">
              El Dashboard (Inicio) siempre es visible para cualquier usuario que inicie sesión. "Usuarios" y
              "Roles" son exclusivos del rol {ROL_ADMIN} y no se asignan desde aquí.
            </small>
          </div>

          <div className="form-actions">
            <button className="btn btn-primary" onClick={guardar} disabled={guardando || !form.nombre.trim()}>
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
          <p className="empty-row">No hay roles todavía.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Permisos</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {items.map((rol) => {
                  const esTodo = (rol.permisos ?? '').trim() === PERMISO_TODO
                  const claves = esTodo ? [] : listaPermisos(rol.permisos)
                  return (
                    <tr key={rol.id}>
                      <td>{rol.nombre}</td>
                      <td>
                        {esTodo
                          ? 'Acceso total'
                          : claves.length === 0
                            ? '— (sin pantallas asignadas)'
                            : claves
                                .map((clave) => MODULOS_PERMISOS.find((m) => m.clave === clave)?.etiqueta ?? clave)
                                .join(', ')}
                      </td>
                      <td>
                        <button className="btn btn-sm" onClick={() => abrirEditar(rol)}>
                          Editar
                        </button>{' '}
                        {rol.nombre !== ROL_ADMIN && (
                          <button className="btn btn-sm btn-danger" onClick={() => eliminar(rol)}>
                            Eliminar
                          </button>
                        )}
                      </td>
                    </tr>
                  )
                })}
              </tbody>
            </table>
          </div>
        )}
      </div>
    </div>
  )
}
