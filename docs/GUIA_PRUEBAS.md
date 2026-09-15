# Guía de pruebas manuales — AlmacenApp

Recorre esta lista de arriba hacia abajo probando en el navegador
(`http://localhost:52567`) con el backend corriendo (`http://localhost:5097`).
Marca cada casilla según lo vayas probando.

## 0. Preparación

- [ ] Backend corriendo: `cd almacenapp/backend/AlmacenApi && dotnet run`
- [ ] Frontend corriendo: `cd almacenapp && npm run dev`
- [ ] `http://localhost:5097/health` responde `{"status":"ok"}`
- [ ] La base de datos `AlmacenApp` tiene las tablas y los datos semilla (categoría "General", unidad "Unidad", almacén "Almacén principal", tipos de movimiento, rol "Administrador", usuario admin)

## 1. Autenticación

- [ ] Entrar a `http://localhost:52567` sin sesión iniciada redirige a `/login`
- [ ] Login con `admin@almacenapp.com` / `Admin123!` funciona y entra al Dashboard
- [ ] Login con contraseña incorrecta muestra un mensaje de error (no entra)
- [ ] Login con un correo que no existe muestra un mensaje de error
- [ ] El nombre y rol del usuario aparecen en la parte inferior del menú lateral
- [ ] "Cerrar sesión" saca de la app y vuelve a pedir login
- [ ] Después de cerrar sesión, entrar a una URL interna (ej. `/productos`) directamente redirige a `/login`

## 2. Catálogos (Categorías, Unidades de medida, Almacenes, Ubicaciones, Tipos de movimiento)

Para cada catálogo (Categorías, Unidades de medida, Almacenes):

- [ ] Crear un registro nuevo aparece en la lista de inmediato
- [ ] Editar un registro guarda los cambios
- [ ] Desactivar un registro lo saca de los combos de selección en otras pantallas (p. ej. una categoría inactiva no aparece al crear un producto)
- [ ] Ubicaciones: crear una ubicación dentro de un almacén y verificar que quede asociada a ese almacén
- [ ] Tipos de movimiento: se listan los 4 precargados (Entrada, Salida, Ajuste, Transferencia)

## 3. Proveedores y clientes

- [ ] Crear un proveedor y un cliente
- [ ] Editar sus datos (teléfono, email, dirección)
- [ ] Desactivar un proveedor sin compras registradas funciona
- [ ] Intentar desactivar/eliminar un proveedor **con compras registradas** debe rechazarse con un mensaje claro

## 4. Productos

- [ ] Crear un producto con SKU nuevo, categoría y unidad de medida
- [ ] Crear otro producto reutilizando el mismo SKU debe **rechazarse** ("Ya existe un producto con ese SKU")
- [ ] Editar nombre, precio y stock mínimo de un producto
- [ ] "Eliminar" un producto lo desactiva (deja de aparecer en el listado, pero no rompe movimientos/compras ya hechos sobre él)
- [ ] El stock mostrado en la lista de productos coincide con la suma de sus existencias por almacén

## 5. Existencias

- [ ] Ver existencias filtrando por producto y por almacén
- [ ] Configurar una cantidad mínima para un producto en un almacén
- [ ] La pantalla de "bajo mínimo" / el aviso del dashboard muestra los productos cuya existencia quedó por debajo del mínimo configurado
- [ ] Al subir la existencia por encima del mínimo, la alerta desaparece

## 6. Movimientos de inventario

- [ ] Registrar una **entrada**: la existencia y el stock del producto suben
- [ ] Registrar una **salida** con existencia suficiente: la existencia y el stock bajan
- [ ] Registrar una **salida mayor a la existencia disponible**: debe rechazarse ("No hay existencia suficiente...") y el stock no debe cambiar
- [ ] Registrar un **ajuste positivo**: suma directamente a la existencia
- [ ] Registrar un **ajuste negativo** que dejaría la existencia en negativo: debe rechazarse
- [ ] Registrar una **transferencia** entre dos almacenes: baja en el origen, sube en el destino, el stock total del producto no cambia
- [ ] Intentar una transferencia con el mismo almacén de origen y destino: debe rechazarse
- [ ] El historial de movimientos muestra todos los anteriores con fecha, tipo, producto y almacén correctos

## 7. Compras (flujo Pendiente → Confirmar/Cancelar)

- [ ] Crear una compra a un proveedor con una o varias líneas de producto: queda en estado **Pendiente** y **no** afecta el stock todavía
- [ ] Confirmar la compra eligiendo un almacén: el estado pasa a **Completada**, sube la existencia en ese almacén y se genera un movimiento de tipo Entrada por cada línea
- [ ] Intentar confirmar la misma compra otra vez: debe rechazarse (ya no está pendiente)
- [ ] Crear otra compra y **cancelarla** sin confirmar: pasa a **Cancelada** y nunca tocó el inventario
- [ ] Intentar confirmar o cancelar una compra ya cancelada: debe rechazarse

## 8. Salidas / ventas (flujo Pendiente → Confirmar/Cancelar)

- [ ] Crear una salida (con o sin cliente) con una o varias líneas: queda en **Pendiente**, sin afectar el stock
- [ ] Confirmar la salida eligiendo un almacén con existencia suficiente: pasa a **Completada**, baja la existencia y se genera un movimiento de tipo Salida
- [ ] Crear una salida pidiendo más cantidad de la que hay en existencia y confirmarla: debe rechazarse indicando qué producto(s) no tienen existencia suficiente, y el stock no debe cambiar
- [ ] Cancelar una salida pendiente: pasa a **Cancelada** sin tocar inventario
- [ ] Intentar confirmar/cancelar una salida que ya no está pendiente: debe rechazarse

## 9. Usuarios y roles (requiere sesión de Administrador)

- [ ] Crear un usuario nuevo desde la API o pantalla de usuarios (si existe): se guarda con la contraseña encriptada, nunca en texto plano
- [ ] Iniciar sesión con el usuario nuevo funciona
- [ ] Cambiar la contraseña de un usuario (`PUT /api/usuarios/{id}`) y volver a iniciar sesión con la nueva contraseña funciona; la anterior deja de funcionar
- [ ] Un usuario con un rol distinto de "Administrador" **no** puede crear, editar ni eliminar usuarios (debe recibir 403 Forbidden)
- [ ] Un usuario con rol "Administrador" sí puede hacerlo

## 10. Casos generales de error

- [ ] Pedir un recurso con un ID que no existe (ej. `GET /api/productos/99999`) responde 404
- [ ] Llamar cualquier endpoint protegido sin token (ej. con Postman, sin el header `Authorization`) responde 401
- [ ] Con el backend corriendo en `localhost:5097` y el frontend en `localhost:52567`, no deben aparecer errores de CORS en la consola del navegador

## 11. Seguridad / configuración (antes de mostrarlo fuera de tu máquina)

- [ ] Cambiada la contraseña del usuario admin por defecto (`Admin123!`)
- [ ] El repositorio de GitHub es **privado**, o la clave `Jwt:Key` de `appsettings.json` se movió a `dotnet user-secrets` / variables de entorno
