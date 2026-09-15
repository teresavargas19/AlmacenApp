# AlmacenApp

Sistema de gestión de inventario/almacén, con backend en **ASP.NET Core Web API (.NET 10)** y frontend en **React + TypeScript (Vite)**.

## Estructura del proyecto

```
AlmacenApp/
├── almacenapp/
│   ├── backend/
│   │   └── AlmacenApi/       # API REST (.NET 10 + EF Core + SQL Server)
│   └── src/                  # Frontend (React + TypeScript + Vite)
└── AlmacenApp.slnx
```

## Funcionalidades

- Login con autenticación JWT y roles de usuario.
- Catálogos: categorías, unidades de medida, almacenes, ubicaciones, tipos de movimiento, proveedores, clientes.
- Gestión de productos y existencias por almacén, con mínimos y alertas de bajo stock.
- Movimientos de inventario (entradas, salidas, transferencias entre almacenes).
- Compras a proveedores con flujo **Pendiente → Confirmar → Completada/Cancelada**.
- Salidas/ventas con el mismo flujo de confirmación.
- Panel (dashboard) con indicadores generales.

## Requisitos

- .NET SDK 10
- Node.js 18+ y npm
- SQL Server (autenticación de Windows), instancia local por ejemplo `DESKTOP-DQH9CCJ`

## Docker (todo junto: base de datos + backend + frontend)

Requiere [Docker Desktop](https://www.docker.com/products/docker-desktop/) instalado. Es la forma más rápida de levantar todo sin instalar .NET, Node ni SQL Server aparte — usa su propio SQL Server en un contenedor, separado del `DESKTOP-DQH9CCJ` que usas para desarrollo manual.

```powershell
copy .env.example .env
docker compose up --build
```

(`.env` ya viene en `.gitignore`, así que sus valores no se suben al repo; cámbialos antes de exponer esto más allá de tu máquina).

La primera vez, espera a ver en los logs algo como `Now listening on: http://+:8080` del backend — ahí ya aplicó las migraciones y sembró los datos iniciales automáticamente. Luego:

- Frontend: `http://localhost:52567`
- Backend: `http://localhost:5097`
- El usuario admin sembrado (`admin@almacenapp.com` / `Admin123!`) funciona igual aquí.

Para apagar todo: `docker compose down` (agrega `-v` si además quieres borrar los datos de esa base, para empezar de cero).

> Nota: esta base de datos en Docker es independiente de tu SQL Server de Windows — para llenarla de datos de prueba corre [`scripts/agregar-datos-prueba.ps1`](scripts/agregar-datos-prueba.ps1) apuntando a `http://localhost:5097/api` (es el valor por defecto del script).

¿Vas a instalar esto en otra computadora (por ejemplo, la de un cliente) y quien lo instale no es técnico? Ver [`docs/GUIA_INSTALACION_DOCKER.md`](docs/GUIA_INSTALACION_DOCKER.md) — guía paso a paso, pensada para alguien que nunca ha usado una terminal.

## Backend (manual, sin Docker)

```powershell
cd almacenapp/backend/AlmacenApi
dotnet restore
dotnet ef database update
dotnet run
```

La API queda disponible en `http://localhost:5097` (ver `Properties/launchSettings.json` si el puerto cambia).

### Configuración

En `appsettings.json`:

```json
"ConnectionStrings": {
  "AlmacenDb": "Server=DESKTOP-DQH9CCJ;Database=AlmacenApp;Trusted_Connection=True;TrustServerCertificate=True;MultipleActiveResultSets=True"
},
"Jwt": {
  "Key": "...",
  "Issuer": "AlmacenApi",
  "Audience": "AlmacenApp",
  "ExpiresMinutes": "480"
}
```

> ⚠️ La clave `Jwt:Key` es un secreto. En un entorno de producción no debe vivir en `appsettings.json`; usar `dotnet user-secrets` o variables de entorno.

### Usuario administrador por defecto (seed)

| Email | Contraseña |
|---|---|
| admin@almacenapp.com | Admin123! |

Cambiar esta contraseña después del primer inicio de sesión.

### Pruebas automatizadas

El backend tiene un proyecto de pruebas (xUnit + EF Core InMemory) que cubre la
lógica de negocio crítica: recálculo de stock, login, y los flujos de
Compras/Salidas/Movimientos de inventario (confirmar, cancelar, validaciones).

```powershell
cd almacenapp/backend/AlmacenApi.Tests
dotnet test
```

No requiere SQL Server ni datos previos: cada prueba usa su propia base en
memoria.

### Pruebas manuales

Ver [`docs/GUIA_PRUEBAS.md`](docs/GUIA_PRUEBAS.md) para un checklist paso a
paso de todo lo que se puede probar a mano en la app (login, catálogos,
productos, movimientos, compras, salidas, roles, etc.).

### Datos de prueba

Con el backend corriendo (`dotnet run`), [`scripts/agregar-datos-prueba.ps1`](scripts/agregar-datos-prueba.ps1)
llena la app con proveedores, clientes, productos con stock inicial, una
compra y dos salidas de ejemplo (llamando a la API real, así que respeta
todas las validaciones):

```powershell
cd scripts
.\agregar-datos-prueba.ps1
```

## Frontend (manual, sin Docker)

```powershell
cd almacenapp
npm install
npm run dev
```

El frontend queda disponible en `http://localhost:52567`.

Variable de entorno opcional (`.env` en `almacenapp/`):

```
VITE_API_BASE_URL=http://localhost:5097/api
```

Si no se define, se usa ese mismo valor por defecto.

## Stack

- Backend: ASP.NET Core Web API, Entity Framework Core (Code First), SQL Server, JWT Bearer, BCrypt.
- Frontend: React, TypeScript, Vite, React Router.
