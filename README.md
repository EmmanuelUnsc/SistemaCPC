# SistemaCPC: sistema de gestión del Colegio de Profesionales en Comunicación de Santa Cruz

Proyecto integrador de Proyecto de Sistemas I, Ingeniería de Software y Programación Web III (Univalle, 2026).
Lleva los afiliados del Colegio, sus aportes mensuales, las defensas de grado en las que participan como tribunal
y los reportes del directorio.

| Dato | Valor |
|---|---|
| Versión de .NET | .NET 10 (ASP.NET Core 10, Entity Framework Core 10) |
| Base de datos | SQL Server (en desarrollo, SQL Server Express LocalDB) |
| Repositorio | https://github.com/EmmanuelUnsc/SistemaCPC |

## Integrantes

| Integrante | Usuario de GitHub |
|---|---|
| Gorostiaga Gonzales Diego Emmanuel | [EmmanuelUnsc](https://github.com/EmmanuelUnsc) |
| Colautti Rodriguez Nicolas | [Niko1099](https://github.com/Niko1099) |
| Yabeta Arias Alejandro | [alejandroyabetar](https://github.com/alejandroyabetar) |

## Estructura

| Proyecto | Qué es | Dirección |
|---|---|---|
| `SistemaCPC.Api` | Web API REST en ASP.NET Core con Entity Framework Core y SQL Server | http://localhost:5100 (Swagger en `/swagger`) |
| `SistemaCPC.Web` | Pantallas en ASP.NET Core MVC con Bootstrap; llama a la API. Está en el repositorio, pero no forma parte de la entrega de la API | http://localhost:5210 |
| `SistemaCPC.Migracion` | Programa de consola que carga el Excel del Colegio (RF-21) y compara los saldos con el Excel (RNF-05). Escribe solo en la base `SistemaCPC_Real`: se niega a usar cualquier otra | — |

Dentro de `SistemaCPC.Api`: `Models/` (entidades), `Dtos/` (datos de entrada y salida), `Data/` (contexto de EF Core,
datos de prueba y migraciones), `Controllers/`, `Servicios/` (estado de cuenta, registro de pagos y ajustes con bitácora),
`Reportes/` (reportes y exportación a Excel), `Validaciones/` (reglas para registrar una defensa), `Configuracion/`
(parámetros del sistema) y `Seguridad/` (token de acceso).

## Configuración local

### 1. Requisitos

- SDK de .NET 10.
- SQL Server. Lo más simple es **SQL Server Express LocalDB**, que se instala con Visual Studio
  (carga de trabajo «Desarrollo de ASP.NET y web» o «Almacenamiento y procesamiento de datos»).
  Para comprobar que está: `sqllocaldb info` debe mostrar `MSSQLLocalDB`.
- La herramienta de migraciones de EF Core, si se aplican a mano: `dotnet tool install --global dotnet-ef`.

### 2. Conexión a SQL Server

La cadena de conexión está en `SistemaCPC.Api/appsettings.json`, clave `ConnectionStrings:CpcConexion`.
Con LocalDB no lleva usuario ni contraseña, porque usa la cuenta de Windows:

```
Server=(localdb)\MSSQLLocalDB;Database=SistemaCPC;Trusted_Connection=True;TrustServerCertificate=True
```

Si se usa otra instancia de SQL Server, se cambia solo esa línea. Ejemplos (sin credenciales reales):

```
Server=localhost;Database=SistemaCPC;Trusted_Connection=True;TrustServerCertificate=True
Server=localhost,1433;Database=SistemaCPC;User Id=USUARIO;Password=CONTRASEÑA;TrustServerCertificate=True
```

### 3. Migraciones

La API aplica las migraciones sola al arrancar (crea la base `SistemaCPC` si no existe). Para aplicarlas a mano,
desde la carpeta que contiene `SistemaCPC.Api`:

```
dotnet ef database update --project SistemaCPC.Api
```

Las migraciones están en `SistemaCPC.Api/Data/Migrations`. Para crear una nueva después de cambiar el modelo:
`dotnet ef migrations add <Nombre> --project SistemaCPC.Api -o Data/Migrations`.

### 4. Datos de prueba

Se cargan solos la primera vez que arranca la API con la base vacía (`SistemaCPC.Api/Data/DatosIniciales.cs`):
los usuarios de la tabla de abajo, los parámetros (aporte de Bs 35, Bs 208 por defensa), un pago y dos defensas.
Para empezar de cero: `dotnet ef database drop --project SistemaCPC.Api` y volver a arrancar la API.

### 5. Ejecutar y abrir Swagger

Solo la API (lo que lleva la entrega). Desde la terminal, en la carpeta que contiene `SistemaCPC.Api`:

```
dotnet run --project SistemaCPC.Api --launch-profile http
```

O con Visual Studio: abrir `SistemaCPC.Api/SistemaCPC.Api.csproj` y presionar Iniciar con el perfil **http**.

- Swagger: http://localhost:5100/swagger

Con el repositorio completo, también se pueden levantar la API y la Web juntas: en Visual Studio, abrir `SistemaCPC.sln`,
elegir el perfil **API + Web** (archivo `SistemaCPC.slnLaunch`) y presionar Iniciar; o desde la terminal, en dos ventanas:

```
dotnet run --project SistemaCPC.Api --launch-profile http
dotnet run --project SistemaCPC.Web --launch-profile http
```

- Pantallas: http://localhost:5210

En Swagger, primero `POST /api/auth/login` con un usuario de prueba, copiar el `token` de la respuesta, presionar
**Authorize** y pegarlo. Desde ese momento se pueden probar las demás rutas con los permisos de ese usuario.

### 6. Datos reales del Colegio

El Excel del Colegio y la base `SistemaCPC_Real` nunca van al repositorio: quedan solo en la PC donde se hace la migración.

Para cargar los datos, desde la carpeta del repositorio:

```
dotnet run --project SistemaCPC.Migracion -- "<ruta del Excel>" "<contraseña del admin>"
```

- Borra y vuelve a crear `SistemaCPC_Real` en LocalDB y carga afiliados, pagos, ajustes del sistema antiguo y defensas.
- La contraseña es la del usuario `admin` de la base real. Los afiliados entran con su número de registro y su carnet
  sin la expedición (si no tienen carnet, con su número de registro).
- Al terminar muestra el resumen de la comparación con el Excel y deja los informes en la carpeta temporal de Windows
  (`%TEMP%\SistemaCPC-Migracion`). Opcionales: una tercera cadena de conexión y una cuarta carpeta para los informes,
  siempre fuera del repositorio.

Para usar el sistema con esos datos: en Visual Studio, abrir `SistemaCPC.sln`, elegir el perfil
**API + Web (datos reales)** y presionar Iniciar; o desde la terminal, en dos ventanas:

```
dotnet run --project SistemaCPC.Api --launch-profile http-real
dotnet run --project SistemaCPC.Web --launch-profile http
```

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
| GET | `/api/afiliados/{registro}/pagos` | Pagos de un afiliado (recurso relacionado) | Directorio | |
| POST | `/api/afiliados` | CU-06 Registrar afiliado (con contraseña inicial; responde 201 con `Location`) | Directorio | |
| PUT | `/api/afiliados/{registro}` | CU-06 Actualizar afiliado | Directorio | |
| DELETE | `/api/afiliados/{registro}` | CU-06 Eliminar un afiliado cargado por error (204; 404 si no existe; 409 si tiene pagos, defensas o ajustes) | Directorio | |
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

Los patrones elegidos en las dinámicas de Ingeniería de Software. Cada clase está en la carpeta de su responsabilidad:

| Patrón | Archivo | Caso de uso |
|---|---|---|
| Singleton | `SistemaCPC.Api/Configuracion/ParametrosSistema.cs` (registrado con `AddSingleton`) | CU-02, CU-03, CU-07, CU-09 |
| Factory Method | `SistemaCPC.Api/Servicios/RegistroDePago.cs` | CU-04 y CU-05 |
| Builder | `SistemaCPC.Api/Reportes/ConstructorReporte.cs` | CU-09 |
| Facade | `SistemaCPC.Api/Servicios/EstadoDeCuenta.cs` (también la usan los reportes) | CU-02 y CU-03 |
| Proxy | En `SistemaCPC.Web` (repositorio, no en la entrega de la API): `SistemaCPC.Web/Servicios/ServicioAutenticacion.cs` (inicio de sesión) y `SistemaCPC.Web/Servicios/ProxyApi.cs` (el resto: agrega el token y lo renueva) | CU-01 y cada caso de uso que la Web conecta a la API |
| Decorator | `SistemaCPC.Api/Servicios/ServicioAjustes.cs` (`ServicioConBitacora`) | CU-10 y la corrección de pagos de CU-05 |
| Adapter | `SistemaCPC.Api/Reportes/ExportadorExcel.cs` (ClosedXML) | CU-09 |
| Chain of Responsibility | `SistemaCPC.Api/Validaciones/ValidadorDefensa.cs` | CU-07 |
| Template Method | `RegistroDePago.Registrar` en `SistemaCPC.Api/Servicios/RegistroDePago.cs` (con la operación gancho `FechaDelPago`) | CU-04 y CU-05 |
| Iterator | `foreach` y LINQ de C# en los reportes (`SistemaCPC.Api/Reportes/ConstructorReporte.cs`) | CU-09 |

Cada patrón está marcado en el código con un comentario `// Patrón …`.

## Modelo de datos

| Tabla | Clave | Relación |
|---|---|---|
| `Usuarios` | `Id` (registro único) | Afiliados, miembros del directorio y superadministradores en una sola tabla (columna `Tipo`) |
| `Pagos` | `Id` (número de recibo único) | Cada pago es de un afiliado (`AfiliadoId`); un afiliado tiene muchos pagos |
| `Defensas` | `Id` | Cada defensa tiene un tribunal (`AfiliadoId`); un tribunal tiene muchas defensas |
| `Ajustes` | `Id` | Cada ajuste es de un afiliado y lo registra un superadministrador |
| `Bitacora` | `Id` | Registro de cada ajuste y corrección de pago, con los valores anterior y nuevo |
| `FotosAfiliados` | `AfiliadoId` | Una foto por afiliado (uno a uno) |
| `Parametros` | `Id` | Una sola fila: aporte mensual, monto por defensa, umbral de inactividad y tope de defensas |

La deuda se calcula en bolivianos, como en el Excel del Colegio: meses desde el inicio de cobro (hasta la baja, si hay) ×
aporte mensual − pagos − ajustes. Si es negativa, es saldo a favor. El afiliado está inactivo si la deuda supera el umbral
(4 cuotas, Bs 140).

## Estado actual

- Funcionan de punta a punta (Web -> API -> SQL Server):
  - CU-01 Iniciar sesión, con token para la API y sesión que se cierra tras 15 minutos de inactividad (RNF-03).
  - Afiliado: Mi estado de cuenta (meses adeudados por año, total o saldo a favor e historial de pagos) y Pagar (pago simulado).
  - Directorio: Afiliados (registrar, editar, foto y baja) y Pagos (registrar un recibo de oficina y corregir un pago).
  - Superadministrador: Ajustes, con la bitácora.
- La API ya registra defensas (CU-07) y genera los reportes de CU-09, pero esas pantallas, la del saldo de tribunal (CU-03)
  y la de avisos todavía muestran datos de ejemplo (Sprint 3).
