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

## Backend

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
  "DefaultConnection": "Server=DESKTOP-DQH9CCJ;Database=AlmacenApp;Trusted_Connection=True;TrustServerCertificate=True"
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

## Frontend

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
