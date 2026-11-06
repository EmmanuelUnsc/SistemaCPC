# Sistema de gestión del CPC

Sistema de gestión del Colegio de Profesionales en Comunicación de Santa Cruz.
Proyecto de Sistemas I, Ingeniería de Software y Programación Web III (Univalle, 2026).

## Estructura

| Proyecto | Qué es | Dirección |
|---|---|---|
| `SistemaCPC.Api` | API REST en ASP.NET Core (.NET 10) con Entity Framework Core y PostgreSQL | http://localhost:5100 (Swagger en `/swagger`) |
| `SistemaCPC.Web` | Pantallas en ASP.NET Core MVC con Bootstrap; llama a la API | http://localhost:5210 |
| `docker-compose.yml` | Base de datos PostgreSQL para desarrollo | puerto 5432 |

## Cómo levantarlo

Requisitos: SDK de .NET 10 y Docker Desktop (abierto).

**Solo la primera vez**, crear la base de datos (desde esta carpeta):

```
docker compose up -d
```

Desde entonces la base arranca sola cada vez que se abre Docker Desktop.

### Con Visual Studio (2022 17.11 o posterior)

1. Abrir Docker Desktop.
2. Abrir `SistemaCPC.sln`.
3. En la barra de arranque, elegir el perfil **API + Web** (archivo `SistemaCPC.slnLaunch`) y presionar F5.
   Se inician los dos proyectos y se abre http://localhost:5210

### Desde la terminal

1. API, en una terminal:

   ```
   dotnet run --project SistemaCPC.Api --launch-profile http
   ```

2. Web, en otra terminal:

   ```
   dotnet run --project SistemaCPC.Web --launch-profile http
   ```

3. Abrir http://localhost:5210

Al arrancar, la API crea las tablas y carga los usuarios de prueba si la base está vacía
(`SistemaCPC.Api/Data/DatosIniciales.cs`).

## Usuarios de prueba

Son inventados. No cargar datos reales del Excel del Colegio.

| Registro | Contraseña | Rol |
|---|---|---|
| admin | admin123 | Superadministrador |
| dir01 | dir123 | Directorio |
| 0457 | 0457 | Afiliado |

## API

| Método | Ruta | Caso de uso | Respuesta |
|---|---|---|---|
| POST | `/api/auth/login` | CU-01 Iniciar sesión | 200 con id, registro, nombre y rol; 401 si el registro o la contraseña son incorrectos |

Se puede probar en http://localhost:5100/swagger o con `SistemaCPC.Api/SistemaCPC.Api.http`.

## Sprint 1

- CU-01 Iniciar sesión funciona de punta a punta: la Web envía los datos a la API, la API los valida
  contra PostgreSQL y la Web abre el panel del rol.
- Las demás pantallas (CU-02 a CU-13) siguen los mockups de Balsamiq con datos de ejemplo.
  Sus botones todavía no hacen nada: cada caso de uso se conecta a la API en los siguientes sprints.

## Base de datos

- Crear una migración después de cambiar el modelo:
  `dotnet ef migrations add <Nombre> --project SistemaCPC.Api -o Data/Migrations`
- Borrar la base de prueba y empezar de cero: `docker compose down -v`
