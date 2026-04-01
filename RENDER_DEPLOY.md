# Deploy en Render (PostgreSQL)

## Variables de entorno necesarias

1. `DATABASE_URL`
   - Render provee esto automáticamente si usás su servicio PostgreSQL.
   - Ejemplo (formato típico): `postgres://user:password@host:5432/dbname?sslmode=require`
2. `RUN_MIGRATIONS_ON_STARTUP` (opcional)
   - Si seteás `false`, se evita correr `db.Database.Migrate()` al arrancar.
   - Por defecto, en `Production` Render se ejecutan migraciones automáticamente.

## Cómo compilar y levantar

El proyecto incluye un `Dockerfile`, así que Render puede construir la imagen y ejecutar:

- `dotnet ResimamisBackend.dll`

## Notas importantes de la migración SQL Server -> PostgreSQL

- Se cambió el provider EF a `Npgsql` y se generó una migración inicial en `Migrations/`.
- El connection string NO está hardcodeado: se resuelve desde `DATABASE_URL` (o desde `ConnectionStrings:DefaultConnection` para desarrollo local).
- Se ajustaron consultas con `.Date` para mejorar compatibilidad con el proveedor PostgreSQL.

