import { useEffect, useState } from 'react'
import { actualizarCliente, crearCliente, eliminarCliente, getClientes } from '../api'
import type { Cliente } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

interface FormState {
  id: number | null
  nombre: string
  telefono: string
  email: string
  direccion: string
  activo: boolean
}

function formVacio(): FormState {
  return { id: null, nombre: '', telefono: '', email: '', direccion: '', activo: true }
}

export default function Clientes() {
  const [items, setItems] = useState<Cliente[]>([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [mostrarForm, setMostrarForm] = useState(false)
  const [form, setForm] = useState<FormState>(formVacio())
  const [guardando, setGuardando] = useState(false)

  async function cargar() {
    setCargando(true)
    setError(null)
    try {
      setItems(await getClientes())
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

  function abrirEditar(cliente: Cliente) {
    setForm({
      id: cliente.id,
      nombre: cliente.nombre,
      telefono: cliente.telefono ?? '',
      email: cliente.email ?? '',
      direccion: cliente.direccion ?? '',
      activo: cliente.activo,
    })
    setMostrarForm(true)
  }

  async function guardar() {
    if (!form.nombre.trim()) return
    setGuardando(true)
    setError(null)
    try {
      const payload = {
        nombre: form.nombre.trim(),
        telefono: form.telefono || null,
        email: form.email || null,
        direccion: form.direccion || null,
        activo: form.activo,
      }
      if (form.id === null) {
        await crearCliente(payload)
      } else {
        await actualizarCliente({ id: form.id, ...payload })
      }
      setMostrarForm(false)
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setGuardando(false)
    }
  }

  async function eliminar(cliente: Cliente) {
    if (!confirm(`¿Desactivar a "${cliente.nombre}"?`)) return
    setError(null)
    try {
      await eliminarCliente(cliente.id)
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Clientes</h1>
          <p className="page-subtitle">A quién le despachas</p>
        </div>
        <button className="btn btn-primary" onClick={abrirNuevo}>
          + Nuevo cliente
        </button>
      </div>

      <ErrorAlert message={error} />

      {mostrarForm && (
        <div className="card">
          <h2>{form.id === null ? 'Nuevo cliente' : 'Editar cliente'}</h2>
          <div className="form-grid">
            <div className="field">
              <label>Nombre</label>
              <input value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
            </div>
            <div className="field">
              <label>Teléfono</label>
              <input value={form.telefono} onChange={(e) => setForm({ ...form, telefono: e.target.value })} />
            </div>
            <div className="field">
              <label>Email</label>
              <input value={form.email} onChange={(e) => setForm({ ...form, email: e.target.value })} />
            </div>
            <div className="field">
              <label>Dirección</label>
              <input value={form.direccion} onChange={(e) => setForm({ ...form, direccion: e.target.value })} />
            </div>
            {form.id !== null && (
              <div className="field field-checkbox">
                <input
                  type="checkbox"
                  id="cliente-activo"
                  checked={form.activo}
                  onChange={(e) => setForm({ ...form, activo: e.target.checked })}
                />
                <label htmlFor="cliente-activo">Activo</label>
              </div>
            )}
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
          <p className="empty-row">No hay clientes todavía.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Nombre</th>
                  <th>Teléfono</th>
                  <th>Email</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {items.map((cliente) => (
                  <tr key={cliente.id}>
                    <td>{cliente.nombre}</td>
                    <td>{cliente.telefono ?? '—'}</td>
                    <td>{cliente.email ?? '—'}</td>
                    <td>
                      <span className={`badge ${cliente.activo ? 'badge-success' : 'badge-neutral'}`}>
                        {cliente.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td>
                      <button className="btn btn-sm" onClick={() => abrirEditar(cliente)}>
                        Editar
                      </button>{' '}
                      <button className="btn btn-sm btn-danger" onClick={() => eliminar(cliente)}>
                        Desactivar
                      </button>
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
