import { NavLink, Outlet } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import { tienePermiso } from '../permisos'

const links = [
  { to: '/productos', label: 'Productos', permiso: 'productos' },
  { to: '/existencias', label: 'Existencias', permiso: 'existencias' },
  { to: '/movimientos', label: 'Movimientos', permiso: 'movimientos' },
  { to: '/compras', label: 'Compras', permiso: 'compras' },
  { to: '/salidas', label: 'Salidas', permiso: 'salidas' },
  { to: '/proveedores', label: 'Proveedores', permiso: 'proveedores' },
  { to: '/clientes', label: 'Clientes', permiso: 'clientes' },
  { to: '/catalogos', label: 'Catálogos', permiso: 'catalogos' },
]

export default function Layout() {
  const { usuario, cerrarSesion } = useAuth()
  const esAdministrador = usuario?.rolNombre === 'Administrador'
  const linksVisibles = links.filter((link) => tienePermiso(usuario, link.permiso))

  return (
    <div className="app-shell">
      <aside className="sidebar">
        <div className="sidebar-brand">
          Almacen<span>App</span>
        </div>
        <nav>
          <NavLink to="/" end className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
            Inicio
          </NavLink>
          {linksVisibles.map((link) => (
            <NavLink
              key={link.to}
              to={link.to}
              className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}
            >
              {link.label}
            </NavLink>
          ))}
          {esAdministrador && (
            <>
              <NavLink to="/usuarios" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
                Usuarios
              </NavLink>
              <NavLink to="/roles" className={({ isActive }) => `nav-link${isActive ? ' active' : ''}`}>
                Roles
              </NavLink>
            </>
          )}
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
