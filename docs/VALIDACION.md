# Validación — 20/10/2026

## Analítica de ocupación — 21/10/2026

El primer gráfico ahora es una torta SVG de funciones completas, con disponibilidad y sin ventas. Tiene filtros propios por fecha de función y experiencia, detalle de butacas, cantidades y porcentajes en la leyenda y tabla accesible. La ocupación por horario comparte esos filtros y mantiene el eje entre 0 y 100 %. El ranking histórico de cinco películas incluye tickets, ventas e ingresos. Los filtros superiores conservan su alcance exclusivo sobre los indicadores.

Se aprobaron 38 comprobaciones en PostgreSQL aislado: resultados contra SQL independiente, permisos, caché, filtros, fecha sin funciones, 24 horarios sin ventas con desplazamiento interno, torta de una sola categoría, colores consistentes al existir estados en cero, teclado, tablas, temas claro/oscuro y anchos de 320, 390, 768, 1024 y 1440 px. Publicaciones Release de cliente y servidor correctas. Se retiraron los programas temporales al terminar.

La ocupación se obtiene con una consulta agregada acotada al intervalo UTC del día, sin cargar butacas individuales en .NET ni en el cliente. Se añadieron caché de 30 segundos con invalidación tras escrituras y el índice `funciones_fecha_experiencia` al SQL único y al mapeo EF. El plan PostgreSQL comprobó el uso de ese índice y del índice de butacas por función: ejecución local de 2,497 ms, sin considerarla una garantía de producción. El índice remoto queda pendiente de la aplicación manual del SQL por el propietario; el código también funciona sin él.

Informes: [38 comprobaciones](validacion/dashboard-occupancy-report.json), [plan de consulta](validacion/dashboard-occupancy-query-plan.json), [escritorio](validacion/dashboard-occupancy-desktop.png) y [móvil](validacion/dashboard-occupancy-mobile.png). No se modificó Supabase remoto.

## Tema, carga inicial, navegación y datos — 21/10/2026

Esta revisión se probó en PostgreSQL 18 aislado, ejecutando el SQL único completo dos veces para verificar la reconstrucción. No se ejecutó el BAT ni se modificó Supabase remoto.

- 53 comprobaciones de preventas: apertura/cierre administrativo, filtros y paginación del servidor, programación desde el estreno, prohibición de compras cerradas, compra completa con QR, idempotencia, butacas duplicadas y protección de fechas con ventas.
- 105 visitas de las pantallas principales a 320, 390, 768, 1024 y 1440 px; otras 30 visitas de cuenta, seguridad, compras, pedidos, recibo y acceso por correo a 320, 390 y 768 px. Sin desbordamiento de página, solapamiento del encabezado, errores de renderizado en consola ni aviso de error de Blazor. Las tablas extensas conservan su desplazamiento interno.
- Primera visita oscura incluso con sistema claro, persistencia de una elección clara, menú opaco por encima del contenido, bloqueo del desplazamiento del fondo y cierre con Escape.
- 17 géneros públicos con cantidades calculadas desde todo el catálogo; 16 géneros asociados a películas y un documental anunciado. 26 de las 27 películas tienen varios géneros.
- Ocupación en las 34 salas: cada una tiene funciones completas, a media capacidad y libres. En la ejecución verificada se generaron 4.722 compras adicionales de 21 clientes, con 18.669 butacas. No hay asientos inexistentes, exceso de capacidad, diferencias entre entradas y butacas ni importes inconsistentes.
- Publicaciones Release del servidor y del cliente correctas; el script de Netlify incluye los nuevos recursos de arranque.

Las solicitudes principales ahora se inician durante la descarga de WebAssembly y sus respuestas se consumen una sola vez. En la muestra local, la sesión pasó de comenzar a 760 ms a 38 ms, y la primera página de cartelera de 1.279 ms a 39 ms. La pantalla con películas pasó de 1,90 s a 1,43 s. Son mediciones locales con API disponible y caché del servidor caliente; no son tiempos garantizados de producción ni una medición de un Render dormido.

Informes: [pantallas y tema](validacion/mobile-after-report.json), [cuenta y pedidos](validacion/mobile-account-report.json), [comparación inicial](validacion/mobile-before-report.json), [ocupación SQL](validacion/realistic-seed-report.json) y [preventas](validacion/presales-report.json). Se conservaron las capturas y se retiraron los programas de prueba al finalizar.

Si el backend utiliza una instancia gratuita de Render, la plataforma la suspende después de 15 minutos sin tráfico y el siguiente acceso puede tardar aproximadamente un minuto en reactivarla. Adelantar las solicitudes reduce la espera encadenada, pero no elimina esa limitación de infraestructura. Véase [Render: instancias gratuitas](https://render.com/docs/free).

---

Las pruebas de negocio utilizaron PostgreSQL 17.10 temporal, con el mismo SQL y los mismos servicios de la aplicación. No se ejecutó la reconstrucción en Supabase remoto.

| Comprobación | Resultado |
| --- | --- |
| Unitarias de dominio | 46 correctas: FIFO, coordenadas, disponibilidad, limpieza, precios, permisos de pago, QR, hashes, GUID, WebP y modelo EF. |
| API y respaldo | 37 correctas: roles/antiforgery, filtros y páginas, compras, idempotencia, horarios, uso de salas, ingreso, pedidos, XLSX y restauración. |
| CRUD de catálogos | 11 correctas: tipos, experiencias, categorías, películas/distribuidoras, combos, cotización, banners, estrenos y planos irregulares. |
| Cuentas y concurrencia | 7 correctas: confirmación, recuperación, token de un uso, solicitud por cuenta, compra simultánea y challenge Google. |
| Compra visual | 21 comprobaciones correctas a 1440 y 390 px: ticket general, FIFO, métodos, tiempos del modal, QR legible, descargas y ausencia de desbordamiento. |
| Publicación Release | Compra completa repetida con éxito en escritorio y móvil, sirviendo Client y API juntos desde los archivos publicados. |
| Perfil y confitería visual | 18 correctas: seis tarjetas, simetría/expansión, paginación/orden, compra histórica, invitado, persistencia al recargar y lectura de imágenes QR para ingreso/retiro. |
| Barrido de pantallas | 23 rutas en escritorio y móvil: 46 visitas sin errores de renderizado ni desbordamiento horizontal de página. |
| Integraciones externas | Búsqueda/ficha TMDB; WebP subido, leído y eliminado en Storage; confirmación aceptada por Brevo. |

El PDF real descargado se renderizó para inspección: una página A4, datos legibles, total, método de pago y QR en la esquina superior derecha. La compra se probó con QR y con tarjeta de prueba. La lectura de imágenes de boletos y pedidos se verificó en la interfaz; no se probó una cámara física.

La revisión NuGet no detectó paquetes vulnerables conocidos en los tres proyectos después de sustituir la dependencia Excel y actualizar los parches criptográficos. Compilación y publicación Release correctas. Los resultados y capturas seleccionadas están en `validacion/`. Los programas temporales de prueba y su PostgreSQL local se retiraron al terminar, como solicitó el propietario; se conservaron los informes.

Límites: PostgreSQL remoto ya pasó autenticación y SELECT con SSL Mode=Require y espera la aplicación manual del BAT; Google espera un login interactivo; Brevo fue aceptado por API, sin comprobar el buzón; Docker se prepara por código y publicación, pero no se ejecuta porque no está instalado en el equipo. No se considera que estas operaciones externas hayan pasado una prueba completa.

Actualización de conexión: se retiró VerifyFull y su archivo CA para seguir SSL Mode=Require de la referencia de e-commerce. Prueba remota de autenticación y SELECT correcta; no se ejecutó DDL ni DML remoto.

## Corrección de arranque local

Se añadió la referencia Server → Client, el perfil compartido y local de Visual Studio y los BAT iniciar/detener. La interfaz y la API se verificaron en https://localhost:7043 con un único servidor. HTTPS se comprobó con la validación normal de certificados del equipo. La publicación del servidor incorpora index.html y el runtime del cliente sin copiar carpetas a mano; no contiene el JSON privado ni las claves recibidas.

El actualizador ya usa Npgsql y la conexión privada del servidor, sin token CLI. El modo comprobar confirmó autenticación y conexión, y detectó que las tablas del cine todavía no estaban creadas. El modo actualizar queda reservado a la ejecución expresa del propietario; no se probó una reconstrucción remota sin su autorización. El arranque bloquea el acceso con una indicación concreta si falta preparar el esquema.


## Revisión de rendimiento y flujos — 20 de octubre de 2026

Las pruebas de esta revisión usan la base real de Supabase reconstruida por el propietario. Se verificaron caché, invalidación tras CRUD, separación de cuentas, paginado de ocho películas, ocupación en vivo, acceso persistente y el cierre de la carga después de los datos de la ruta. Se probaron una compra con cuenta QA y el pago/entrega de un pedido de confitería. Los registros QA quedan como operaciones de prueba hasta la próxima reconstrucción manual.

El PDF nuevo es vectorial: una página, texto seleccionable y QR decodificable desde su render. Las plantillas de correo se comprobaron con transporte HTTP simulado, sin enviar correos adicionales. Se comprobaron destinos de desarrollo y producción.

El SQL consolidado nuevo está preparado para ejecución manual. Su sintaxis PostgreSQL, claves literales y 30 planes de INSERT/UPDATE se comprobaron; los planes se solicitaron a Supabase en una transacción READ ONLY, sin ejecutar los INSERT, UPDATE ni DROP. La reconstrucción del seed nuevo sigue reservada al propietario. Los conteos y fechas nuevos se reflejarán al ejecutar update_DB.bat.

Reportes en `docs/validacion`: performance-before/after, refinement-unit, refinement-ui, refinement-booking, refinement-integration, seed-refinement y vector-pdf.


Cierre de esta revisión: 129 comprobaciones, incluidas 44 visitas de rutas; Debug y publicación Release sin errores ni advertencias. Los arneses temporales se retiraron; se conservaron los reportes. El Git inicial se eliminó a petición del propietario.
# Despliegue y PWA (21/10/2026)

Publicación Release de Server y Client, ejecución del script de Netlify y 59 comprobaciones correctas en `validacion/deployment-pwa-report.json`. Se verificaron 243 recursos por publicación, integridad del shell, exclusión de secretos, iconos 192/512, rutas de Docker, rechazo de URLs inválidas, reglas del proxy, API Production con Supabase, cookies OAuth y callback canónico de Netlify sin puerto HTTPS redundante.

En Edge real: worker activado, 158 recursos de interfaz en caché, navegación del shell sin conexión, API excluida del modo offline, recarga y vista móvil de 390 px sin desbordamiento. Las capturas están en `validacion/pwa-desktop.png` y `validacion/pwa-mobile.png`. Los programas temporales de pruebas fueron retirados y el servidor temporal se detuvo.

Docker no está instalado en este equipo: no se ejecutó una construcción local del contenedor. El workflow de GitHub construye la imagen y publica el cliente con configuración ficticia de CI. Netlify/Render públicos y consentimiento Google interactivo se validarán cuando existan sus URLs reales. Pasos y variables en [DESPLIEGUE](DESPLIEGUE.md).

## Separación de producción: Render solo backend (21/10/2026)

Esta revisión sustituye el alojamiento conjunto de producción descrito arriba: Release y Docker excluyen Client; Debug conserva el arranque local integrado. En Production no se sirven archivos estáticos del frontend ni el fallback SPA. La compilación de Netlify sigue publicando Client por separado.

28 comprobaciones correctas en `validacion/backend-only-report.json`: publicación sin frontend ni credenciales, rutas de interfaz con 404 JSON, API y Supabase disponibles, autorización, CORS, callback Google `https://cinespacebo.netlify.app/signin-google` y retirada del antiguo worker de Render sin afectar otros cachés. El frontend se volvió a publicar usando `https://cinespaceproject.onrender.com` como destino del proxy. La propiedad Debug mantiene la referencia a Client. El programa temporal de validación y su servidor se retiraron; no se ejecutó ninguna reconstrucción de base.
