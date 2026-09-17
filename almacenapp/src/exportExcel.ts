import * as XLSX from 'xlsx'

/**
 * Descarga un archivo .xlsx con una sola hoja a partir de un arreglo de objetos planos.
 * Cada key del primer objeto se usa como encabezado de columna, en el orden dado.
 */
export function exportarExcel(nombreArchivo: string, nombreHoja: string, filas: Record<string, unknown>[]) {
  const hoja = XLSX.utils.json_to_sheet(filas)
  const libro = XLSX.utils.book_new()
  XLSX.utils.book_append_sheet(libro, hoja, nombreHoja)
  XLSX.writeFile(libro, nombreArchivo.endsWith('.xlsx') ? nombreArchivo : `${nombreArchivo}.xlsx`)
}
