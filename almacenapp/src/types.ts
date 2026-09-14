export interface UsuarioSesion {
  id: number
  rolId: number
  rolNombre: string
  nombre: string
  email: string
  activo: boolean
}

export interface Categoria {
  id: number
  nombre: string
  activo: boolean
}

export interface UnidadMedida {
  id: number
  nombre: string
  abreviatura: string
  activo: boolean
}

export interface Almacen {
  id: number
  nombre: string
  direccion?: string | null
  activo: boolean
}

export interface Ubicacion {
  id: number
  almacenId: number
  nombre: string
  pasillo?: string | null
  estante?: string | null
  gaveta?: string | null
  activo: boolean
}

export interface TipoMovimiento {
  id: number
  nombre: string
  activo: boolean
}

export interface Proveedor {
  id: number
  nombre: string
  telefono?: string | null
  email?: string | null
  direccion?: string | null
  activo: boolean
}

export type Cliente = Proveedor

export interface Producto {
  id: number
  categoriaId: number
  unidadMedidaId: number
  sku: string
  nombre: string
  descripcion?: string | null
  stock: number
  stockMinimo: number
  precio: number
  activo: boolean
  creadoEn: string
}

export interface Existencia {
  id: number
  productoId: number
  productoNombre: string
  productoSku: string
  almacenId: number
  almacenNombre: string
  ubicacionId?: number | null
  ubicacionNombre?: string | null
  cantidad: number
  cantidadMinima: number
}

export interface Movimiento {
  id: number
  productoId: number
  productoNombre: string
  almacenId: number
  almacenNombre: string
  tipoMovimientoId: number
  tipoMovimientoNombre: string
  cantidad: number
  fecha: string
  referencia?: string | null
  observaciones?: string | null
}

export interface CompraListItem {
  id: number
  proveedorId: number
  proveedorNombre: string
  fecha: string
  estado: string
  observaciones?: string | null
  total: number
}

export interface CompraDetalleView {
  id: number
  productoId: number
  productoNombre: string
  productoSku: string
  cantidad: number
  precioUnitario: number
  subtotal: number
}

export interface CompraDetail {
  id: number
  proveedorId: number
  proveedorNombre: string
  fecha: string
  estado: string
  observaciones?: string | null
  detalles: CompraDetalleView[]
}

export interface SalidaListItem {
  id: number
  clienteId?: number | null
  clienteNombre?: string | null
  fecha: string
  estado: string
  observaciones?: string | null
  totalUnidades: number
}

export interface SalidaDetalleView {
  id: number
  productoId: number
  productoNombre: string
  productoSku: string
  cantidad: number
}

export interface SalidaDetail {
  id: number
  clienteId?: number | null
  clienteNombre?: string | null
  fecha: string
  estado: string
  observaciones?: string | null
  detalles: SalidaDetalleView[]
}
