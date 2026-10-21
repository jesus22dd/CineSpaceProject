# Estado de configuración — 20/10/2026

| Servicio | Comprobado | Pendiente externo |
| --- | --- | --- |
| PostgreSQL Supabase | Base actual operativa. Inicio de sesión, filtros, compra, confitería y analítica comprobados contra Supabase. | El propietario ejecuta el BAT para cargar la revisión nueva de datos relativos. |
| Supabase Storage | Bucket cinespace-media público. Ocho imágenes nuevas WebP de catálogo subidas y verificadas. Las cargas y ediciones de la API convierten a WebP. | Ninguna credencial adicional para desarrollo. |
| TMDB | Búsqueda, fichas e imágenes de las películas solicitadas comprobadas. Clave exclusivamente en el servidor. | Ninguna. |
| Brevo | API y remitente configurados. Plantilla actual comprobada con HTTP simulado, enlaces de desarrollo/producción y sin formulario público de reenvío. La API aceptó un envío real autorizado en la validación anterior. | Verificar recepción real en el buzón; una respuesta de Brevo no garantiza entrega. |
| Google | Cliente/secret privados, challenge HTTPS, callback y persistencia implementados. | En producción, agregar el callback con la URL pública final. |
| Actualizador manual | Conexión PostgreSQL privada directa; no requiere token CLI. DROP limitado al esquema cine. Arranque y comprobación no aplican SQL. | El propietario aplica el SQL nuevo mediante update_DB.bat. |
| Render | Publicación Release de Server con el frontend, Dockerfile y variables preparados para un mismo origen. | Repositorio y URL públicos. Docker no está instalado en este equipo. |

La sesión usa una cookie HttpOnly/Secure persistente de 30 días, sin token en localStorage ni procesos de sesión mantenidos en segundo plano. Las claves permanecen en el JSON Development privado, excluido de Git, Docker y publicación.

El seed nuevo tiene sintaxis PostgreSQL válida, relaciones y butacas comprobadas y 30 planes revisados en Supabase en una transacción READ ONLY. No se ejecutó su reconstrucción remota: queda reservada al propietario. Las operaciones normales de QA guardadas durante las pruebas se eliminarán cuando el propietario reconstruya el esquema.

El acceso PostgreSQL conserva SSL Mode=Require, como la referencia e-commerce. No se añadió un requisito de certificado CA manual.

Inicio: iniciar_CineSpace.bat o CineSpace.Server / https en Visual Studio; interfaz y API en https://localhost:7043. Se retiró la sección de respaldo/importación del prototipo y se conserva la exportación de reseñas XLSX.
