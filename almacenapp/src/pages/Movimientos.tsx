import { useEffect, useState } from 'react'
import {
  getAlmacenes,
  getMovimientos,
  getProductos,
  getTiposMovimiento,
  registrarMovimiento,
  registrarTransferencia,
} from '../api'
import type { Almacen, Movimiento, Producto, TipoMovimiento } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

const TIPOS_SIMPLES = ['Entrada', 'Salida', 'Ajuste']

export default function Movimientos() {
  const [movimientos, setMovimientos] = useState<Movimiento[]>([])
  const [productos, setProductos] = useState<Producto[]>([])
  const [almacenes, setAlmacenes] = useState<Almacen[]>([])
  const [tipos, setTipos] = useState<TipoMovimiento[]>([])
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [mensaje, setMensaje] = useState<string | null>(null)
  const [mostrarTransferencia, setMostrarTransferencia] = useState(false)

  const [productoId, setProductoId] = useState<number | ''>('')
  const [almacenId, setAlmacenId] = useState<number | ''>('')
  const [tipoMovimientoId, setTipoMovimientoId] = useState<number | ''>('')
  const [cantidad, setCantidad] = useState('')
  const [referencia, setReferencia] = useState('')
  const [observaciones, setObservaciones] = useState('')
  const [enviando, setEnviando] = useState(false)

  const [tProductoId, setTProductoId] = useState<number | ''>('')
  const [tOrigenId, setTOrigenId] = useState<number | ''>('')
  const [tDestinoId, setTDestinoId] = useState<number | ''>('')
  const [tCantidad, setTCantidad] = useState('')
  const [enviandoT, setEnviandoT] = useState(false)

  async function cargarCatalogos() {
    try {
      const [prods, alms, tps] = await Promise.all([getProductos(), getAlmacenes(), getTiposMovimiento()])
      setProductos(prods)
      setAlmacenes(alms)
      setTipos(tps)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  async function cargarMovimientos() {
    setCargando(true)
    try {
      setMovimientos(await getMovimientos())
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setCargando(false)
    }
  }

  useEffect(() => {
    cargarCatalogos()
    cargarMovimientos()
  }, [])

  const tiposSimples = tipos.filter((tipo) => TIPOS_SIMPLES.includes(tipo.nombre))

  async function enviarMovimiento() {
    if (productoId === '' || almacenId === '' || tipoMovimientoId === '' || !cantidad) return
    setEnviando(true)
    setError(null)
    setMensaje(null)
    try {
      await registrarMovimiento({
        productoId,
        almacenId,
        tipoMovimientoId,
        cantidad: Number(cantidad),
        referencia: referencia || null,
        observaciones: observaciones || null,
      })
      setMensaje('Movimiento registrado correctamente.')
      setCantidad('')
      setReferencia('')
      setObservaciones('')
      await cargarMovimientos()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setEnviando(false)
    }
  }

  async function enviarTransferencia() {
    if (tProductoId === '' || tOrigenId === '' || tDestinoId === '' || !tCantidad) return
    setEnviandoT(true)
    setError(null)
    setMensaje(null)
    try {
      await registrarTransferencia({
        productoId: tProductoId,
        almacenOrigenId: tOrigenId,
        almacenDestinoId: tDestinoId,
        cantidad: Number(tCantidad),
      })
      setMensaje('Transferencia registrada correctamente.')
      setTCantidad('')
      await cargarMovimientos()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setEnviandoT(false)
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Movimientos</h1>
          <p className="page-subtitle">Entradas, salidas, ajustes y transferencias de inventario</p>
        </div>
      </div>

      <ErrorAlert message={error} />
      {mensaje && <div className="alert alert-success">{mensaje}</div>}

      <div className="card">
        <h2>Registrar movimiento</h2>
        <div className="form-grid">
          <div className="field">
            <label>Producto</label>
            <select value={productoId} onChange={(e) => setProductoId(e.target.value === '' ? '' : Number(e.target.value))}>
              <option value="">Selecciona…</option>
              {productos.map((producto) => (
                <option key={producto.id} value={producto.id}>
                  {producto.sku} — {producto.nombre}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Almacén</label>
            <select value={almacenId} onChange={(e) => setAlmacenId(e.target.value === '' ? '' : Number(e.target.value))}>
              <option value="">Selecciona…</option>
              {almacenes.map((almacen) => (
                <option key={almacen.id} value={almacen.id}>
                  {almacen.nombre}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Tipo</label>
            <select
              value={tipoMovimientoId}
              onChange={(e) => setTipoMovimientoId(e.target.value === '' ? '' : Number(e.target.value))}
            >
              <option value="">Selecciona…</option>
              {tiposSimples.map((tipo) => (
                <option key={tipo.id} value={tipo.id}>
                  {tipo.nombre}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Cantidad {tipos.find((t) => t.id === tipoMovimientoId)?.nombre === 'Ajuste' ? '(+ o −)' : ''}</label>
            <input type="number" step="0.0001" value={cantidad} onChange={(e) => setCantidad(e.target.value)} />
          </div>
          <div className="field">
            <label>Referencia (opcional)</label>
            <input value={referencia} onChange={(e) => setReferencia(e.target.value)} />
          </div>
          <div className="field">
            <label>Observaciones (opcional)</label>
            <input value={observaciones} onChange={(e) => setObservaciones(e.target.value)} />
          </div>
        </div>
        <div className="form-actions">
          <button
            className="btn btn-primary"
            onClick={enviarMovimiento}
            disabled={enviando || productoId === '' || almacenId === '' || tipoMovimientoId === '' || !cantidad}
          >
            {enviando ? 'Registrando…' : 'Registrar movimiento'}
          </button>
          <button className="btn btn-link" onClick={() => setMostrarTransferencia((v) => !v)}>
            {mostrarTransferencia ? 'Ocultar transferencia entre almacenes' : '¿Necesitas transferir entre almacenes?'}
          </button>
        </div>
      </div>

      {mostrarTransferencia && (
        <div className="card">
          <h2>Transferencia entre almacenes</h2>
          <div className="form-grid">
            <div className="field">
              <label>Producto</label>
              <select
                value={tProductoId}
                onChange={(e) => setTProductoId(e.target.value === '' ? '' : Number(e.target.value))}
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
              <label>Almacén origen</label>
              <select value={tOrigenId} onChange={(e) => setTOrigenId(e.target.value === '' ? '' : Number(e.target.value))}>
                <option value="">Selecciona…</option>
                {almacenes.map((almacen) => (
                  <option key={almacen.id} value={almacen.id}>
                    {almacen.nombre}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label>Almacén destino</label>
              <select value={tDestinoId} onChange={(e) => setTDestinoId(e.target.value === '' ? '' : Number(e.target.value))}>
                <option value="">Selecciona…</option>
                {almacenes.map((almacen) => (
                  <option key={almacen.id} value={almacen.id}>
                    {almacen.nombre}
                  </option>
                ))}
              </select>
            </div>
            <div className="field">
              <label>Cantidad</label>
              <input type="number" min={0} step="0.0001" value={tCantidad} onChange={(e) => setTCantidad(e.target.value)} />
            </div>
          </div>
          <div className="form-actions">
            <button
              className="btn btn-primary"
              onClick={enviarTransferencia}
              disabled={enviandoT || tProductoId === '' || tOrigenId === '' || tDestinoId === '' || !tCantidad}
            >
              {enviandoT ? 'Transfiriendo…' : 'Transferir'}
            </button>
          </div>
        </div>
      )}

      <div className="card">
        <h2>Movimientos recientes</h2>
        {cargando ? (
          <p className="muted">Cargando…</p>
        ) : movimientos.length === 0 ? (
          <p className="empty-row">Todavía no hay movimientos registrados.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Fecha</th>
                  <th>Producto</th>
                  <th>Almacén</th>
                  <th>Tipo</th>
                  <th className="num">Cantidad</th>
                  <th>Referencia</th>
                </tr>
              </thead>
              <tbody>
                {movimientos.slice(0, 100).map((movimiento) => (
                  <tr key={movimiento.id}>
                    <td>{new Date(movimiento.fecha).toLocaleString()}</td>
                    <td>{movimiento.productoNombre}</td>
                    <td>{movimiento.almacenNombre}</td>
                    <td>{movimiento.tipoMovimientoNombre}</td>
                    <td className="num">{movimiento.cantidad}</td>
                    <td>{movimiento.referencia ?? '—'}</td>
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
