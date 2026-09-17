import { useState } from 'react'
import type { FormEvent } from 'react'
import { useAuth } from '../context/AuthContext'
import { cambiarPassword } from '../api'
import ErrorAlert, { errorMessage } from '../components/ErrorAlert'

export default function MiCuenta() {
  const { usuario } = useAuth()
  const [passwordActual, setPasswordActual] = useState('')
  const [passwordNueva, setPasswordNueva] = useState('')
  const [confirmacion, setConfirmacion] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [exito, setExito] = useState(false)
  const [guardando, setGuardando] = useState(false)

  function limpiarCampos() {
    setPasswordActual('')
    setPasswordNueva('')
    setConfirmacion('')
  }

  async function enviar(evento: FormEvent) {
    evento.preventDefault()
    setError(null)
    setExito(false)

    if (passwordNueva.length < 6) {
      setError('La nueva contraseña debe tener al menos 6 caracteres.')
      return
    }
    if (passwordNueva !== confirmacion) {
      setError('La confirmación no coincide con la nueva contraseña.')
      return
    }

    setGuardando(true)
    try {
      await cambiarPassword({ passwordActual, passwordNueva })
      setExito(true)
      limpiarCampos()
    } catch (err) {
      setError(errorMessage(err))
    } finally {
      setGuardando(false)
    }
  }

  return (
    <div>
      <div className="page-header">
        <div>
          <h1>Mi cuenta</h1>
          <p className="page-subtitle">{usuario?.nombre} — {usuario?.email}</p>
        </div>
      </div>

      <div className="card" style={{ maxWidth: 420 }}>
        <h2>Cambiar contraseña</h2>
        <ErrorAlert message={error} />
        {exito && <div className="alert alert-success">Contraseña actualizada correctamente.</div>}
        <form onSubmit={enviar}>
          <div className="field" style={{ marginBottom: 14 }}>
            <label>Contraseña actual</label>
            <input
              type="password"
              value={passwordActual}
              onChange={(e) => setPasswordActual(e.target.value)}
              required
              autoFocus
            />
          </div>
          <div className="field" style={{ marginBottom: 14 }}>
            <label>Nueva contraseña</label>
            <input
              type="password"
              value={passwordNueva}
              onChange={(e) => setPasswordNueva(e.target.value)}
              minLength={6}
              required
            />
          </div>
          <div className="field" style={{ marginBottom: 18 }}>
            <label>Confirmar nueva contraseña</label>
            <input
              type="password"
              value={confirmacion}
              onChange={(e) => setConfirmacion(e.target.value)}
              minLength={6}
              required
            />
          </div>
          <button className="btn btn-primary" type="submit" disabled={guardando}>
            {guardando ? 'Guardando…' : 'Cambiar contraseña'}
          </button>
        </form>
      </div>
    </div>
  )
}
