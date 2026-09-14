import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'

const links = [
  { to: '/', label: 'Inicio', end: true },
  { to: '/productos', label: 'Productos' },
  { to: '/existencias', label: 'Existencias' },
  { to: '/movimientos', label: 'Movimientos' },
  { to: '/compras', label: 'Compras' },
  { to: '/salidas', label: 'Salidas' },
  { to: '/proveedores', label: 'Proveedores' },
  { to: '/clientes', label: 'Clientes' },
  { to: '/catalogos', label: 'Catálogos' },
]

export default function Layout() {
  const { usuario, cerrarSesion } = useAuth()

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          Almacen<span>App</span>
        </div>
        <nav>
          {links.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              end={link.end}
              className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
            >
              {link.label}
            </NavLink>
          ))}
        </nav>
        <div className="sidebar-footer">
          <div className="sidebar-user">
            <div className="sidebar-user-name">{usuario?.nombre}</div>
            <div className="sidebar-user-role">{usuario?.rolNombre}</div>
          </div>
          <button className="btn btn-sm" onClick={cerrarSesion} style={{ width: '100%' }}>
            Cerrar sesión
          </button>
        </div>
      </aside>
      <main className="app-main">
        <Outlet />
      </main>
    </div>
  )
}
