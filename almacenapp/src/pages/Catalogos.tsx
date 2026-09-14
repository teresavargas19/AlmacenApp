import { useEffect, useState } from 'react'
import {
  actualizarAlmacen,
  actualizarCategoria,
  actualizarUnidad,
  crearAlmacen,
  crearCategoria,
  crearUnidad,
  eliminarAlmacen,
  eliminarCategoria,
  eliminarUnidad,
  getAlmacenes,
  getCategorias,
  getUnidades,
} from '../api'
import type { Almacen, Categoria, UnidadMedida } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

type Tab = 'categorias' | 'unidades' | 'almacenes'

export default function Catalogos() {
  const [tab, setTab] = useState<Tab>('categorias')

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Catálogos</h1>
          <p className="page-subtitle">Categorías, unidades de medida y almacenes</p>
        </div>
      </div>

      <div className="tabs">
        <button className={`tab-btn${tab === 'categorias' ? ' active' : ''}`} onClick={() => setTab('categorias')}>
          Categorías
        </button>
        <button className={`tab-btn${tab === 'unidades' ? ' active' : ''}`} onClick={() => setTab('unidades')}>
          Unidades de medida
        </button>
        <button className={`tab-btn${tab === 'almacenes' ? ' active' : ''}`} onClick={() => setTab('almacenes')}>
          Almacenes
        </button>
      </div>

      {tab === 'categorias' && <CategoriasTab />}
      {tab === 'unidades' && <UnidadesTab />}
      {tab === 'almacenes' && <AlmacenesTab />}
    </div>
  )
}

function CategoriasTab() {
  const [items, setItems] = useState<Categoria[]>([])
  const [nombre, setNombre] = useState('')
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function cargar() {
    setCargando(true)
    setError(null)
    try {
      setItems(await getCategorias())
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargar()
  }, [])

  async function agregar() {
    if (!nombre.trim()) return
    setError(null)
    try {
      await crearCategoria({ nombre: nombre.trim(), activo: true })
      setNombre('')
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  async function alternarActivo(categoria: Categoria) {
    setError(null)
    try {
      if (categoria.activo) {
        await eliminarCategoria(categoria.id)
      } else {
        await actualizarCategoria({ ...categoria, activo: true })
      }
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="card">
      <ErrorAlert message={error} />
      <div className="detail-line two-col" style={{ maxWidth: 520 }}>
        <div className="field">
          <label>Nueva categoría</label>
          <input value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Ej. Bebidas" />
        </div>
        <div />
        <button className="btn btn-primary" onClick={agregar} disabled={!nombre.trim()}>
          Agregar
        </button>
      </div>

      {cargando ? (
        <p className="muted">Cargando…</p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map((categoria) => (
                <tr key={categoria.id}>
                  <td>{categoria.nombre}</td>
                  <td>
                    <span className={`badge ${categoria.activo ? 'badge-success' : 'badge-neutral'}`}>
                      {categoria.activo ? 'Activa' : 'Inactiva'}
                    </span>
                  </td>
                  <td>
                    <button className="btn btn-sm" onClick={() => alternarActivo(categoria)}>
                      {categoria.activo ? 'Desactivar' : 'Reactivar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function UnidadesTab() {
  const [items, setItems] = useState<UnidadMedida[]>([])
  const [nombre, setNombre] = useState('')
  const [abreviatura, setAbreviatura] = useState('')
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function cargar() {
    setCargando(true)
    setError(null)
    try {
      setItems(await getUnidades())
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargar()
  }, [])

  async function agregar() {
    if (!nombre.trim() || !abreviatura.trim()) return
    setError(null)
    try {
      await crearUnidad({ nombre: nombre.trim(), abreviatura: abreviatura.trim(), activo: true })
      setNombre('')
      setAbreviatura('')
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  async function alternarActivo(unidad: UnidadMedida) {
    setError(null)
    try {
      if (unidad.activo) {
        await eliminarUnidad(unidad.id)
      } else {
        await actualizarUnidad({ ...unidad, activo: true })
      }
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="card">
      <ErrorAlert message={error} />
      <div className="detail-line" style={{ maxWidth: 620 }}>
        <div className="field">
          <label>Nombre</label>
          <input value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Ej. Caja" />
        </div>
        <div className="field">
          <label>Abreviatura</label>
          <input value={abreviatura} onChange={(e) => setAbreviatura(e.target.value)} placeholder="Ej. caja" />
        </div>
        <div />
        <button className="btn btn-primary" onClick={agregar} disabled={!nombre.trim() || !abreviatura.trim()}>
          Agregar
        </button>
      </div>

      {cargando ? (
        <p className="muted">Cargando…</p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Abreviatura</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map((unidad) => (
                <tr key={unidad.id}>
                  <td>{unidad.nombre}</td>
                  <td>{unidad.abreviatura}</td>
                  <td>
                    <span className={`badge ${unidad.activo ? 'badge-success' : 'badge-neutral'}`}>
                      {unidad.activo ? 'Activa' : 'Inactiva'}
                    </span>
                  </td>
                  <td>
                    <button className="btn btn-sm" onClick={() => alternarActivo(unidad)}>
                      {unidad.activo ? 'Desactivar' : 'Reactivar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}

function AlmacenesTab() {
  const [items, setItems] = useState<Almacen[]>([])
  const [nombre, setNombre] = useState('')
  const [direccion, setDireccion] = useState('')
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)

  async function cargar() {
    setCargando(true)
    setError(null)
    try {
      setItems(await getAlmacenes())
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargar()
  }, [])

  async function agregar() {
    if (!nombre.trim()) return
    setError(null)
    try {
      await crearAlmacen({ nombre: nombre.trim(), direccion: direccion.trim() || null, activo: true })
      setNombre('')
      setDireccion('')
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  async function alternarActivo(almacen: Almacen) {
    setError(null)
    try {
      if (almacen.activo) {
        await eliminarAlmacen(almacen.id)
      } else {
        await actualizarAlmacen({ ...almacen, activo: true })
      }
      await cargar()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div className="card">
      <ErrorAlert message={error} />
      <div className="detail-line" style={{ maxWidth: 620 }}>
        <div className="field">
          <label>Nombre</label>
          <input value={nombre} onChange={(e) => setNombre(e.target.value)} placeholder="Ej. Almacén Norte" />
        </div>
        <div className="field">
          <label>Dirección (opcional)</label>
          <input value={direccion} onChange={(e) => setDireccion(e.target.value)} />
        </div>
        <div />
        <button className="btn btn-primary" onClick={agregar} disabled={!nombre.trim()}>
          Agregar
        </button>
      </div>

      {cargando ? (
        <p className="muted">Cargando…</p>
      ) : (
        <div className="table-wrap">
          <table className="table">
            <thead>
              <tr>
                <th>Nombre</th>
                <th>Dirección</th>
                <th>Estado</th>
                <th></th>
              </tr>
            </thead>
            <tbody>
              {items.map((almacen) => (
                <tr key={almacen.id}>
                  <td>{almacen.nombre}</td>
                  <td>{almacen.direccion ?? '—'}</td>
                  <td>
                    <span className={`badge ${almacen.activo ? 'badge-success' : 'badge-neutral'}`}>
                      {almacen.activo ? 'Activo' : 'Inactivo'}
                    </span>
                  </td>
                  <td>
                    <button className="btn btn-sm" onClick={() => alternarActivo(almacen)}>
                      {almacen.activo ? 'Desactivar' : 'Reactivar'}
                    </button>
                  </td>
                </tr>
              ))}
            </tbody>
          </table>
        </div>
      )}
    </div>
  )
}
