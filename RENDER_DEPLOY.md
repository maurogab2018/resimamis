# Deploy en Render (Docker + PostgreSQL)

Backend **ASP.NET Core 8** (API + Swagger en la raíz), **EF Core + Npgsql**, migraciones al arranque salvo que desactives `RUN_MIGRATIONS_ON_STARTUP`.

## Resumen del proyecto

- **Entrada:** `Program.cs` — Kestrel usa la variable **`PORT`** si existe (Render la define siempre en Web Services).
- **Base de datos:** connection string desde **`DATABASE_URL`** (formato `postgres://...`) o `ConnectionStrings:DefaultConnection` en config local (`Datos/ConnectionStringResolver.cs`).
- **Imagen:** `Dockerfile` en la raíz — build multi-stage, publica `ResimamisBackend.dll`.

## Variables de entorno en Render

| Variable | Obligatoria | Notas |
|----------|-------------|--------|
| `DATABASE_URL` | Sí (producción) | Render la crea al **vincular** un PostgreSQL al Web Service, o copiala del panel de la base. |
| `PORT` | No | La setea Render; no hace falta definirla a mano. |
| `ASPNETCORE_ENVIRONMENT` | No | El `Dockerfile` ya pone `Production`. |
| `RUN_MIGRATIONS_ON_STARTUP` | No | Si vale `false`, no se ejecuta `Migrate()` al arrancar. |
| `Email__Enabled` | No (mail) | `true` para activar envío de correos (avisos de stock, etc.). |
| `Email__From` | Sí (mail) | Remitente autorizado por tu proveedor SMTP. |
| `Email__Smtp__Host` | Sí (mail) | Host SMTP (ej. `smtp.tuproveedor.com`). |
| `Email__Smtp__User` o `EMAIL_SMTP_USER` | Sí (mail) | Usuario SMTP. |
| `Email__Smtp__Password` o `EMAIL_SMTP_PASSWORD` | Sí (mail) | Contraseña o clave SMTP. |
| `Email__AvisoStockMinimo__Destinatarios__0` | No | Primer destinatario del aviso de stock (Render: índices 0, 1, …). |

En local podés seguir usando `appsettings.json`; en Render conviene **no** depender de secretos en el repo: usá solo `DATABASE_URL` y rotá credenciales si alguna vez quedó commiteada.

## Correo (SMTP genérico)

El backend usa SMTP estándar (`System.Net.Mail.SmtpClient`). Con `Email:Enabled` en `false` (default) no se envía nada.

Para activar en Render, configurá host, usuario, contraseña y remitente. Destinatarios del aviso de stock: `Email:AvisoStockMinimo:Destinatarios` o, si está vacío, mails de coordinadoras en la BD.

Probar: `POST /api/Insumo/avisoStockMinimo` (con JWT). También se dispara solo tras un movimiento de stock que deje un insumo bajo el mínimo.

## Pasos en Render (Docker)

1. **Subí el código** a GitHub/GitLab/Bitbucket (misma rama que vas a desplegar).
2. En [Render Dashboard](https://dashboard.render.com): **New +** → **PostgreSQL** (plan según necesidad). Anotá el nombre del servicio.
3. **New +** → **Web Service** → conectá el repositorio.
4. Configuración típica:
   - **Runtime:** Docker
   - **Dockerfile path:** `Dockerfile` (raíz del repo)
   - **Instance type:** la que necesites
5. En la sección **Environment** del Web Service:
   - **Link database:** elegí el PostgreSQL creado; Render inyecta `DATABASE_URL` automáticamente.
   - Si no usás “link”, agregá manualmente `DATABASE_URL` con el *Internal Database URL* o *External* según corresponda.
6. **Create Web Service**. El primer build ejecuta `docker build` y luego el contenedor arranca con `dotnet ResimamisBackend.dll`.
7. Cuando termine el deploy, abrí la URL pública del servicio: Swagger está en **`/`** (raíz), no en `/swagger`.

## Comprobar localmente con Docker

Desde la raíz del repo:

```bash
docker build -t resimamis-back .
docker run --rm -p 8080:8080 -e PORT=8080 -e DATABASE_URL="postgresql://usuario:clave@host:5432/nombre_db?sslmode=require" resimamis-back
```

Ajustá `DATABASE_URL` a una base accesible desde tu máquina (por ejemplo Postgres local).

## Notas (SQL Server → PostgreSQL)

- Provider EF: **Npgsql**; migraciones en `Migrations/`.
- Las URLs `postgres://` / `postgresql://` se normalizan a connection string Npgsql (SSL / `TrustServerCertificate` para PaaS).
