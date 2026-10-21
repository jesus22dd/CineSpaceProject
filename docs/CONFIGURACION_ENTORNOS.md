# Configuracion por entorno

Se conserva el enfoque de la referencia de e-commerce: configuracion privada fuera de Git y del despliegue, cookies HttpOnly y origenes explicitos. El cine tiene tres proyectos. El usuario amplio el alcance para incluir confirmacion de cuenta y recuperacion de contrasena por correo con Brevo HTTPS.

## Archivos

| Archivo | Uso | Publicable |
| --- | --- | --- |
| Server/appsettings.json | Estructura comun; valores privados vacios | Si |
| Server/appsettings.Development.json | Contrasena PostgreSQL, claves Supabase y Google reales | No: excluido de Git, Docker, salida y publicacion |
| Server/appsettings.Development.example.json | Plantilla sin claves para otro equipo | Si, como codigo fuente; excluido de publicacion |
| Client/wwwroot/appsettings.json | Solo URL publica de API; vacia utiliza el origen actual | Si, visible en el navegador |
| Client/wwwroot/appsettings.Development.json | URLs locales de API | Si, contiene solo datos publicos |
| supabase/.env.cli.local | Configuración CLI anterior, opcional; el BAT ahora usa la conexión del servidor | No |

Las rutas Server/ y Client/ de la tabla corresponden a CineSpace.Server/ y CineSpace.Client/. El antiguo appsettings.Local.json fue migrado y retirado. No se carga mediante Program.cs.

En desarrollo ASP.NET carga: appsettings.json, appsettings.Development.json, User Secrets, variables de entorno y argumentos. Los ultimos tienen prioridad. En Production no se carga el JSON Development. [Configuracion ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-8.0).

Para otro equipo, copia el ejemplo a appsettings.Development.json y completa los valores privados localmente. No copies credenciales a Client ni a Shared.

## Variables del servidor en Render

Render y Docker Compose ya tienen preparados los siguientes campos; se proporcionan al ejecutar la aplicacion, nunca como argumentos de compilacion:

| Variable de Render | Clave del JSON | Valor al desplegar |
| --- | --- | --- |
| ConnectionStrings__CineSpace | ConnectionStrings:CineSpace | Cadena Npgsql real con password y configuracion TLS |
| Supabase__ProjectRef | Supabase:ProjectRef | ID del proyecto del cine |
| Supabase__Url | Supabase:Url | URL HTTPS del proyecto |
| Supabase__SecretKey | Supabase:SecretKey | service_role recibida o Secret key compatible |
| Supabase__Bucket | Supabase:Bucket | cinespace-media |
| Authentication__Google__ClientId | Authentication:Google:ClientId | ID del cliente Google del cine |
| Authentication__Google__ClientSecret | Authentication:Google:ClientSecret | Secreto OAuth |
| Tmdb__ApiKey | Tmdb:ApiKey | Clave existente del prototipo, guardada en el servidor |
| BrevoSettings__ApiKey | BrevoSettings:ApiKey | Clave API propia de CineSpace, recibida y guardada en Development |
| BrevoSettings__FromEmail | BrevoSettings:FromEmail | Direccion de remitente verificada en Brevo |
| BrevoSettings__FromName | BrevoSettings:FromName | CineSpace |
| Frontend__Url | Frontend:Url | Origen HTTPS publico del cine, sin rutas |
| Backend__PublicUrl | Backend:PublicUrl | Origen HTTPS publico del servicio Render |
| Deployment__BehindProxy | Deployment:BehindProxy | true solo detras del proxy de Render |

ASPNETCORE_ENVIRONMENT=Production y ASPNETCORE_HTTP_PORTS=8080 ya estan declarados. AllowedHosts puede restringirse al dominio real cuando exista. Si se necesitan mas origenes, se anaden como Frontend__Origins__0, Frontend__Origins__1, etc.; no se acepta un comodin con credenciales.

La clave anon se conserva en Development porque fue recibida, pero el flujo preparado no usa Supabase desde el navegador y no necesita inyectarla en produccion. La service_role permanece exclusivamente en el servidor.

En Compose, .env.example documenta los nombres equivalentes del archivo .env privado. No se genera un archivo .env con secretos automaticamente.

## Frontend y backend

Client deja Api:BaseUrl vacío y utiliza rutas relativas. En producción se publica como PWA en Netlify usando `netlify.toml` y `scripts/build-netlify.sh`; Render publica únicamente la API. En Netlify, `RENDER_API_ORIGIN` genera las reglas de proxy de `/api`, `/signin-google` y `/healthz` durante la compilación. `Frontend__Url` es el dominio de Netlify y `Backend__PublicUrl` el de Render. Solo el entorno local Debug sirve ambos desde el servidor.

Las cookies HttpOnly/Secure y el retorno OAuth usan el origen canónico de `Frontend__Url`. El navegador conserva el dominio del frontend durante todo el flujo. Las variables privadas del servidor nunca se incorporan al cliente WebAssembly. Los pasos completos y la tabla para los paneles están en [DESPLIEGUE](DESPLIEGUE.md).

En Google se agregara exactamente https://DOMINIO_PUBLICO/signin-google. El callback local configurado es https://localhost:7043/signin-google. Los endpoints de identidad y el callback de negocio están implementados; falta probar un acceso Google interactivo.

## Bucket

En Supabase > Storage > New bucket:

1. Nombre: cinespace-media.
2. Activar Public bucket para las imagenes publicas del cine.
3. Limite recomendado: 5 MB por archivo almacenado.
4. MIME permitido: image/webp.

La API valida permisos de administrador/trabajador y convierte las imágenes a WebP antes de subirlas. No se deben crear politicas de escritura anonima para resolver las cargas. El servicio está integrado con los formularios del cine y se verificó una subida/lectura/retirada real.

## Pendientes reales

- Bucket cinespace-media creado y publico, confirmado por API. Falta restringirlo a 5 MB y MIME image/webp si se mantienen los limites recomendados; actualmente no tiene limites propios configurados.
- Ejecutar update_DB.bat como propietario para crear/reconstruir el esquema cine. No requiere token de CLI.
- Brevo aceptó el envío de confirmación al propietario; falta comprobar el buzón.
- El único SQL ya contiene estructura y datos; debe aplicarlo el propietario mediante el BAT.
- Completar la prueba interactiva de Google con una cuenta autorizada; módulos, Identity, carga WebP y claves de sesión persistentes ya están implementados.
- Obtener la URL publica del despliegue y registrar su callback Google.

La conexión PostgreSQL sigue la referencia de e-commerce: SSL Mode=Require, cifrada y sin descarga manual de CA. La autenticación y SELECT remotos se comprobaron correctamente. Require no verifica el certificado del servidor.

En desarrollo, Frontend:Url y Backend:PublicUrl usan https://localhost:7043: CineSpace.Server aloja también el cliente. Los puertos 7143/5143 quedan solo para un cliente separado opcional, sin necesidad de utilizarlo para arrancar el sistema.
