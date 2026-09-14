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
import { useAuth } from './context/AuthContext'
import './App.css'

function RequireAuth({ children }: { children: ReactElement }) {
  const { isAuthenticated } = useAuth()
  const location = useLocation()

  if (!isAuthenticated) {
    return <Navigate to="/login" state={{ from: location.pathname }} replace />
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
        <Route path="productos" element={<Productos />} />
        <Route path="existencias" element={<Existencias />} />
        <Route path="movimientos" element={<Movimientos />} />
        <Route path="compras" element={<Compras />} />
        <Route path="salidas" element={<Salidas />} />
        <Route path="proveedores" element={<Proveedores />} />
        <Route path="clientes" element={<Clientes />} />
        <Route path="catalogos" element={<Catalogos />} />
      </Route>
    </Routes>
  )
}

export default App
