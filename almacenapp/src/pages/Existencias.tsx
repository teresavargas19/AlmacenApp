import { useEffect, useState } from 'react'
import { actualizarMinimo, getAlmacenes, getExistencias, getExistenciasBajoMinimo, getProductos } from '../api'
import type { Almacen, Existencia, Producto } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

export default function Existencias() {
  const [items, setItems] = useState<Existencia[]>([])
  const [productos, setProductos] = useState<Producto[]>([])
  const [almacenes, setAlmacenes] = useState<Almacen[]>([])
  const [productoId, setProductoId] = useState<number | ''>('')
  const [almacenId, setAlmacenId] = useState<number | ''>('')
  const [soloBajoMinimo, setSoloBajoMinimo] = useState(false)
  const [cargando, setCargando] = useState(true)
  const [error, setError] = useState<string | null>(null)
  const [editandoId, setEditandoId] = useState<number | null>(null)
  const [minimoTemp, setMinimoTemp] = useState('')

  async function cargarCatalogos() {
    try {
      const [prods, alms] = await Promise.all([getProductos(), getAlmacenes()])
      setProductos(prods)
      setAlmacenes(alms)
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  async function cargarExistencias() {
    setCargando(true)
    setError(null)
    try {
      const data = soloBajoMinimo
        ? await getExistenciasBajoMinimo()
        : await getExistencias({
            productoId: productoId === '' ? undefined : productoId,
            almacenId: almacenId === '' ? undefined : almacenId,
          })
      setItems(data)
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
    cargarExistencias()
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [productoId, almacenId, soloBajoMinimo])

  function empezarEdicion(item: Existencia) {
    setEditandoId(item.id)
    setMinimoTemp(String(item.cantidadMinima))
  }

  async function guardarMinimo(item: Existencia) {
    const valor = Number(minimoTemp)
    if (Number.isNaN(valor) || valor < 0) return
    setError(null)
    try {
      await actualizarMinimo(item.id, valor)
      setEditandoId(null)
      await cargarExistencias()
    } catch (err) {
      setError(errorMessage(err))
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Existencias</h1>
          <p className="page-subtitle">Cantidad actual por producto y almacén</p>
        </div>
      </div>

      <ErrorAlert message={error} />

      <div className="card">
        <div className="form-grid" style={{ marginBottom: 0 }}>
          <div className="field">
            <label>Producto</label>
            <select
              value={productoId}
              onChange={(e) => setProductoId(e.target.value === '' ? '' : Number(e.target.value))}
              disabled={soloBajoMinimo}
            >
              <option value="">Todos</option>
              {productos.map((producto) => (
                <option key={producto.id} value={producto.id}>
                  {producto.sku} — {producto.nombre}
                </option>
              ))}
            </select>
          </div>
          <div className="field">
            <label>Almacén</label>
            <select
              value={almacenId}
              onChange={(e) => setAlmacenId(e.target.value === '' ? '' : Number(e.target.value))}
              disabled={soloBajoMinimo}
            >
              <option value="">Todos</option>
              {almacenes.map((almacen) => (
                <option key={almacen.id} value={almacen.id}>
                  {almacen.nombre}
                </option>
              ))}
            </select>
          </div>
          <div className="field field-checkbox" style={{ alignSelf: 'end' }}>
            <input
              type="checkbox"
              id="solo-bajo-minimo"
              checked={soloBajoMinimo}
              onChange={(e) => setSoloBajoMinimo(e.target.checked)}
            />
            <label htmlFor="solo-bajo-minimo">Solo bajo mínimo</label>
          </div>
        </div>
      </div>

      <div className="card">
        {cargando ? (
          <p className="muted">Cargando…</p>
        ) : items.length === 0 ? (
          <p className="empty-row">No hay existencias que coincidan con el filtro.</p>
        ) : (
          <div className="table-wrap">
            <table className="table">
              <thead>
                <tr>
                  <th>SKU</th>
                  <th>Producto</th>
                  <th>Almacén</th>
                  <th>Ubicación</th>
                  <th className="num">Cantidad</th>
                  <th className="num">Mínimo</th>
                  <th></th>
                </tr>
              </thead>
              <tbody>
                {items.map((item) => (
                  <tr key={item.id} className={item.cantidad < item.cantidadMinima ? 'selected' : undefined}>
                    <td>{item.productoSku}</td>
                    <td>{item.productoNombre}</td>
                    <td>{item.almacenNombre}</td>
                    <td>{item.ubicacionNombre ?? '—'}</td>
                    <td className="num">{item.cantidad}</td>
                    <td className="num">
                      {editandoId === item.id ? (
                        <input
                          type="number"
                          min={0}
                          value={minimoTemp}
                          onChange={(e) => setMinimoTemp(e.target.value)}
                          style={{ width: 80 }}
                        />
                      ) : (
                        item.cantidadMinima
                      )}
                    </td>
                    <td>
                      {editandoId === item.id ? (
                        <>
                          <button className="btn btn-sm btn-primary" onClick={() => guardarMinimo(item)}>
                            Guardar
                          </button>{' '}
                          <button className="btn btn-sm" onClick={() => setEditandoId(null)}>
                            Cancelar
                          </button>
                        </>
                      ) : (
                        <button className="btn btn-sm" onClick={() => empezarEdicion(item)}>
                          Editar mínimo
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
