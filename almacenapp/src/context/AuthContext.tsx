import { createContext, useContext, useEffect, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { SESSION_STORAGE_KEY, login as apiLogin, setUnauthorizedHandler } from '../api'
import type { UsuarioSesion } from '../types'

interface AuthState {
  usuario: UsuarioSesion | null
  token: string | null
}

const ESTADO_VACIO: AuthState = { usuario: null, token: null }

function leerSesionGuardada(): AuthState {
  try {
    const raw = localStorage.getItem(SESSION_STORAGE_KEY)
    if (!raw) return ESTADO_VACIO
    return JSON.parse(raw) as AuthState
  } catch {
    return ESTADO_VACIO
  }
}

interface AuthContextValue {
  usuario: UsuarioSesion | null
  isAuthenticated: boolean
  iniciarSesion: (email: string, password: string) => Promise<void>
  cerrarSesion: () => void
}

const AuthContext = createContext<AuthContextValue | null>(null)

export function AuthProvider({ children }: { children: ReactNode }) {
  const [state, setState] = useState<AuthState>(() => leerSesionGuardada())

  useEffect(() => {
    setUnauthorizedHandler(() => setState(ESTADO_VACIO))
    return () => setUnauthorizedHandler(null)
  }, [])

  async function iniciarSesion(email: string, password: string) {
    const respuesta = await apiLogin({ email, password })
    const nuevoEstado: AuthState = { usuario: respuesta.usuario, token: respuesta.token }
    localStorage.setItem(SESSION_STORAGE_KEY, JSON.stringify(nuevoEstado))
    setState(nuevoEstado)
  }

  function cerrarSesion() {
    localStorage.removeItem(SESSION_STORAGE_KEY)
    setState(ESTADO_VACIO)
  }

  const value = useMemo<AuthContextValue>(
    () => ({
      usuario: state.usuario,
      isAuthenticated: state.token !== null,
      iniciarSesion,
      cerrarSesion,
    }),
    [state],
  )

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>
}

export function useAuth(): AuthContextValue {
  const ctx = useContext(AuthContext)
  if (!ctx) {
    throw new Error('useAuth debe usarse dentro de <AuthProvider>')
  }
  return ctx
}
