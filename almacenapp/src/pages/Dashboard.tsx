import { useEffect, useState } from 'react'
import { getCompras, getExistenciasBajoMinimo, getProductos, getSalidas } from '../api'
import type { Existencia } from '../types'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

export default function Dashboard() {
  const [productosActivos, setProductosActivos] = useState(0)
  const [bajoMinimo, setBajoMinimo] = useState<Existencia[]>([])
  const [comprasPendientes, setComprasPendientes] = useState(0)
  const [salidasPendientes, setSalidasPendientes] = useState(0)
  const [error, setError] = useState<string | null>(null)
  const [cargando, setCargando] = useState(true)

  useEffect(() => {
    let activo = true

    async function cargar() {
      setCargando(true)
      setError(null)
      try {
        const [productos, existencias, compras, salidas] = await Promise.all([
          getProductos(),
          getExistenciasBajoMinimo(),
          getCompras('Pendiente'),
          getSalidas('Pendiente'),
        ])
        if (!activo) return
        setProductosActivos(productos.length)
        setBajoMinimo(existencias)
        setComprasPendientes(compras.length)
        setSalidasPendientes(salidas.length)
      } catch (err) {
        if (activo) setError(errorMessage(err))
      } finally {
        if (activo) setCargando(false)
      }
    }

    cargar()
    return () => {
      activo = false
    }
  }, [])

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Inicio</h1>
          <p className="page-subtitle">Resumen general del almacén</p>
        </div>
      </div>

      <ErrorAlert message={error} />

      {cargando ? (
        <p className="muted">Cargando…</p>
      ) : (
        <>
          <div className="stat-grid">
            <div className="stat-card">
              <div className="stat-label">Productos activos</div>
              <div className="stat-value">{productosActivos}</div>
            </div>
            <div className={`stat-card${bajoMinimo.length > 0 ? ' warning' : ''}`}>
              <div className="stat-label">Existencias bajo mínimo</div>
              <div className="stat-value">{bajoMinimo.length}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Compras pendientes</div>
              <div className="stat-value">{comprasPendientes}</div>
            </div>
            <div className="stat-card">
              <div className="stat-label">Salidas pendientes</div>
              <div className="stat-value">{salidasPendientes}</div>
            </div>
          </div>

          <div className="card">
            <h2>Productos bajo el mínimo</h2>
            {bajoMinimo.length === 0 ? (
              <p className="muted">Ningún producto está por debajo de su mínimo. Todo en orden.</p>
            ) : (
              <div className="table-wrap">
                <table className="table">
                  <thead>
                    <tr>
                      <th>SKU</th>
                      <th>Producto</th>
                      <th>Almacén</th>
                      <th className="num">Cantidad</th>
                      <th className="num">Mínimo</th>
                    </tr>
                  </thead>
                  <tbody>
                    {bajoMinimo.map((item) => (
                      <tr key={item.id}>
                        <td>{item.productoSku}</td>
                        <td>{item.productoNombre}</td>
                        <td>{item.almacenNombre}</td>
                        <td className="num">{item.cantidad}</td>
                        <td className="num">{item.cantidadMinima}</td>
                      </tr>
                    ))}
                  </tbody>
                </table>
              </div>
            )}
          </div>
        </>
      )}
    </div>
  )
}
