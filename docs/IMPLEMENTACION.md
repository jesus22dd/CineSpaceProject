# Implementación

Se conserva el flujo de `VersionFinal` en Client, Server y Shared. La API aplica los permisos aunque se omita o altere la interfaz. Los controladores delegan casos de uso a servicios inyectados; el repositorio común centraliza el conteo y la paginación, y EF mantiene las relaciones y consultas traducibles a SQL.

## Módulos

- Cuentas: Identity, perfiles, roles, cookies HttpOnly seguras, confirmación, recuperación y cambio de contraseña. Google valida el correo del proveedor, crea clientes sin contraseña local y no vincula automáticamente un correo a una cuenta existente.
- Usuarios y trabajadores: búsqueda, orden reciente/antiguo, paginación y CRUD. Protección de la propia cuenta administrativa y de las cuentas con historial.
- Catálogo: películas manuales/TMDB, idiomas, géneros, fecha de estreno, distribuidora, imágenes, tráiler, banners y próximos estrenos. Búsqueda TMDB paginada desde la API.
- Salas: tipos, experiencias, precios/ofertas, planos irregulares y editor de butacas. Las funciones conservan la distribución y experiencia de las salas con ventas.
- Programación: duración, limpieza, rangos de fechas, horarios, conflictos y disponibilidad. Solo se ofrecen funciones futuras y los tres primeros días disponibles, sin calendario libre de reserva.
- Reserva: ficha y función → ticket general con información de sala/experiencia → butacas FIFO → datos y pago → QR y comprobante. Clientes: débito o QR; personal: también efectivo. Tres mensajes de pago, sin barras ni texto secundario, con pausas de 1,5 segundos. La API fija identidad, precio, disponibilidad y factura; las escrituras son transaccionales e idempotentes.
- Cuenta del cliente: compras y reseñas de seis en seis, orden desde el servidor y comentarios con altura fija hasta pulsar Ver más. Las reseñas requieren una compra de una función pasada.
- Confitería: categorías, combos, cantidades, carrito, cotización vigente, pedido privado de invitado o cliente, QR y pago al retirar. Un cambio de precio exige revisar el nuevo total. Solo el personal registra pago/entrega; pedidos retirados o cancelados no pueden reutilizarse.
- Verificación: códigos, carga de imagen QR y cámara tanto para pedidos como para boletos. El ingreso de un boleto queda registrado una sola vez y se muestra su estado.
- Administración: filtros, páginas, estadísticas y gráficos calculados en SQL; exportación de reseñas XLSX. La sección de respaldo/importación del prototipo fue retirada.

## Datos y archivos

El único archivo de `supabase/migrations` contiene `BEGIN`, el DROP del esquema `cine`, estructura/índices/restricciones, datos de prueba y reparación de secuencias, seguido de `COMMIT`. No elimina los objetos internos de Supabase. La reserva combina bloqueo de sala, claves únicas y restricciones de butacas para impedir ventas duplicadas y horarios solapados.

Los 76 recursos iniciales están en WebP dentro de `Client/wwwroot/images`; el QR de pago también es local. Toda carga nueva o imagen importada de TMDB se valida y recodifica en servidor como WebP antes de guardarla en `cinespace-media`, con nombre aleatorio, dimensiones acotadas y caché. Los bytes no se guardan en PostgreSQL. La descarga TMDB se limita al proveedor autorizado.

La base se reconstruye exclusivamente con el BAT del propietario y el único SQL consolidado. La aplicación no ofrece importación de bases ni aplica migraciones al arrancar.

## Seguridad y operación

Antiforgery en escrituras, autorización por rol, CORS explícito, límite de intentos, bloqueo temporal de acceso, tokens de cuenta de dos horas y claves de protección persistentes en PostgreSQL. Errores internos no se muestran al cliente. La API no se almacena en caché. Contraseñas de prueba: PBKDF2 de Identity con sal individual.

El BAT pertenece al propietario: la aplicación no aplica DDL ni reconstruye datos. La configuración privada no forma parte del cliente, del repositorio ni de la imagen Docker. El pago sigue siendo una simulación, sin procesador bancario ni almacenamiento de números de tarjeta.
