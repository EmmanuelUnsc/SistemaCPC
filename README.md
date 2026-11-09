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

**Solo la primera vez**, crear el contenedor de la base de datos (desde esta carpeta):

```
docker compose up -d
```

### Con Visual Studio (2022 17.11 o posterior)

1. Abrir Docker Desktop y darle Play al contenedor `sistemacpc`.
2. Abrir `SistemaCPC.sln`.
3. En la barra de arranque, elegir el perfil **API + Web** (archivo `SistemaCPC.slnLaunch`) y presionar Iniciar.
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
| 0457 | 0457 | Afiliado (tribunal habilitado) |
| 0458 | 0458 | Afiliado |

## API

| Método | Ruta | Caso de uso | Patrón |
|---|---|---|---|
| POST | `/api/auth/login` | CU-01 Iniciar sesión | |
| GET | `/api/parametros` | Parámetros de los cálculos | Singleton |
| GET | `/api/pagos/{registro}` | CU-05 Gestionar pagos (consulta) | |
| POST | `/api/pagos/plataforma` | CU-04 Pagar aportes (simulado) | Factory Method |
| POST | `/api/pagos/oficina` | CU-05 Gestionar pagos | Factory Method |
| GET | `/api/reportes/defensas?desde=&hasta=` | CU-09 Defensas del período | Builder |
| GET | `/api/reportes/estados` | CU-09 Afiliados por estado | Builder |
| GET | `/api/reportes/matriz` | CU-09 Matriz de afiliados | Builder |

Se puede probar en http://localhost:5100/swagger o con `SistemaCPC.Api/SistemaCPC.Api.http`.

## Patrones de diseño

En `SistemaCPC.Api/Patrones/`, los patrones creacionales elegidos en la dinámica de Ingeniería de Software:

- `Singleton/ParametrosSistema.cs`: una sola instancia de los parámetros, registrada con `AddSingleton` en `Program.cs`.
- `FactoryMethod/RegistroDePago.cs`: el proceso común de registrar un pago; `RegistroPagoPlataforma` (CU-04) y
  `RegistroPagoOficina` (CU-05) deciden cómo se crea el recibo.
- `Builder/ConstructorReporte.cs`: los tres reportes de CU-09 se arman con los mismos pasos; `ServicioReportes` los ejecuta en orden.

## Estado actual

- CU-01 Iniciar sesión funciona de punta a punta: la Web envía los datos a la API, la API los valida
  contra PostgreSQL y la Web abre el panel del rol.
- La API ya registra pagos (CU-04 y CU-05) y genera los reportes de CU-09.
- Las pantallas de la Web siguen los mockups de Balsamiq con datos de ejemplo; todavía no llaman a esos endpoints.

## Base de datos

- Crear una migración después de cambiar el modelo:
  `dotnet ef migrations add <Nombre> --project SistemaCPC.Api -o Data/Migrations`
- Borrar la base de prueba y empezar de cero: `docker compose down -v`
