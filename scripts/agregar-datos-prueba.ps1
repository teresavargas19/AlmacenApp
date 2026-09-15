<#
.SYNOPSIS
  Llena AlmacenApp con datos de prueba (proveedores, clientes, categorias,
  productos, stock inicial, una compra y dos salidas) llamando a la API real.

.DESCRIPCION
  Usa la API en vez de insertar directo en SQL Server, para que el stock,
  las existencias y las validaciones queden consistentes (igual que si los
  hubieras creado a mano desde la pantalla).

.REQUISITOS
  - El backend debe estar corriendo: cd almacenapp/backend/AlmacenApi; dotnet run
  - Se ejecuta una sola vez. Si lo corres dos veces, fallara al intentar
    crear productos con un SKU que ya existe (es la validacion normal de
    la API funcionando).

.USO
  cd C:\Users\teres\source\repos\AlmacenApp\scripts
  .\agregar-datos-prueba.ps1

  Si PowerShell bloquea el script la primera vez, corre antes (una sola vez):
  Set-ExecutionPolicy -Scope CurrentUser RemoteSigned
#>

$ErrorActionPreference = "Stop"
$baseUrl = "http://localhost:5097/api"

function Invoke-Api {
    param(
        [Parameter(Mandatory)] [string]$Method,
        [Parameter(Mandatory)] [string]$Path,
        $Body = $null
    )
    $uri = "$baseUrl$Path"
    $headers = @{ Authorization = "Bearer $script:token" }
    if ($null -ne $Body) {
        $json = $Body | ConvertTo-Json -Depth 6
        return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers -ContentType "application/json" -Body $json
    }
    return Invoke-RestMethod -Method $Method -Uri $uri -Headers $headers
}

Write-Host "Conectando a $baseUrl ..." -ForegroundColor Cyan
try {
    Invoke-RestMethod -Method Get -Uri "http://localhost:5097/health" | Out-Null
} catch {
    Write-Host "No se pudo conectar al backend en http://localhost:5097." -ForegroundColor Red
    Write-Host "Abre otra terminal y corre: cd almacenapp\backend\AlmacenApi ; dotnet run" -ForegroundColor Red
    exit 1
}

Write-Host "Iniciando sesion como admin@almacenapp.com..." -ForegroundColor Cyan
$loginBody = @{ email = "admin@almacenapp.com"; password = "Admin123!" } | ConvertTo-Json
$loginResponse = Invoke-RestMethod -Method Post -Uri "$baseUrl/auth/login" -ContentType "application/json" -Body $loginBody
$script:token = $loginResponse.token
Write-Host "Sesion iniciada como $($loginResponse.usuario.nombre)." -ForegroundColor Green

# --- Proveedores ---
Write-Host "`nCreando proveedores..." -ForegroundColor Cyan
$proveedores = @(
    @{ nombre = "Distribuidora El Sol";  telefono = "809-555-0101"; email = "ventas@elsol.com";    direccion = "Santo Domingo"; activo = $true },
    @{ nombre = "Suministros Caribe";    telefono = "809-555-0102"; email = "contacto@caribe.com"; direccion = "Santiago";      activo = $true },
    @{ nombre = "Importadora Global RD"; telefono = "809-555-0103"; email = "info@globalrd.com";   direccion = "Santo Domingo"; activo = $true }
)
$proveedorIds = @()
foreach ($p in $proveedores) {
    $creado = Invoke-Api -Method Post -Path "/proveedores" -Body $p
    $proveedorIds += $creado.id
    Write-Host "  - $($p.nombre) (Id $($creado.id))"
}

# --- Clientes ---
Write-Host "`nCreando clientes..." -ForegroundColor Cyan
$clientes = @(
    @{ nombre = "Ferreteria Los Hermanos"; telefono = "809-555-0201"; email = "compras@loshermanos.com"; direccion = "Santo Domingo Este"; activo = $true },
    @{ nombre = "Constructora Reyes";      telefono = "809-555-0202"; email = "reyes@construccion.com";  direccion = "Santiago";           activo = $true }
)
$clienteIds = @()
foreach ($c in $clientes) {
    $creado = Invoke-Api -Method Post -Path "/clientes" -Body $c
    $clienteIds += $creado.id
    Write-Host "  - $($c.nombre) (Id $($creado.id))"
}

# --- Categorias adicionales (ya existe "General" de fabrica) ---
Write-Host "`nCreando categorias..." -ForegroundColor Cyan
$categorias = @("Herramientas", "Materiales de construccion", "Electricidad")
$categoriaIds = @{}
foreach ($nombre in $categorias) {
    $creado = Invoke-Api -Method Post -Path "/categorias" -Body @{ nombre = $nombre; activo = $true }
    $categoriaIds[$nombre] = $creado.id
    Write-Host "  - $nombre (Id $($creado.id))"
}

# --- Unidades de medida adicionales (ya existe "Unidad" de fabrica, Id 1) ---
Write-Host "`nCreando unidades de medida..." -ForegroundColor Cyan
$unidades = @(
    @{ nombre = "Caja";  abreviatura = "cja" },
    @{ nombre = "Metro"; abreviatura = "m" }
)
$unidadIds = @{}
foreach ($u in $unidades) {
    $creado = Invoke-Api -Method Post -Path "/unidadesmedida" -Body @{ nombre = $u.nombre; abreviatura = $u.abreviatura; activo = $true }
    $unidadIds[$u.nombre] = $creado.id
    Write-Host "  - $($u.nombre) (Id $($creado.id))"
}

# --- Productos ---
Write-Host "`nCreando productos..." -ForegroundColor Cyan
$productosDef = @(
    @{ sku = "HER-001"; nombre = "Martillo 16oz";        categoriaId = $categoriaIds["Herramientas"];               unidadMedidaId = 1;                       precio = 350;  stockMinimo = 5 },
    @{ sku = "HER-002"; nombre = "Taladro inalambrico";   categoriaId = $categoriaIds["Herramientas"];               unidadMedidaId = 1;                       precio = 4500; stockMinimo = 2 },
    @{ sku = "MAT-001"; nombre = "Cemento gris 42.5kg";   categoriaId = $categoriaIds["Materiales de construccion"]; unidadMedidaId = 1;                       precio = 320;  stockMinimo = 20 },
    @{ sku = "MAT-002"; nombre = "Varilla 3/8 x 20ft";    categoriaId = $categoriaIds["Materiales de construccion"]; unidadMedidaId = $unidadIds["Metro"];     precio = 280;  stockMinimo = 10 },
    @{ sku = "ELE-001"; nombre = "Cable THHN #12 (caja)"; categoriaId = $categoriaIds["Electricidad"];               unidadMedidaId = $unidadIds["Caja"];      precio = 5200; stockMinimo = 3 }
)
$productoIds = @()
foreach ($p in $productosDef) {
    $body = @{
        categoriaId    = $p.categoriaId
        unidadMedidaId = $p.unidadMedidaId
        sku            = $p.sku
        nombre         = $p.nombre
        stock          = 0
        stockMinimo    = $p.stockMinimo
        precio         = $p.precio
        activo         = $true
    }
    $creado = Invoke-Api -Method Post -Path "/productos" -Body $body
    $productoIds += $creado.id
    Write-Host "  - $($p.sku) $($p.nombre) (Id $($creado.id))"
}

# --- Stock inicial: una entrada por producto en el Almacen principal (Id=1) ---
Write-Host "`nRegistrando stock inicial (movimientos de entrada)..." -ForegroundColor Cyan
$cantidadesIniciales = @(15, 4, 100, 60, 25)
for ($i = 0; $i -lt $productoIds.Count; $i++) {
    Invoke-Api -Method Post -Path "/movimientosinventario" -Body @{
        productoId       = $productoIds[$i]
        almacenId        = 1
        tipoMovimientoId = 1
        cantidad         = $cantidadesIniciales[$i]
        referencia       = "Carga inicial de prueba"
    } | Out-Null
    Write-Host "  - $($productosDef[$i].sku): +$($cantidadesIniciales[$i]) unidades"
}

# --- Una compra de ejemplo, pendiente y luego confirmada ---
Write-Host "`nCreando una compra de ejemplo (Pendiente -> Completada)..." -ForegroundColor Cyan
$compraCreada = Invoke-Api -Method Post -Path "/compras" -Body @{
    proveedorId   = $proveedorIds[0]
    observaciones = "Compra de prueba generada por script"
    detalles      = @(
        @{ productoId = $productoIds[0]; cantidad = 10; precioUnitario = 340 },
        @{ productoId = $productoIds[2]; cantidad = 50; precioUnitario = 315 }
    )
}
Invoke-Api -Method Post -Path "/compras/$($compraCreada.id)/confirmar" -Body @{ almacenId = 1 } | Out-Null
Write-Host "  - Compra #$($compraCreada.id) confirmada"

# --- Una salida de ejemplo, pendiente y luego confirmada ---
Write-Host "`nCreando una salida de ejemplo (Pendiente -> Completada)..." -ForegroundColor Cyan
$salidaCreada = Invoke-Api -Method Post -Path "/salidas" -Body @{
    clienteId     = $clienteIds[0]
    observaciones = "Salida de prueba generada por script"
    detalles      = @(
        @{ productoId = $productoIds[0]; cantidad = 3 },
        @{ productoId = $productoIds[1]; cantidad = 1 }
    )
}
Invoke-Api -Method Post -Path "/salidas/$($salidaCreada.id)/confirmar" -Body @{ almacenId = 1 } | Out-Null
Write-Host "  - Salida #$($salidaCreada.id) confirmada"

# --- Una segunda salida para dejar el taladro por debajo de su minimo ---
# (stock inicial 4, minimo 2; -1 de la salida anterior, -3 aqui = 0, activa la alerta)
Write-Host "`nForzando una alerta de stock bajo (taladro por debajo del minimo)..." -ForegroundColor Cyan
$salidaBajoMinimo = Invoke-Api -Method Post -Path "/salidas" -Body @{
    clienteId     = $clienteIds[1]
    observaciones = "Salida para forzar alerta de stock bajo"
    detalles      = @( @{ productoId = $productoIds[1]; cantidad = 3 } )
}
Invoke-Api -Method Post -Path "/salidas/$($salidaBajoMinimo.id)/confirmar" -Body @{ almacenId = 1 } | Out-Null

Write-Host "`nListo. Se crearon:" -ForegroundColor Green
Write-Host "  - $($proveedorIds.Count) proveedores"
Write-Host "  - $($clienteIds.Count) clientes"
Write-Host "  - $($categoriaIds.Count) categorias adicionales"
Write-Host "  - $($productoIds.Count) productos con stock inicial"
Write-Host "  - 1 compra confirmada y 2 salidas confirmadas"
Write-Host "  - El taladro inalambrico quedo con existencia bajo el minimo (para probar la alerta)"
Write-Host "`nAbre http://localhost:52567 e inicia sesion con admin@almacenapp.com / Admin123! para verlos." -ForegroundColor Cyan
