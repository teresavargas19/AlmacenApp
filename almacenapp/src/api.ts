import type {
  Almacen,
  Categoria,
  Cliente,
  CompraDetail,
  CompraListItem,
  Existencia,
  Movimiento,
  Producto,
  Proveedor,
  Rol,
  SalidaDetail,
  SalidaListItem,
  TipoMovimiento,
  Ubicacion,
  UnidadMedida,
  Usuario,
  UsuarioSesion,
} from './types'

const BASE_URL = (import.meta.env.VITE_API_BASE_URL as string | undefined) ?? 'http://localhost:5097/api'

export const SESSION_STORAGE_KEY = 'almacenapp.session'

interface SesionGuardada {
  token: string | null
}

function leerTokenGuardado(): string | null {
  try {
    const raw = localStorage.getItem(SESSION_STORAGE_KEY)
    if (!raw) return null
    return (JSON.parse(raw) as SesionGuardada).token
  } catch {
    return null
  }
}

let manejadorNoAutorizado: (() => void) | null = null

/** Registrado por AuthContext: se llama cuando la API responde 401 (token vencido o inválido). */
export function setUnauthorizedHandler(handler: (() => void) | null) {
  manejadorNoAutorizado = handler
}

export class ApiError extends Error {
  status: number

  constructor(status: number, message: string) {
    super(message)
    this.status = status
  }
}

async function request<T>(path: string, options?: RequestInit): Promise<T> {
  const token = leerTokenGuardado()
  const response = await fetch(`${BASE_URL}${path}`, {
    headers: {
      'Content-Type': 'application/json',
      ...(token ? { Authorization: `Bearer ${token}` } : {}),
    },
    ...options,
  })

  if (response.status === 401 && !path.startsWith('/auth/')) {
    manejadorNoAutorizado?.()
  }

  if (!response.ok) {
    let message = `Error ${response.status}`
    try {
      const body = await response.json()
      if (body?.message) {
        message = body.message as string
      }
    } catch {
      // Sin cuerpo JSON: se deja el mensaje genérico.
    }
    throw new ApiError(response.status, message)
  }

  if (response.status === 204) {
    return undefined as T
  }

  return (await response.json()) as T
}

function get<T>(path: string): Promise<T> {
  return request<T>(path)
}

function post<T>(path: string, body?: unknown): Promise<T> {
  return request<T>(path, {
    method: 'POST',
    body: body !== undefined ? JSON.stringify(body) : undefined,
  })
}

function put<T>(path: string, body: unknown): Promise<T> {
  return request<T>(path, { method: 'PUT', body: JSON.stringify(body) })
}

function del<T>(path: string): Promise<T> {
  return request<T>(path, { method: 'DELETE' })
}

// Categorías
export const getCategorias = () => get<Categoria[]>('/categorias')
export const crearCategoria = (data: { nombre: string; activo: boolean }) =>
  post<Categoria>('/categorias', { id: 0, ...data })
export const actualizarCategoria = (data: Categoria) => put<void>(`/categorias/${data.id}`, data)
export const eliminarCategoria = (id: number) => del<void>(`/categorias/${id}`)

// Unidades de medida
export const getUnidades = () => get<UnidadMedida[]>('/unidadesmedida')
export const crearUnidad = (data: { nombre: string; abreviatura: string; activo: boolean }) =>
  post<UnidadMedida>('/unidadesmedida', { id: 0, ...data })
export const actualizarUnidad = (data: UnidadMedida) => put<void>(`/unidadesmedida/${data.id}`, data)
export const eliminarUnidad = (id: number) => del<void>(`/unidadesmedida/${id}`)

// Almacenes
export const getAlmacenes = () => get<Almacen[]>('/almacenes')
export const crearAlmacen = (data: { nombre: string; direccion?: string | null; activo: boolean }) =>
  post<Almacen>('/almacenes', { id: 0, ...data })
export const actualizarAlmacen = (data: Almacen) => put<void>(`/almacenes/${data.id}`, data)
export const eliminarAlmacen = (id: number) => del<void>(`/almacenes/${id}`)

// Ubicaciones
export const getUbicaciones = (almacenId?: number) =>
  get<Ubicacion[]>(`/ubicaciones${almacenId ? `?almacenId=${almacenId}` : ''}`)

// Tipos de movimiento
export const getTiposMovimiento = () => get<TipoMovimiento[]>('/tiposmovimiento')

// Proveedores
export const getProveedores = () => get<Proveedor[]>('/proveedores')
export const crearProveedor = (data: Omit<Proveedor, 'id'>) => post<Proveedor>('/proveedores', { id: 0, ...data })
export const actualizarProveedor = (data: Proveedor) => put<void>(`/proveedores/${data.id}`, data)
export const eliminarProveedor = (id: number) => del<void>(`/proveedores/${id}`)

// Clientes
export const getClientes = () => get<Cliente[]>('/clientes')
export const crearCliente = (data: Omit<Cliente, 'id'>) => post<Cliente>('/clientes', { id: 0, ...data })
export const actualizarCliente = (data: Cliente) => put<void>(`/clientes/${data.id}`, data)
export const eliminarCliente = (id: number) => del<void>(`/clientes/${id}`)

// Productos
export const getProductos = () => get<Producto[]>('/productos')
export const crearProducto = (data: Omit<Producto, 'id' | 'stock' | 'creadoEn'>) =>
  post<Producto>('/productos', { id: 0, stock: 0, creadoEn: new Date().toISOString(), ...data })
export const actualizarProducto = (data: Producto) => put<void>(`/productos/${data.id}`, data)
export const eliminarProducto = (id: number) => del<void>(`/productos/${id}`)

// Existencias
export const getExistencias = (params?: { productoId?: number; almacenId?: number }) => {
  const query = new URLSearchParams()
  if (params?.productoId) query.set('productoId', String(params.productoId))
  if (params?.almacenId) query.set('almacenId', String(params.almacenId))
  const suffix = query.toString() ? `?${query.toString()}` : ''
  return get<Existencia[]>(`/existencias${suffix}`)
}
export const getExistenciasBajoMinimo = () => get<Existencia[]>('/existencias/bajo-minimo')
export const actualizarMinimo = (id: number, cantidadMinima: number) =>
  put<void>(`/existencias/${id}/minimo`, { cantidadMinima })

// Movimientos
export const getMovimientos = (params?: {
  productoId?: number
  almacenId?: number
  tipoMovimientoId?: number
}) => {
  const query = new URLSearchParams()
  if (params?.productoId) query.set('productoId', String(params.productoId))
  if (params?.almacenId) query.set('almacenId', String(params.almacenId))
  if (params?.tipoMovimientoId) query.set('tipoMovimientoId', String(params.tipoMovimientoId))
  const suffix = query.toString() ? `?${query.toString()}` : ''
  return get<Movimiento[]>(`/movimientosinventario${suffix}`)
}

export interface MovimientoCreatePayload {
  productoId: number
  almacenId: number
  ubicacionId?: number | null
  tipoMovimientoId: number
  cantidad: number
  referencia?: string | null
  observaciones?: string | null
}
export const registrarMovimiento = (data: MovimientoCreatePayload) =>
  post<Movimiento>('/movimientosinventario', data)

export interface TransferenciaPayload {
  productoId: number
  almacenOrigenId: number
  almacenDestinoId: number
  cantidad: number
  referencia?: string | null
  observaciones?: string | null
}
export const registrarTransferencia = (data: TransferenciaPayload) =>
  post<{ message: string }>('/movimientosinventario/transferencia', data)

// Compras
export const getCompras = (estado?: string) => get<CompraListItem[]>(`/compras${estado ? `?estado=${estado}` : ''}`)
export const getCompra = (id: number) => get<CompraDetail>(`/compras/${id}`)

export interface CompraCreatePayload {
  proveedorId: number
  observaciones?: string | null
  detalles: { productoId: number; cantidad: number; precioUnitario: number }[]
}
export const crearCompra = (data: CompraCreatePayload) => post<{ id: number }>('/compras', data)
export const confirmarCompra = (id: number, almacenId: number) =>
  post<{ message: string }>(`/compras/${id}/confirmar`, { almacenId })
export const cancelarCompra = (id: number) => post<void>(`/compras/${id}/cancelar`)

// Salidas
export const getSalidas = (estado?: string) => get<SalidaListItem[]>(`/salidas${estado ? `?estado=${estado}` : ''}`)
export const getSalida = (id: number) => get<SalidaDetail>(`/salidas/${id}`)

export interface SalidaCreatePayload {
  clienteId?: number | null
  observaciones?: string | null
  detalles: { productoId: number; cantidad: number }[]
}
export const crearSalida = (data: SalidaCreatePayload) => post<{ id: number }>('/salidas', data)
export const confirmarSalida = (id: number, almacenId: number) =>
  post<{ message: string }>(`/salidas/${id}/confirmar`, { almacenId })
export const cancelarSalida = (id: number) => post<void>(`/salidas/${id}/cancelar`)

// Roles
export const getRoles = () => get<Rol[]>('/roles')
export const crearRol = (data: { nombre: string; permisos?: string | null }) =>
  post<Rol>('/roles', { id: 0, ...data })
export const actualizarRol = (data: Rol) => put<void>(`/roles/${data.id}`, data)
export const eliminarRol = (id: number) => del<void>(`/roles/${id}`)

// Usuarios
export const getUsuarios = () => get<Usuario[]>('/usuarios')

export interface UsuarioCreatePayload {
  rolId: number
  nombre: string
  email: string
  password: string
}
export const crearUsuario = (data: UsuarioCreatePayload) => post<Usuario>('/usuarios', data)

export interface UsuarioUpdatePayload {
  id: number
  rolId: number
  nombre: string
  email: string
  activo: boolean
  password?: string | null
}
export const actualizarUsuario = (data: UsuarioUpdatePayload) => put<void>(`/usuarios/${data.id}`, data)
export const eliminarUsuario = (id: number) => del<void>(`/usuarios/${id}`)

// Autenticación
export interface LoginPayload {
  email: string
  password: string
}
export interface LoginResponse {
  token: string
  expiresAt: string
  usuario: UsuarioSesion
}
export const login = (data: LoginPayload) => post<LoginResponse>('/auth/login', data)
