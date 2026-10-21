# Estrenos y venta anticipada

En **Próximos estrenos**, el administrador vincula el anuncio con una película del catálogo y define su fecha de estreno. El estado **A estrenar** permite anunciarla sin vender entradas.

Desde **Programar funciones**, se elige sala, experiencia, fechas y horarios desde el estreno en adelante. Preparar funciones no abre la venta. Después, en el anuncio se selecciona **Preventa disponible** y se guarda. La apertura requiere un anuncio publicado, una película publicada y funciones futuras en una sala activa.

El público puede reservar desde el primer día programado, aunque falten varias semanas: elige función, entradas y butacas, finaliza el pago simulado y recibe su QR/PDF. El servidor valida ocupación, precios y disponibilidad nuevamente al comprar. Cerrar la preventa retira la oferta pública y bloquea nuevas compras; conserva las entradas existentes. La fecha de estreno queda protegida cuando ya hay ventas. El día del estreno la película pasa a cartelera automáticamente.

Los pósteres aceptan rutas locales, TMDB y URLs completas del bucket. Una imagen nueva o reemplazada sigue el servicio de conversión a WebP y carga a Storage; no se antepone TMDB a una URL de Storage.

El SQL único incluye siete preventas vinculadas con funciones, más un anuncio sin venta. Sus fechas se calculan desde el día en que se ejecuta el BAT. El propietario aplica `update_DB.bat`; la aplicación no ejecuta reconstrucciones al arrancar. Este BAT elimina los datos anteriores del esquema `cine`, como está definido en el flujo de reconstrucción.

Las pruebas de esta revisión se ejecutaron únicamente en PostgreSQL aislado. Informes: [preventas](validacion/presales-report.json), [ocupación y géneros](validacion/realistic-seed-report.json) y [pantallas móviles](validacion/mobile-after-report.json).
