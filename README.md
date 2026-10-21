# CineSpace

Aplicación del cine construida desde cero con el patrón de las referencias y las pantallas de `VersionFinal`: tres proyectos .NET 8.

| Proyecto | Responsabilidad |
| --- | --- |
| CineSpace.Client | Blazor WebAssembly, componentes Razor, estilos y clientes HTTP. |
| CineSpace.Server | API, autorización, casos de uso, repositorios, Identity, EF Core e integraciones. |
| CineSpace.Shared | Contratos, DTOs y reglas de presentación compartidas, sin secretos ni acceso a datos. |

Incluye catálogo manual/TMDB, distribuidores, salas y diseños, funciones, reserva con butacas FIFO, pago simulado, QR/PDF, cuentas y Google, compras y reseñas, confitería, verificación, administración, analítica y exportación de reseñas XLSX. Los filtros, páginas, totales y agregados se calculan en el servidor. El navegador conserva únicamente preferencias y el borrador del carrito.

## Arranque con un clic

La interfaz y la API arrancan juntas desde **CineSpace.Server**, en **https://localhost:7043**. No hace falta abrir dos terminales ni iniciar Client por separado.

1. La primera vez, el propietario ejecuta **update_DB.bat**. Aplica el único SQL y muestra los conteos finales. Usa directamente la conexión PostgreSQL privada del JSON Development; no pide token de CLI. **Reconstruye el esquema cine y elimina su contenido anterior.**
2. Haz doble clic en **iniciar_CineSpace.bat**. Compila, comprueba que las tablas existen, inicia el servidor y abre el navegador. Si ya está iniciado, reutiliza ese servidor. El arranque no aplica SQL ni reconstruye datos.
3. Para detenerlo, usa **detener_CineSpace.bat**.

En Visual Studio abre la solución y selecciona el perfil **CineSpace completo**, o inicia **CineSpace.Server / https** con F5. El perfil compartido está en CineSpace.slnLaunch, y también se preparó el archivo local equivalente al utilizado por DS2. La referencia Server → Client incorpora los recursos del frontend a la ejecución y publicación del servidor.

Alternativa por terminal:

```powershell
dotnet run --project CineSpace.Server --launch-profile https
```

Swagger: https://localhost:7043/swagger. Los logs del lanzador están en `.local/`, fuera de Git y Docker. Se requiere SDK .NET 8.0.425 o posterior de la banda 8.0.4xx. Google conserva su callback autorizado `https://localhost:7043/signin-google`; su retorno al frontend ahora usa el mismo puerto 7043.

Para comprobar la base sin modificarla: `update_DB.bat comprobar`. La ventana del BAT permanece abierta para que se vea el resultado o el error.

## Cuentas de prueba

| Rol | Correo | Contraseña |
| --- | --- | --- |
| Administrador | admin@admin.com | Contraseña/123 |
| Cliente | cliente@cliente.com | Contraseña/123 |
| Trabajador | trabajador@trabajador.com | Contraseña/123 |

El único SQL incluye 30 tablas, 47 cuentas con hashes Identity independientes, 27 películas relacionadas con 14 distribuidoras, 34 salas, 5 diseños, 71 reseñas y actividad de cine/confitería durante 30 días. Hay siete preventas con funciones y 26 películas con varios géneros. La exploración pública muestra 17 géneros con contenido. Las próximas funciones incluyen ocupación completa, media y libre en todas las salas, con compras de 21 clientes diferentes. Las fechas se ajustan al día en que se ejecuta el BAT. El Anfiteatro tiene 342 butacas y es la sala más grande; ninguna sala usa Gran Formato. No hay contraseñas de Google inventadas ni guardadas.

Después de cambiar el seed, el propietario debe ejecutar `update_DB.bat` para ver los nuevos datos en Supabase. Si cambia la estructura del SQL, debe aplicarla antes de desplegar el código que la utiliza. El arranque nunca reconstruye datos automáticamente. El flujo de venta anticipada está en [PREVENTAS](docs/PREVENTAS.md).

## Configuración y despliegue

Las credenciales están en `CineSpace.Server/appsettings.Development.json`, excluido de Git, Docker y publicación. El JSON global y los ejemplos conservan la estructura con campos privados vacíos. Los archivos de Client son públicos y nunca reciben claves privadas.

`Dockerfile`, `compose.yaml` y `render.yaml` publican únicamente el backend en Render; `netlify.toml` aloja el frontend PWA en Netlify con proxy de API y Google hacia Render. Debug conserva el servidor con frontend integrado para iniciar localmente con un clic. El contexto Docker es esta carpeta `CineSpaceFS`. Los pasos, variables exactas y callbacks están en [DESPLIEGUE](docs/DESPLIEGUE.md); la configuración privada se describe en [CONFIGURACION_ENTORNOS](docs/CONFIGURACION_ENTORNOS.md).

Consulta [cobertura funcional](docs/IMPLEMENTACION.md), [pruebas y límites verificados](docs/VALIDACION.md), [conexiones y BAT](docs/CONEXIONES.md) y [estado de los servicios externos](docs/ESTADO_CONFIGURACION.md).


## Rendimiento y acceso

Las consultas de catálogo, administración y analítica tienen caché en el servidor, separada por cuenta y limitada a 32 MiB. Crear, editar o eliminar cambia su versión para invalidar resultados anteriores. El cliente reutiliza lecturas durante 20 segundos, agrupa solicitudes idénticas y limpia su caché después de escribir o cambiar de sesión. Butacas ocupadas, cotizaciones y validaciones de QR se consultan en vivo; el servidor vuelve a validar precios y disponibilidad al finalizar.

La cartelera pública devuelve 8 películas por página desde el backend y muestra el paginado junto a sus resultados. El encabezado se presenta antes de cargar WebAssembly; el generador de PDF se descarga únicamente cuando se necesita. Los botones esperan solicitudes reales. Los tres mensajes del pago conservan la simulación de 1,5 segundos y muestran un indicador giratorio.

La primera visita utiliza modo oscuro; cambiarlo conserva la elección. Las solicitudes de sesión, referencias, banners y primera página comienzan mientras se descarga WebAssembly, y el cliente consume esas respuestas sin duplicarlas. No se guardan catálogos ni sesiones en localStorage. Los géneros y sus cantidades se calculan sobre todo el catálogo público desde el backend. El menú móvil es opaco, bloquea el desplazamiento del fondo y libera la navegación al cerrarse.

La sesión usa una cookie persistente HttpOnly/Secure de 30 días; no mantiene procesos de sesión activos en el servidor ni guarda el token en localStorage. Los registros requieren confirmación de correo. Un correo pendiente no recibe envíos repetidos y puede registrarse nuevamente después de 24 horas. Los enlaces usan `Frontend:Url` del entorno; producción requiere su URL HTTPS pública.

Los recibos PDF contienen texto y líneas vectoriales, con QR en la esquina superior derecha. La sección de respaldo de la base del prototipo fue retirada; se conserva la exportación de reseñas.
