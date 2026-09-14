import { useEffect, useState } from 'react'
import {
  actualizarProducto,
  crearProducto,
  eliminarProducto,
  getCategorias,
  getProductos,
  getUnidades,
} from '../api'
import type { Categoria, Producto, UnidadMedida } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

interface FormState {
  id: number | null
  sku: string
  nombre: string
  descripcion: string
  categoriaId: number
  unidadMedidaId: number
  stockMinimo: number
  precio: number
  activo: boolean
}

function formVacio(categorias: Categoria[], unidades: UnidadMedida[]): FormState {
  return {
    id: null,
    sku: '',
    nombre: '',
    descripcion: '',
    categoriaId: categorias[0]?.id ?? 0,
    unidadMedidaId: unidades[0]?.id ?? 0,
    stockMinimo: 0,
    precio: 0,
    activo: true,
  }
}

export default function Productos() {
  const [productos, setProductos] = useState<Producto[]>([])
  const [categorias, setCategorias] = useState<Categoria[]>([])
  const [unidades, setUnidades] = useState<UnidadMedida[]>([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [mostrarForm, setMostrarForm] = useState(false)
  const [form, setForm] = useState<FormState | null>(null)
  const [guardando, setGuardando] = useState(false)

  async function cargarTodo() {
    setCargando(true)
    setError(null)
    try {
      const [prods, cats, unids] = await Promise.all([getProductos(), getCategorias(), getUnidades()])
      setProductos(prods)
      setCategorias(cats)
      setUnidades(unids)
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargarTodo()
  }, [])

  function nombreCategoria(id: number) {
    return categorias.find((categoria) => categoria.id === id)?.nombre ?? '—'
  }

  function nombreUnidad(id: number) {
    return unidades.find((unidad) => unidad.id === id)?.abreviatura ?? '—'
  }

  function abrirNuevo() {
    setForm(formVacio(categorias, unidades))
    setMostrarForm(true)
  }

  function abrirEditar(producto: Producto) {
    setForm({
      id: producto.id,
      sku: producto.sku,
      nombre: producto.nombre,
      descripcion: producto.descripcion ?? '',
      categoriaId: producto.categoriaId,
      unidadMedidaId: producto.unidadMedidaId,
      stockMinimo: producto.stockMinimo,
      precio: producto.precio,
      activo: producto.activo,
    })
    setMostrarForm(true)
  }

  async function guardar() {
    if (!form) return
    setGuardando(true)
    setError(null)
    try {
      if (form.id === null) {
        await crearProducto({
          sku: form.sku,
          nombre: form.nombre,
          descripcion: form.descripcion || null,
          categoriaId: form.categoriaId,
          unidadMedidaId: form.unidadMedidaId,
          stockMinimo: form.stockMinimo,
          precio: form.precio,
          activo: form.activo,
        })
      } else {
        const original = productos.find((producto) => producto.id === form.id)
        await actualizarProducto({
          id: form.id,
          sku: form.sku,
          nombre: form.nombre,
          descripcion: form.descripcion || null,
          categoriaId: form.categoriaId,
          unidadMedidaId: form.unidadMedidaId,
          stock: original?.stock ?? 0,
          stockMinimo: form.stockMinimo,
          precio: form.precio,
          activo: form.activo,
          creadoEn: original?.creadoEn ?? new Date().toISOString(),
        })
      }
      setMostrarForm(false)
      setForm(null)
      await cargarTodo()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setGuardando(false)
    }
  }

  async function eliminar(producto: Producto) {
    if (!confirm(`¿Desactivar el producto "${producto.nombre}"?`)) return
    setError(null)
    try {
      await eliminarProducto(producto.id)
      await cargarTodo()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Productos</h1>
          <p className="page-subtitle">Catálogo de productos del almacén</p>
        </div>
        <button className="btn btn-primary" onClick={abrirNuevo} disabled={categorias.length === 0 || unidades.length === 0}>
          + Nuevo producto
        </button>
      </div>

      <ErrorAlert message={error} />

      {(categorias.length === 0 || unidades.length === 0) && !cargando && (
        <div className="alert alert-error">
          Antes de crear productos, agrega al menos una categoría y una unidad de medida en{' '}
          <strong>Catálogos</strong>.
        </div>
      )}

      {mostrarForm && form && (
        <div className="card">
          <h2>{form.id === null ? 'Nuevo producto' : 'Editar producto'}</h2>
          <div className="form-grid">
            <div className="field">
              <label>SKU</label>
              <input value={form.sku} onChange={(e) => setForm({ ...form, sku: e.target.value })} />
            </div>
            <div className="field">
              <label>Nombre</label>
              <input value={form.nombre} onChange={(e) => setForm({ ...form, nombre: e.target.value })} />
            </div>
            <div className="field">
              <label>Categoría</label>
              <select
                value={form.categoriaId}
                onChange={(e) => setForm({ ...form, categoriaId: Number(e.target.value) })}
              >
                {categorias.map((categoria) => (
                  <option key={categoria.id} value={categoria.id}>
                    {categoria.nombre}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label>Unidad de medida</label>
              <select
                value={form.unidadMedidaId}
                onChange={(e) => setForm({ ...form, unidadMedidaId: Number(e.target.value) })}
              >
                {unidades.map((unidad) => (
                  <option key={unidad.id} value={unidad.id}>
                    {unidad.nombre} ({unidad.abreviatura})
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label>Precio</label>
              <input
                type="number"
                min={0}
                step="0.01"
                value={form.precio}
                onChange={(e) => setForm({ ...form, precio: Number(e.target.value) })}
              />
            </div>
            <div className="field">
              <label>Stock mínimo (alerta)</label>
              <input
                type="number"
                min={0}
                value={form.stockMinimo}
                onChange={(e) => setForm({ ...form, stockMinimo: Number(e.target.value) })}
              />
            </div>
            <div className="field" style={{ gridColumn: '1 / -1' }}>
              <label>Descripción</label>
              <textarea
                rows={2}
                value={form.descripcion}
                onChange={(e) => setForm({ ...form, descripcion: e.target.value })}
              />
            </div>
            {form.id !== null && (
              <div className="field field-checkbox">
                <input
                  type="checkbox"
                  id="producto-activo"
                  checked={form.activo}
                  onChange={(e) => setForm({ ...form, activo: e.target.checked })}
                />
                <label htmlFor="producto-activo">Activo</label>
              </div>
            )}
          </div>
          <p className="muted">
            El stock real se calcula solo a partir de los movimientos de inventario (Entradas, Salidas, Compras,
            Salidas confirmadas) — regístralo desde la sección Movimientos.
          </p>
          <div className="form-actions">
            <button className="btn btn-primary" onClick={guardar} disabled={guardando || !form.sku || !form.nombre}>
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
        ) : productos.length === 0 ? (
          <p className="empty-row">No hay productos todavía.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Nombre</th>
                  <th>Categoría</th>
                  <th>Unidad</th>
                  <th className="num">Stock</th>
                  <th className="num">Mínimo</th>
                  <th className="num">Precio</th>
                  <th>Estado</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {productos.map((producto) => (
                  <tr key={producto.id}>
                    <td>{producto.sku}</td>
                    <td>{producto.nombre}</td>
                    <td>{nombreCategoria(producto.categoriaId)}</td>
                    <td>{nombreUnidad(producto.unidadMedidaId)}</td>
                    <td className={`num${producto.stock < producto.stockMinimo ? ' badge-warning' : ''}`}>
                      {producto.stock}
                    </td>
                    <td className="num">{producto.stockMinimo}</td>
                    <td className="num">{producto.precio.toFixed(2)}</td>
                    <td>
                      <span className={`badge ${producto.activo ? 'badge-success' : 'badge-neutral'}`}>
                        {producto.activo ? 'Activo' : 'Inactivo'}
                      </span>
                    </td>
                    <td>
                      <button className="btn btn-sm" onClick={() => abrirEditar(producto)}>
                        Editar
                      </button>{' '}
                      <button className="btn btn-sm btn-danger" onClick={() => eliminar(producto)}>
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
