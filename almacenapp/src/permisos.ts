import type { UsuarioSesion } from './types'

export interface PermisoModulo {
  clave: string
  etiqueta: string
}

/**
 * Catálogo de módulos que se pueden habilitar/deshabilitar por rol.
 * "Usuarios" y "Roles" quedan fuera de este catálogo a propósito: esas dos
 * pantallas siempre están restringidas al rol "Administrador" porque así lo
 * exige el backend (UsuariosController/RolesController usan
 * [Authorize(Roles = "Administrador")] en sus operaciones de escritura), así
 * que no tendría sentido poder dárselas a otro rol desde aquí.
 */
export const MODULOS_PERMISOS: PermisoModulo[] = [
  { clave: 'productos', etiqueta: 'Productos' },
  { clave: 'existencias', etiqueta: 'Existencias' },
  { clave: 'movimientos', etiqueta: 'Movimientos de inventario' },
  { clave: 'compras', etiqueta: 'Compras' },
  { clave: 'salidas', etiqueta: 'Salidas / ventas' },
  { clave: 'proveedores', etiqueta: 'Proveedores' },
  { clave: 'clientes', etiqueta: 'Clientes' },
  { clave: 'catalogos', etiqueta: 'Catálogos (categorías, unidades, almacenes, ubicaciones)' },
]

/** "*" en Permisos significa acceso total (así queda seeded el rol Administrador). */
export const PERMISO_TODO = '*'

export function listaPermisos(permisos: string | null | undefined): string[] {
  if (!permisos) return []
  return permisos
    .split(',')
    .map((p) => p.trim())
    .filter(Boolean)
}

export function tienePermiso(usuario: UsuarioSesion | null, clave: string): boolean {
  if (!usuario) return false
  if (usuario.rolNombre === 'Administrador') return true
  const permisos = (usuario.permisos ?? '').trim()
  if (permisos === PERMISO_TODO) return true
  return listaPermisos(permisos).includes(clave)
}
