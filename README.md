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

Todas las rutas, salvo el inicio de sesión, piden el token que devuelve `POST /api/auth/login`
(encabezado `Authorization: Bearer <token>`). El token dura 15 minutos y se renueva con `POST /api/auth/renovar`.
Un afiliado solo puede ver su propia información (RF-02).

| Método | Ruta | Caso de uso | Quién | Patrón |
|---|---|---|---|---|
| POST | `/api/auth/login` | CU-01 Iniciar sesión (devuelve el token) | Todos | |
| POST | `/api/auth/renovar` | Renovar el token antes de que venza | Con sesión | |
| GET | `/api/parametros` | Parámetros de los cálculos | Con sesión | Singleton |
| PUT | `/api/parametros` | Cambiar los parámetros | Directorio | Singleton |
| GET | `/api/estado-cuenta/{registro}` | CU-02 Consultar estado de cuenta (meses adeudados por año) | El afiliado o el directorio | Facade |
| GET | `/api/estado-cuenta/{registro}/tribunal` | CU-03 Consultar saldo de tribunal | El afiliado o el directorio | Facade |
| GET | `/api/pagos/{registro}` | CU-05 Gestionar pagos (consulta) | El afiliado o el directorio | |
| POST | `/api/pagos/plataforma` | CU-04 Pagar aportes (simulado, del afiliado que inició sesión) | Afiliado | Factory Method |
| POST | `/api/pagos/oficina` | CU-05 Registrar el pago de un recibo físico | Directorio | Factory Method |
| PUT | `/api/pagos/{numeroRecibo}` | CU-05 Corregir pago (queda en la bitácora) | Directorio | Decorator |
| GET | `/api/afiliados` | CU-06 Gestionar afiliados (lista con estado y deuda) | Directorio | |
| GET | `/api/afiliados/{registro}` | CU-06 Ver un afiliado | Directorio | |
| POST | `/api/afiliados` | CU-06 Registrar afiliado (con contraseña inicial) | Directorio | |
| PUT | `/api/afiliados/{registro}` | CU-06 Actualizar afiliado | Directorio | |
| POST | `/api/afiliados/{registro}/baja` | CU-06 Registrar baja (RF-25) | Directorio | |
| PUT / GET | `/api/afiliados/{registro}/foto` | CU-06 Guardar y ver la foto (JPG o PNG, hasta 2 MB) | Directorio | |
| GET | `/api/reportes/defensas?desde=&hasta=` | CU-09 Defensas del período | Directorio | Builder |
| GET | `/api/reportes/estados` | CU-09 Afiliados por estado | Directorio | Builder |
| GET | `/api/reportes/matriz` | CU-09 Matriz de afiliados | Directorio | Builder |
| GET | `/api/reportes/{tipo}/excel` | CU-09 Descargar en Excel (`defensas`, `estados`, `matriz`) | Directorio | Builder + Adapter |
| GET | `/api/defensas` | CU-07 Gestionar defensas (consulta) | Directorio | |
| POST | `/api/defensas` | CU-07 Registrar defensa | Directorio | Chain of Responsibility |
| POST | `/api/ajustes` | CU-10 Registrar ajuste | Superadministrador | Decorator |
| GET | `/api/ajustes/bitacora` | Bitácora de ajustes y correcciones | Directorio | |

Se puede probar en http://localhost:5100/swagger: primero `POST /api/auth/login`, copiar el `token` de la respuesta
y pegarlo en el botón **Authorize**. También con `SistemaCPC.Api/SistemaCPC.Api.http`, que inicia sesión y usa el token solo.

La clave con la que se firman los tokens está en `SistemaCPC.Api/appsettings.json` (`Token:Clave`) y es solo para desarrollo.

## Patrones de diseño

Los patrones elegidos en las dinámicas de Ingeniería de Software, en las carpetas `Patrones/`:

| Patrón | Archivo | Caso de uso |
|---|---|---|
| Singleton | `SistemaCPC.Api/Patrones/Singleton/ParametrosSistema.cs` (registrado con `AddSingleton`) | CU-02, CU-03, CU-07, CU-09 |
| Factory Method | `SistemaCPC.Api/Patrones/FactoryMethod/RegistroDePago.cs` | CU-04 y CU-05 |
| Builder | `SistemaCPC.Api/Patrones/Builder/ConstructorReporte.cs` | CU-09 |
| Facade | `SistemaCPC.Api/Patrones/Facade/EstadoDeCuenta.cs` (también la usan los reportes) | CU-02 y CU-03 |
| Proxy | `SistemaCPC.Web/Patrones/Proxy/ServicioAutenticacion.cs` (inicio de sesión) y `ProxyApi.cs` (el resto: agrega el token y lo renueva) | CU-01 y cada caso de uso que la Web conecta a la API |
| Decorator | `SistemaCPC.Api/Patrones/Decorator/ServicioAjustes.cs` (`ServicioConBitacora`) | CU-10 y la corrección de pagos de CU-05 |
| Adapter | `SistemaCPC.Api/Patrones/Adapter/ExportadorExcel.cs` (ClosedXML) | CU-09 |
| Chain of Responsibility | `SistemaCPC.Api/Patrones/ChainOfResponsibility/ValidadorDefensa.cs` | CU-07 |
| Template Method | `RegistroDePago.Registrar` en `Patrones/FactoryMethod/RegistroDePago.cs` (con la operación gancho `FechaDelPago`) | CU-04 y CU-05 |
| Iterator | `foreach` y LINQ de C# en los reportes (`Patrones/Builder/ConstructorReporte.cs`) | CU-09 |

Cada patrón está marcado en el código con un comentario `// Patrón …`.

## Estado actual

- Funcionan de punta a punta (Web -> API -> PostgreSQL):
  - CU-01 Iniciar sesión, con token para la API y sesión que se cierra tras 15 minutos de inactividad (RNF-03).
  - Afiliado: Mi estado de cuenta (meses adeudados por año, ajustes, total e historial de pagos) y Pagar (pago simulado).
  - Directorio: Afiliados (registrar, editar, foto y baja) y Pagos (registrar un recibo de oficina y corregir un pago).
  - Superadministrador: Ajustes, con la bitácora.
- La API ya registra defensas (CU-07) y genera los reportes de CU-09, pero esas pantallas, la del saldo de tribunal (CU-03)
  y la de avisos todavía muestran datos de ejemplo (Sprint 3).

## Base de datos

- Antes de una demostración, empezar con los datos de prueba limpios: `docker compose down -v` y luego `docker compose up -d`.

- Crear una migración después de cambiar el modelo:
  `dotnet ef migrations add <Nombre> --project SistemaCPC.Api -o Data/Migrations`
- Borrar la base de prueba y empezar de cero: `docker compose down -v`
