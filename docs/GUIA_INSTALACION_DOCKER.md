# Guía de instalación con Docker — AlmacenApp

Para quién es esta guía: alguien que va a instalar AlmacenApp en una computadora nueva (una máquina de prueba, o la computadora de un negocio que la vaya a usar), **sin necesidad de instalar .NET, Node.js ni SQL Server por separado**. Todo lo instala y lo levanta Docker con un par de comandos.

## Qué se necesita antes de empezar

- [ ] Windows 10/11 de 64 bits (ver nota de Mac/Linux al final).
- [ ] Conexión a internet (para descargar Docker y los programas necesarios la primera vez).
- [ ] La carpeta del proyecto `AlmacenApp` completa.

## Paso 1: Instalar Docker Desktop

1. Ir a [docker.com/products/docker-desktop](https://www.docker.com/products/docker-desktop/) y descargar **Docker Desktop para Windows**.
2. Instalarlo dejando las opciones por defecto. Puede pedir reiniciar la computadora — si lo pide, reiniciar.
3. Abrir Docker Desktop después de instalar y esperar a que la ballena de la barra de tareas deje de "cargar" (arriba a la izquierda debe decir "Engine running" o similar).

> Si durante la instalación Windows pide activar "WSL2", seguir el enlace que aparece en pantalla para instalarlo y luego reiniciar Docker Desktop. Es un paso normal la primera vez, no un error.

## Paso 2: Copiar el proyecto a la computadora

Copiar la carpeta completa `AlmacenApp` a la computadora — por USB, por red, o clonando desde GitHub si tiene Git instalado:

```powershell
git clone https://github.com/teresavargas19/AlmacenApp.git
```

## Paso 3: Crear el archivo de configuración (`.env`)

Este archivo guarda la contraseña de la base de datos y la clave de seguridad de la app — cada instalación debe tener el suyo propio, por eso no viene ya creado.

1. Abrir la carpeta del proyecto (`AlmacenApp`, la que tiene `docker-compose.yml`).
2. Copiar el archivo `.env.example` y pegar la copia en la misma carpeta.
3. Renombrar esa copia a `.env` (exactamente así, sin `.example` al final).
   - Si el Explorador de Windows no deja nombrar un archivo que empiece con punto: abrir PowerShell en esa carpeta y escribir `copy .env.example .env`
4. Abrir `.env` con el Bloc de notas y reemplazar los dos valores de ejemplo por unos propios:
   - `MSSQL_SA_PASSWORD`: una contraseña fuerte (mínimo 8 caracteres, con mayúsculas, minúsculas, números y algún símbolo).
   - `JWT_KEY`: cualquier texto largo y aleatorio (mientras más largo y variado, mejor).
5. Guardar y cerrar.

## Paso 4: Levantar la aplicación

1. Abrir PowerShell (buscar "PowerShell" en el menú de inicio de Windows).
2. Ubicarse en la carpeta del proyecto: escribir `cd ` (con un espacio) y luego arrastrar la carpeta `AlmacenApp` hacia la ventana de PowerShell — completa la ruta sola. Presionar Enter.
3. Escribir:
   ```powershell
   docker compose up --build
   ```
   y presionar Enter.
4. Esperar. **La primera vez tarda varios minutos** porque descarga todo lo necesario (SQL Server, .NET, Node, nginx). Las siguientes veces es mucho más rápido.
5. Cuando el texto deja de moverse y aparece una línea parecida a `Now listening on: http://+:8080`, la aplicación ya está lista.

> ⚠️ No cerrar esta ventana de PowerShell mientras se esté usando la aplicación — ahí es donde corre todo. Se puede minimizar, pero no cerrar.

## Paso 5: Abrir y usar la aplicación

1. Abrir un navegador (Chrome, Edge, el que sea).
2. Ir a: **http://localhost:52567**
3. Iniciar sesión con el usuario que viene por defecto:
   - Usuario: `admin@almacenapp.com`
   - Contraseña: `Admin123!`
4. **Cambiar esta contraseña de inmediato** desde la app (Usuarios → editar el usuario admin), o crear un usuario nuevo para el uso diario y desactivar este.

## Para apagar la aplicación

En la misma ventana de PowerShell donde quedó corriendo, presionar `Ctrl + C`.

Si se cerró esa ventana sin querer, abrir otra en la carpeta del proyecto y escribir:
```powershell
docker compose down
```

## Para volver a prenderla otro día

No hace falta repetir todos los pasos — solo, desde la carpeta del proyecto:
```powershell
docker compose up
```
(sin `--build`, a menos que se haya actualizado el código del proyecto).

## Preguntas frecuentes / problemas comunes

| Problema | Qué hacer |
|---|---|
| "Docker Desktop no está corriendo" o el comando no responde | Abrir Docker Desktop manualmente y esperar a que cargue por completo antes de correr el comando. |
| "Port is already allocated" / "el puerto ya está en uso" | Algo más está usando el puerto 52567, 5097 o 1433. Si es una instalación anterior de AlmacenApp que quedó corriendo, primero correr `docker compose down`. |
| Se perdieron todos los datos después de apagar | Solo pasa si se usó `docker compose down -v` — el `-v` borra los datos guardados. Sin `-v`, los datos quedan intactos aunque se apague todo. |
| Se quiere empezar de cero (borrar todo y volver al estado inicial) | `docker compose down -v` y luego `docker compose up --build` otra vez. |
| La página no carga en `localhost:52567` | Revisar en la ventana de PowerShell que no haya errores en rojo, y confirmar que Docker Desktop siga abierto. |

## Nota para Mac / Linux

Los pasos son los mismos, cambiando:
- Docker Desktop para Mac (o Docker Engine, en Linux).
- Terminal en vez de PowerShell — los comandos `docker compose ...` son idénticos.
