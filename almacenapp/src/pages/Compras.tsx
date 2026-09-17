import { useEffect, useState } from 'react'
import {
  cancelarCompra,
  confirmarCompra,
  crearCompra,
  getAlmacenes,
  getCompra,
  getCompras,
  getProductos,
  getProveedores,
} from '../api'
import type { Almacen, CompraDetail, CompraListItem, Producto, Proveedor } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'
import StatusBadge from '../components/StatusBadge'
import { exportarExcel } from '../exportExcel'

interface LineaForm {
  productoId: number | ''
  cantidad: string
  precioUnitario: string
}

function lineaVacia(): LineaForm {
  return { productoId: '', cantidad: '', precioUnitario: '' }
}

export default function Compras() {
  const [compras, setCompras] = useState<CompraListItem[]>([])
  const [proveedores, setProveedores] = useState<Proveedor[]>([])
  const [productos, setProductos] = useState<Producto[]>([])
  const [almacenes, setAlmacenes] = useState<Almacen[]>([])
  const [filtroEstado, setFiltroEstado] = useState('')
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [mensaje, setMensaje] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [proveedorId, setProveedorId] = useState<number | ''>('')
  const [observaciones, setObservaciones] = useState('')
  const [lineas, setLineas] = useState<LineaForm[]>([lineaVacia()])
  const [guardando, setGuardando] = useState(false)

  const [seleccionada, setSeleccionada] = useState<CompraDetail | null>(null)
  const [almacenConfirmar, setAlmacenConfirmar] = useState<number | ''>('')
  const [procesando, setProcesando] = useState(false)

  async function cargarCatalogos() {
    try {
      const [provs, prods, alms] = await Promise.all([getProveedores(), getProductos(), getAlmacenes()])
      setProveedores(provs)
      setProductos(prods)
      setAlmacenes(alms)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  async function cargarCompras() {
    setCargando(true)
    setError(null)
    try {
      setCompras(await getCompras(filtroEstado || undefined))
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargarCatalogos()
  }, [])

  useEffect(() => {
    cargarCompras()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [filtroEstado])

  function actualizarLinea(index: number, cambios: Partial<LineaForm>) {
    setLineas((prev) => prev.map((linea, i) => (i === index ? { ...linea, ...cambios } : linea)))
  }

  function agregarLinea() {
    setLineas((prev) => [...prev, lineaVacia()])
  }

  function quitarLinea(index: number) {
    setLineas((prev) => (prev.length > 1 ? prev.filter((_, i) => i !== index) : prev))
  }

  function exportarCompras() {
    exportarExcel(
      'compras',
      'Compras',
      compras.map((compra) => ({
        '#': compra.id,
        Proveedor: compra.proveedorNombre,
        Fecha: new Date(compra.fecha).toLocaleDateString(),
        Estado: compra.estado,
        Total: compra.total,
      })),
    )
  }

  const totalForm = lineas.reduce((acc, linea) => {
    const cantidad = Number(linea.cantidad) || 0
    const precio = Number(linea.precioUnitario) || 0
    return acc + cantidad * precio
  }, 0)

  async function guardarCompra() {
    if (proveedorId === '') return
    const detalles = lineas
      .filter((linea) => linea.productoId !== '' && Number(linea.cantidad) > 0)
      .map((linea) => ({
        productoId: linea.productoId as number,
        cantidad: Number(linea.cantidad),
        precioUnitario: Number(linea.precioUnitario) || 0,
      }))
    if (detalles.length === 0) return

    setGuardando(true)
    setError(null)
    try {
      await crearCompra({ proveedorId, observaciones: observaciones || null, detalles })
      setMensaje('Compra creada como Pendiente. Confírmala cuando la mercancía llegue.')
      setMostrarForm(false)
      setProveedorId('')
      setObservaciones('')
      setLineas([lineaVacia()])
      await cargarCompras()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setGuardando(false)
    }
  }

  async function verDetalle(compra: CompraListItem) {
    setError(null)
    try {
      const detalle = await getCompra(compra.id)
      setSeleccionada(detalle)
      setAlmacenConfirmar('')
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  async function confirmar() {
    if (!seleccionada || almacenConfirmar === '') return
    setProcesando(true)
    setError(null)
    try {
      await confirmarCompra(seleccionada.id, almacenConfirmar)
      setMensaje('Compra confirmada: existencias actualizadas.')
      setSeleccionada(null)
      await cargarCompras()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setProcesando(false)
    }
  }

  async function cancelar() {
    if (!seleccionada) return
    if (!confirm('¿Cancelar esta compra?')) return
    setProcesando(true)
    setError(null)
    try {
      await cancelarCompra(seleccionada.id)
      setMensaje('Compra cancelada.')
      setSeleccionada(null)
      await cargarCompras()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setProcesando(false)
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Compras</h1>
          <p className="page-subtitle">Encabezado y detalle de compras a proveedores</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn btn-sm" onClick={exportarCompras} disabled={compras.length === 0}>
            Exportar a Excel
          </button>
          <button className="btn btn-primary" onClick={() => setMostrarForm((v) => !v)} disabled={proveedores.length === 0}>
            + Nueva compra
          </button>
        </div>
      </div>

      <ErrorAlert message={error} />
      {mensaje && <div className="alert alert-success">{mensaje}</div>}
      {proveedores.length === 0 && (
        <div className="alert alert-error">Agrega al menos un proveedor antes de crear una compra.</div>
      )}

      {mostrarForm && (
        <div className="card">
          <h2>Nueva compra</h2>
          <div className="form-grid">
            <div className="field">
              <label>Proveedor</label>
              <select value={proveedorId} onChange={(e) => setProveedorId(e.target.value === '' ? '' : Number(e.target.value))}>
                <option value="">Selecciona…</option>
                {proveedores.map((proveedor) => (
                  <option key={proveedor.id} value={proveedor.id}>
                    {proveedor.nombre}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label>Observaciones (opcional)</label>
              <input value={observaciones} onChange={(e) => setObservaciones(e.target.value)} />
            </div>
          </div>

          <div className="detail-lines">
            {lineas.map((linea, index) => (
              <div className="detail-line" key={index}>
                <div className="field">
                  <label>Producto</label>
                  <select
                    value={linea.productoId}
                    onChange={(e) => actualizarLinea(index, { productoId: e.target.value === '' ? '' : Number(e.target.value) })}
                  >
                    <option value="">Selecciona…</option>
                    {productos.map((producto) => (
                      <option key={producto.id} value={producto.id}>
                        {producto.sku} — {producto.nombre}
                      </option>
                    ))}
                  </select>
                </div>
                <div className="field">
                  <label>Cantidad</label>
                  <input
                    type="number"
                    min={0}
                    step="0.0001"
                    value={linea.cantidad}
                    onChange={(e) => actualizarLinea(index, { cantidad: e.target.value })}
                  />
                </div>
                <div className="field">
                  <label>Precio unitario</label>
                  <input
                    type="number"
                    min={0}
                    step="0.01"
                    value={linea.precioUnitario}
                    onChange={(e) => actualizarLinea(index, { precioUnitario: e.target.value })}
                  />
                </div>
                <button className="btn btn-sm btn-danger" onClick={() => quitarLinea(index)} disabled={lineas.length === 1}>
                  Quitar
                </button>
              </div>
            ))}
          </div>
          <button className="btn btn-sm" onClick={agregarLinea}>
            + Agregar línea
          </button>

          <p className="muted" style={{ marginTop: 14 }}>
            Total estimado: <strong>{totalForm.toFixed(2)}</strong>
          </p>

          <div className="form-actions">
            <button className="btn btn-primary" onClick={guardarCompra} disabled={guardando || proveedorId === ''}>
              {guardando ? 'Guardando…' : 'Crear compra (Pendiente)'}
            </button>
            <button className="btn" onClick={() => setMostrarForm(false)} disabled={guardando}>
              Cancelar
            </button>
          </div>
        </div>
      )}

      {seleccionada && (
        <div className="card">
          <h2>
            Compra #{seleccionada.id} — {seleccionada.proveedorNombre} <StatusBadge estado={seleccionada.estado} />
          </h2>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th className="num">Cantidad</th>
                  <th className="num">Precio</th>
                  <th className="num">Subtotal</th>
                </tr>
              </thead>
              <tbody>
                {seleccionada.detalles.map((detalle) => (
                  <tr key={detalle.id}>
                    <td>
                      {detalle.productoSku} — {detalle.productoNombre}
                    </td>
                    <td className="num">{detalle.cantidad}</td>
                    <td className="num">{detalle.precioUnitario.toFixed(2)}</td>
                    <td className="num">{detalle.subtotal.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {seleccionada.estado === 'Pendiente' ? (
            <>
              <div className="form-grid" style={{ marginTop: 16 }}>
                <div className="field">
                  <label>Almacén que recibe la mercancía</label>
                  <select
                    value={almacenConfirmar}
                    onChange={(e) => setAlmacenConfirmar(e.target.value === '' ? '' : Number(e.target.value))}
                  >
                    <option value="">Selecciona…</option>
                    {almacenes.map((almacen) => (
                      <option key={almacen.id} value={almacen.id}>
                        {almacen.nombre}
                      </option>
                    ))}
                  </select>
                </div>
              </div>
              <div className="form-actions">
                <button className="btn btn-primary" onClick={confirmar} disabled={procesando || almacenConfirmar === ''}>
                  {procesando ? 'Confirmando…' : 'Confirmar y actualizar existencias'}
                </button>
                <button className="btn btn-danger" onClick={cancelar} disabled={procesando}>
                  Cancelar compra
                </button>
                <button className="btn" onClick={() => setSeleccionada(null)}>
                  Cerrar
                </button>
              </div>
            </>
          ) : (
            <div className="form-actions">
              <button className="btn" onClick={() => setSeleccionada(null)}>
                Cerrar
              </button>
            </div>
          )}
        </div>
      )}

      <div className="card">
        <div className="tabs">
          {['', 'Pendiente', 'Completada', 'Cancelada'].map((estado) => (
            <button
              key={estado || 'todas'}
              className={`tab-btn${filtroEstado === estado ? ' active' : ''}`}
              onClick={() => setFiltroEstado(estado)}
            >
              {estado || 'Todas'}
            </button>
          ))}
        </div>

        {cargando ? (
          <p className="muted">Cargando…</p>
        ) : compras.length === 0 ? (
          <p className="empty-row">No hay compras registradas.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Proveedor</th>
                  <th>Fecha</th>
                  <th>Estado</th>
                  <th className="num">Total</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {compras.map((compra) => (
                  <tr key={compra.id}>
                    <td>{compra.id}</td>
                    <td>{compra.proveedorNombre}</td>
                    <td>{new Date(compra.fecha).toLocaleDateString()}</td>
                    <td>
                      <StatusBadge estado={compra.estado} />
                    </td>
                    <td className="num">{compra.total.toFixed(2)}</td>
                    <td>
                      <button className="btn btn-sm" onClick={() => verDetalle(compra)}>
                        Ver / Confirmar
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
