import { Navigate, Route, Routes, useLocation } from 'react-router-dom'
import type { ReactElement } from 'react'
import Layout from './components/Layout'
import Login from './pages/Login'
import Dashboard from './pages/Dashboard'
import Productos from './pages/Productos'
import Catalogos from './pages/Catalogos'
import Existencias from './pages/Existencias'
import Movimientos from './pages/Movimientos'
import Compras from './pages/Compras'
import Salidas from './pages/Salidas'
import Proveedores from './pages/Proveedores'
import Clientes from './pages/Clientes'
import Usuarios from './pages/Usuarios'
import Roles from './pages/Roles'
import { useAuth } from './context/AuthContext'
import { tienePermiso } from './permisos'
import './App.css'

function RequireAuth({ children }: { children: ReactElement }) {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
  }

  return children
}

function RequireAdmin({ children }: { children: ReactElement }) {
  const { usuario } = useAuth()

  if (usuario?.rolNombre !== 'Administrador') {
    return <Navigate to="/" replace />
  }

  return children
}

/** Además de estar logueado, el rol del usuario debe tener este permiso de módulo. */
function RequirePermiso({ clave, children }: { clave: string; children: ReactElement }) {
  const { usuario } = useAuth()

  if (!tienePermiso(usuario, clave)) {
    return <Navigate to="/" replace />
  }

  return children
}

function App() {
  return (
    <Routes>
      <Route path="/login" element={<Login />} />
      <Route
        element={
          <RequireAuth>
            <Layout />
          </RequireAuth>
        }
      >
        <Route index element={<Dashboard />} />
        <Route
          path="productos"
          element={
            <RequirePermiso clave="productos">
              <Productos />
            </RequirePermiso>
          }
        />
        <Route
          path="existencias"
          element={
            <RequirePermiso clave="existencias">
              <Existencias />
            </RequirePermiso>
          }
        />
        <Route
          path="movimientos"
          element={
            <RequirePermiso clave="movimientos">
              <Movimientos />
            </RequirePermiso>
          }
        />
        <Route
          path="compras"
          element={
            <RequirePermiso clave="compras">
              <Compras />
            </RequirePermiso>
          }
        />
        <Route
          path="salidas"
          element={
            <RequirePermiso clave="salidas">
              <Salidas />
            </RequirePermiso>
          }
        />
        <Route
          path="proveedores"
          element={
            <RequirePermiso clave="proveedores">
              <Proveedores />
            </RequirePermiso>
          }
        />
        <Route
          path="clientes"
          element={
            <RequirePermiso clave="clientes">
              <Clientes />
            </RequirePermiso>
          }
        />
        <Route
          path="catalogos"
          element={
            <RequirePermiso clave="catalogos">
              <Catalogos />
            </RequirePermiso>
          }
        />
        <Route
          path="usuarios"
          element={
            <RequireAdmin>
              <Usuarios />
            </RequireAdmin>
          }
        />
        <Route
          path="roles"
          element={
            <RequireAdmin>
              <Roles />
            </RequireAdmin>
          }
        />
      </Route>
    </Routes>
  )
}

export default App
