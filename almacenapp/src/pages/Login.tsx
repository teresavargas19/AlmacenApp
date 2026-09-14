import { useState } from 'react'
import type { FormEvent } from 'react'
import { Navigate, useLocation, useNavigate } from 'react-router-dom'
import { useAuth } from '../context/AuthContext'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

export default function Login() {
  const { isAuthenticated, iniciarSesion } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [email, setEmail] = useState('')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [enviando, setEnviando] = useState(false)

  if (isAuthenticated) {
    const destino = (location.state as { from?: string } | null)?.from ?? '/'
    return <Navigate to={destino} replace />
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    setEnviando(true)
    setError(null)
    try {
      await iniciarSesion(email, password)
      navigate('/', { replace: true })
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setEnviando(false)
    }
  }

  return (
    <div className="login-screen">
      <form className="login-card card" onSubmit={enviar}>
        <h1 className="sidebar-brand" style={{ padding: 0, marginBottom: 4 }}>
          Almacen<span>App</span>
        </h1>
        <p className="page-subtitle" style={{ marginBottom: 18 }}>
          Inicia sesión para continuar
        </p>
        <ErrorAlert message={error} />
        <div className="field" style={{ marginBottom: 14 }}>
          <label>Correo</label>
          <input
            type="email"
            value={email}
            onChange={(e) => setEmail(e.target.value)}
            required
            autoFocus
          />
        </div>
        <div className="field" style={{ marginBottom: 18 }}>
          <label>Contraseña</label>
          <input type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
        </div>
        <button
          className="btn btn-primary"
          type="submit"
          disabled={enviando}
          style={{ width: '100%', justifyContent: 'center' }}
        >
          {enviando ? 'Ingresando…' : 'Ingresar'}
        </button>
      </form>
    </div>
  )
}
