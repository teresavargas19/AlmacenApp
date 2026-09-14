import { useEffect, useState } from 'react'
import {
  cancelarSalida,
  confirmarSalida,
  crearSalida,
  getAlmacenes,
  getClientes,
  getProductos,
  getSalida,
  getSalidas,
} from '../api'
import type { Almacen, Cliente, Producto, SalidaDetail, SalidaListItem } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'
import StatusBadge from '../components/StatusBadge'

interface LineaForm {
  productoId: number | ''
  cantidad: string
}

function lineaVacia(): LineaForm {
  return { productoId: '', cantidad: '' }
}

export default function Salidas() {
  const [salidas, setSalidas] = useState<SalidaListItem[]>([])
  const [clientes, setClientes] = useState<Cliente[]>([])
  const [productos, setProductos] = useState<Producto[]>([])
  const [almacenes, setAlmacenes] = useState<Almacen[]>([])
  const [filtroEstado, setFiltroEstado] = useState('')
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [mensaje, setMensaje] = useState<string | null>(null)

  const [mostrarForm, setMostrarForm] = useState(false)
  const [clienteId, setClienteId] = useState<number | ''>('')
  const [observaciones, setObservaciones] = useState('')
  const [lineas, setLineas] = useState<LineaForm[]>([lineaVacia()])
  const [guardando, setGuardando] = useState(false)

  const [seleccionada, setSeleccionada] = useState<SalidaDetail | null>(null)
  const [almacenConfirmar, setAlmacenConfirmar] = useState<number | ''>('')
  const [procesando, setProcesando] = useState(false)

  async function cargarCatalogos() {
    try {
      const [clis, prods, alms] = await Promise.all([getClientes(), getProductos(), getAlmacenes()])
      setClientes(clis)
      setProductos(prods)
      setAlmacenes(alms)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  async function cargarSalidas() {
    setCargando(true)
    setError(null)
    try {
      setSalidas(await getSalidas(filtroEstado || undefined))
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
    cargarSalidas()
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

  async function guardarSalida() {
    const detalles = lineas
      .filter((linea) => linea.productoId !== '' && Number(linea.cantidad) > 0)
      .map((linea) => ({ productoId: linea.productoId as number, cantidad: Number(linea.cantidad) }))
    if (detalles.length === 0) return

    setGuardando(true)
    setError(null)
    try {
      await crearSalida({
        clienteId: clienteId === '' ? null : clienteId,
        observaciones: observaciones || null,
        detalles,
      })
      setMensaje('Salida creada como Pendiente. Confírmala para descontar el inventario.')
      setMostrarForm(false)
      setClienteId('')
      setObservaciones('')
      setLineas([lineaVacia()])
      await cargarSalidas()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setGuardando(false)
    }
  }

  async function verDetalle(salida: SalidaListItem) {
    setError(null)
    try {
      const detalle = await getSalida(salida.id)
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
      await confirmarSalida(seleccionada.id, almacenConfirmar)
      setMensaje('Salida confirmada: existencias descontadas.')
      setSeleccionada(null)
      await cargarSalidas()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setProcesando(false)
    }
  }

  async function cancelar() {
    if (!seleccionada) return
    if (!confirm('¿Cancelar esta salida?')) return
    setProcesando(true)
    setError(null)
    try {
      await cancelarSalida(seleccionada.id)
      setMensaje('Salida cancelada.')
      setSeleccionada(null)
      await cargarSalidas()
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
          <h1>Salidas</h1>
          <p className="page-subtitle">Despachos de mercancía, con o sin cliente</p>
        </div>
        <button className="btn btn-primary" onClick={() => setMostrarForm((v) => !v)}>
          + Nueva salida
        </button>
      </div>

      <ErrorAlert message={error} />
      {mensaje && <div className="alert alert-success">{mensaje}</div>}

      {mostrarForm && (
        <div className="card">
          <h2>Nueva salida</h2>
          <div className="form-grid">
            <div className="field">
              <label>Cliente (opcional)</label>
              <select value={clienteId} onChange={(e) => setClienteId(e.target.value === '' ? '' : Number(e.target.value))}>
                <option value="">Sin cliente específico</option>
                {clientes.map((cliente) => (
                  <option key={cliente.id} value={cliente.id}>
                    {cliente.nombre}
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
              <div className="detail-line two-col" key={index}>
                <div className="field">
                  <label>Producto</label>
                  <select
                    value={linea.productoId}
                    onChange={(e) => actualizarLinea(index, { productoId: e.target.value === '' ? '' : Number(e.target.value) })}
                  >
                    <option value="">Selecciona…</option>
                    {productos.map((producto) => (
                      <option key={producto.id} value={producto.id}>
                        {producto.sku} — {producto.nombre} (stock: {producto.stock})
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
                <button className="btn btn-sm btn-danger" onClick={() => quitarLinea(index)} disabled={lineas.length === 1}>
                  Quitar
                </button>
              </div>
            ))}
          </div>
          <button className="btn btn-sm" onClick={agregarLinea}>
            + Agregar línea
          </button>

          <div className="form-actions" style={{ marginTop: 14 }}>
            <button className="btn btn-primary" onClick={guardarSalida} disabled={guardando}>
              {guardando ? 'Guardando…' : 'Crear salida (Pendiente)'}
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
            Salida #{seleccionada.id} {seleccionada.clienteNombre ? `— ${seleccionada.clienteNombre}` : ''}{' '}
            <StatusBadge estado={seleccionada.estado} />
          </h2>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th className="num">Cantidad</th>
                </tr>
              </thead>
              <tbody>
                {seleccionada.detalles.map((detalle) => (
                  <tr key={detalle.id}>
                    <td>
                      {detalle.productoSku} — {detalle.productoNombre}
                    </td>
                    <td className="num">{detalle.cantidad}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          {seleccionada.estado === 'Pendiente' ? (
            <>
              <div className="form-grid" style={{ marginTop: 16 }}>
                <div className="field">
                  <label>Almacén del que sale la mercancía</label>
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
                  {procesando ? 'Confirmando…' : 'Confirmar y descontar existencias'}
                </button>
                <button className="btn btn-danger" onClick={cancelar} disabled={procesando}>
                  Cancelar salida
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
        ) : salidas.length === 0 ? (
          <p className="empty-row">No hay salidas registradas.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>#</th>
                  <th>Cliente</th>
                  <th>Fecha</th>
                  <th>Estado</th>
                  <th className="num">Unidades</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {salidas.map((salida) => (
                  <tr key={salida.id}>
                    <td>{salida.id}</td>
                    <td>{salida.clienteNombre ?? '—'}</td>
                    <td>{new Date(salida.fecha).toLocaleDateString()}</td>
                    <td>
                      <StatusBadge estado={salida.estado} />
                    </td>
                    <td className="num">{salida.totalUnidades}</td>
                    <td>
                      <button className="btn btn-sm" onClick={() => verDetalle(salida)}>
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
