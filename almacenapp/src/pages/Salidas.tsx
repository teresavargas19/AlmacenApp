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
  registrarAbono,
} from '../api'
import type { Almacen, Cliente, MetodoPago, Producto, SalidaDetail, SalidaListItem } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'
import StatusBadge from '../components/StatusBadge'
import { exportarExcel } from '../exportExcel'

const ITBIS_PORCENTAJE = 0.18

interface LineaForm {
  productoId: number | ''
  cantidad: string
  precioUnitario: string
  descuentoPorcentaje: string
}

function lineaVacia(): LineaForm {
  return { productoId: '', cantidad: '', precioUnitario: '', descuentoPorcentaje: '' }
}

function calcularSubtotalLinea(linea: LineaForm) {
  const cantidad = Number(linea.cantidad) || 0
  const precio = Number(linea.precioUnitario) || 0
  const descuento = Number(linea.descuentoPorcentaje) || 0
  return cantidad * precio * (1 - descuento / 100)
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
  const [metodoPago, setMetodoPago] = useState<MetodoPago>('Efectivo')
  const [descuentoGeneral, setDescuentoGeneral] = useState('')
  const [lineas, setLineas] = useState<LineaForm[]>([lineaVacia()])
  const [guardando, setGuardando] = useState(false)

  const [seleccionada, setSeleccionada] = useState<SalidaDetail | null>(null)
  const [almacenConfirmar, setAlmacenConfirmar] = useState<number | ''>('')
  const [procesando, setProcesando] = useState(false)

  const [montoAbono, setMontoAbono] = useState('')
  const [obsAbono, setObsAbono] = useState('')
  const [enviandoAbono, setEnviandoAbono] = useState(false)

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

  function exportarSalidas() {
    exportarExcel(
      'salidas',
      'Salidas',
      salidas.map((salida) => ({
        '#': salida.id,
        Cliente: salida.clienteNombre ?? '',
        Fecha: new Date(salida.fecha).toLocaleDateString(),
        Estado: salida.estado,
        'Método de pago': salida.metodoPago,
        Total: salida.total,
        'Saldo pendiente': salida.saldoPendiente,
        Unidades: salida.totalUnidades,
      })),
    )
  }

  const subtotalForm = lineas.reduce((acc, linea) => acc + calcularSubtotalLinea(linea), 0)
  const descuentoGeneralNum = Number(descuentoGeneral) || 0
  const subtotalConDescuentoGeneralForm = subtotalForm * (1 - descuentoGeneralNum / 100)
  const itbisForm = subtotalConDescuentoGeneralForm * ITBIS_PORCENTAJE
  const totalForm = subtotalConDescuentoGeneralForm + itbisForm

  async function guardarSalida() {
    const detalles = lineas
      .filter((linea) => linea.productoId !== '' && Number(linea.cantidad) > 0)
      .map((linea) => ({
        productoId: linea.productoId as number,
        cantidad: Number(linea.cantidad),
        precioUnitario: Number(linea.precioUnitario) || 0,
        descuentoPorcentaje: Number(linea.descuentoPorcentaje) || 0,
      }))
    if (detalles.length === 0) return
    if (metodoPago === 'Credito' && clienteId === '') {
      setError('Las ventas a crédito requieren indicar un cliente.')
      return
    }

    setGuardando(true)
    setError(null)
    try {
      await crearSalida({
        clienteId: clienteId === '' ? null : clienteId,
        observaciones: observaciones || null,
        metodoPago,
        descuentoGeneralPorcentaje: descuentoGeneralNum,
        detalles,
      })
      setMensaje('Salida creada como Pendiente. Confírmala para descontar el inventario.')
      setMostrarForm(false)
      setClienteId('')
      setObservaciones('')
      setMetodoPago('Efectivo')
      setDescuentoGeneral('')
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
      setMontoAbono('')
      setObsAbono('')
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

  async function enviarAbono() {
    if (!seleccionada || !montoAbono) return
    setEnviandoAbono(true)
    setError(null)
    try {
      await registrarAbono(seleccionada.id, { monto: Number(montoAbono), observaciones: obsAbono || null })
      setMensaje('Abono registrado.')
      setMontoAbono('')
      setObsAbono('')
      const detalle = await getSalida(seleccionada.id)
      setSeleccionada(detalle)
      await cargarSalidas()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setEnviandoAbono(false)
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Salidas</h1>
          <p className="page-subtitle">Despachos de mercancía, con o sin cliente</p>
        </div>
        <div style={{ display: 'flex', gap: 8 }}>
          <button className="btn btn-sm" onClick={exportarSalidas} disabled={salidas.length === 0}>
            Exportar a Excel
          </button>
          <button className="btn btn-primary" onClick={() => setMostrarForm((v) => !v)}>
            + Nueva salida
          </button>
        </div>
      </div>

      <ErrorAlert message={error} />
      {mensaje && <div className="alert alert-success">{mensaje}</div>}

      {mostrarForm && (
        <div className="card">
          <h2>Nueva salida</h2>
          <div className="form-grid">
            <div className="field">
              <label>Cliente {metodoPago === 'Credito' ? '(obligatorio para crédito)' : '(opcional)'}</label>
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
              <label>Método de pago</label>
              <select value={metodoPago} onChange={(e) => setMetodoPago(e.target.value as MetodoPago)}>
                <option value="Efectivo">Efectivo</option>
                <option value="Transferencia">Transferencia</option>
                <option value="Credito">Crédito</option>
              </select>
            </div>
            <div className="field">
              <label>Descuento general % (opcional)</label>
              <input
                type="number"
                min={0}
                max={100}
                step="0.01"
                value={descuentoGeneral}
                onChange={(e) => setDescuentoGeneral(e.target.value)}
              />
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
                <div className="field">
                  <label>Descuento % (opcional)</label>
                  <input
                    type="number"
                    min={0}
                    max={100}
                    step="0.01"
                    value={linea.descuentoPorcentaje}
                    onChange={(e) => actualizarLinea(index, { descuentoPorcentaje: e.target.value })}
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

          <div className="muted" style={{ marginTop: 14, textAlign: 'right' }}>
            <p style={{ margin: '2px 0' }}>Subtotal: {subtotalForm.toFixed(2)}</p>
            <p style={{ margin: '2px 0' }}>ITBIS (18%): {itbisForm.toFixed(2)}</p>
            <p style={{ margin: '2px 0' }}>
              Total estimado: <strong>{totalForm.toFixed(2)}</strong>
            </p>
          </div>

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
          <p className="muted">
            Método de pago: <strong>{seleccionada.metodoPago === 'Credito' ? 'Crédito' : seleccionada.metodoPago}</strong>
          </p>
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>Producto</th>
                  <th className="num">Cantidad</th>
                  <th className="num">Precio</th>
                  <th className="num">Desc. %</th>
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
                    <td className="num">{detalle.descuentoPorcentaje}</td>
                    <td className="num">{detalle.subtotal.toFixed(2)}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <div className="muted" style={{ marginTop: 12, textAlign: 'right' }}>
            <p style={{ margin: '2px 0' }}>Subtotal: {seleccionada.subtotal.toFixed(2)}</p>
            {seleccionada.descuentoGeneralPorcentaje > 0 && (
              <p style={{ margin: '2px 0' }}>Descuento general: {seleccionada.descuentoGeneralPorcentaje}%</p>
            )}
            <p style={{ margin: '2px 0' }}>ITBIS: {seleccionada.itbis.toFixed(2)}</p>
            <p style={{ margin: '2px 0' }}>
              <strong>Total: {seleccionada.total.toFixed(2)}</strong>
            </p>
            {seleccionada.metodoPago === 'Credito' && (
              <p style={{ margin: '2px 0' }}>
                <strong>Saldo pendiente: {seleccionada.saldoPendiente.toFixed(2)}</strong>
              </p>
            )}
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
            <>
              {seleccionada.metodoPago === 'Credito' && (
                <div style={{ marginTop: 16 }}>
                  <h3>Abonos</h3>
                  {seleccionada.abonos.length === 0 ? (
                    <p className="muted">Todavía no se han registrado abonos.</p>
                  ) : (
                    <div className="table-wrap">
                      <table className="table">
                        <thead>
                          <tr>
                            <th>Fecha</th>
                            <th className="num">Monto</th>
                            <th>Observaciones</th>
                          </tr>
                        </thead>
                        <tbody>
                          {seleccionada.abonos.map((abono) => (
                            <tr key={abono.id}>
                              <td>{new Date(abono.fecha).toLocaleString()}</td>
                              <td className="num">{abono.monto.toFixed(2)}</td>
                              <td>{abono.observaciones ?? '—'}</td>
                            </tr>
                          ))}
                        </tbody>
                      </table>
                    </div>
                  )}

                  {seleccionada.saldoPendiente > 0 && (
                    <div className="form-grid" style={{ marginTop: 12 }}>
                      <div className="field">
                        <label>Monto del abono</label>
                        <input
                          type="number"
                          min={0}
                          max={seleccionada.saldoPendiente}
                          step="0.01"
                          value={montoAbono}
                          onChange={(e) => setMontoAbono(e.target.value)}
                        />
                      </div>
                      <div className="field">
                        <label>Observaciones (opcional)</label>
                        <input value={obsAbono} onChange={(e) => setObsAbono(e.target.value)} />
                      </div>
                      <div className="field" style={{ alignSelf: 'end' }}>
                        <button className="btn btn-primary btn-sm" onClick={enviarAbono} disabled={enviandoAbono || !montoAbono}>
                          {enviandoAbono ? 'Registrando…' : 'Registrar abono'}
                        </button>
                      </div>
                    </div>
                  )}
                </div>
              )}
              <div className="form-actions" style={{ marginTop: 16 }}>
                <button className="btn" onClick={() => setSeleccionada(null)}>
                  Cerrar
                </button>
              </div>
            </>
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
                  <th>Pago</th>
                  <th className="num">Total</th>
                  <th className="num">Saldo</th>
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
                    <td>{salida.metodoPago === 'Credito' ? 'Crédito' : salida.metodoPago}</td>
                    <td className="num">{salida.total.toFixed(2)}</td>
                    <td className="num">{salida.metodoPago === 'Credito' ? salida.saldoPendiente.toFixed(2) : '—'}</td>
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
