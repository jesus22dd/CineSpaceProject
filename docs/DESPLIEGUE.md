# Producción: Netlify + Render

El repositorio debe contener `Dockerfile`, `netlify.toml` y `CineSpace.sln` en su raíz (el contenido de CineSpaceFS). Netlify publica Client; Render ejecuta únicamente Server en Docker. Supabase conserva PostgreSQL, imágenes WebP y las claves persistentes de Identity. El frontend integrado está disponible solo en desarrollo local (Debug).

El navegador usa siempre el dominio de Netlify. Netlify envía `/api`, `/api/*`, `/signin-google` y `/healthz` a Render mediante reglas generadas al compilar. El cliente mantiene `Api:BaseUrl` vacío. No necesita claves de Supabase, Google, Brevo o TMDB.

## 1. Render

Crear **New → Web Service**, conectar el repositorio y seleccionar:

| Campo | Valor |
| --- | --- |
| Language / Runtime | Docker |
| Root Directory | Vacío: los archivos ya están en la raíz del repositorio |
| Dockerfile Path | `./Dockerfile` |
| Docker Build Context Directory | `.` |
| Docker Command | Vacío: usar ENTRYPOINT de la imagen |
| Health Check Path | `/healthz` |
| Region | Preferiblemente próxima a la región de Supabase |

También se puede crear un **Blueprint** usando `render.yaml`; los campos `sync: false` se completan en el panel. Elegir el plan en Render según el uso previsto.

Configurar las variables de la tabla siguiente. Obtener/copiar la URL asignada `https://TU-SERVICIO.onrender.com`; será `Backend__PublicUrl`. Si todavía no tienes el dominio de Netlify, puedes usar temporalmente esta misma URL en `Frontend__Url` para iniciar el servicio y cambiarla al terminar el paso 2.

| Variable exacta en Render | Valor |
| --- | --- |
| `ASPNETCORE_ENVIRONMENT` | `Production` |
| `ASPNETCORE_HTTP_PORTS` | `8080` |
| `PORT` | `8080` |
| `Deployment__BehindProxy` | `true` |
| `ConnectionStrings__CineSpace` | Copiar COMPLETA la cadena de `ConnectionStrings:CineSpace` del JSON Development privado del Server, sin comillas JSON |
| `Supabase__ProjectRef` | `beopqvzyjrqtcersuhcu` |
| `Supabase__Url` | `https://beopqvzyjrqtcersuhcu.supabase.co` |
| `Supabase__SecretKey` | La `service_role` / clave privada de Storage que está en Development |
| `Supabase__Bucket` | `cinespace-media` |
| `Authentication__Google__ClientId` | Copiar `Authentication:Google:ClientId` del JSON privado |
| `Authentication__Google__ClientSecret` | Copiar `Authentication:Google:ClientSecret` del JSON privado |
| `Tmdb__ApiKey` | Copiar `Tmdb:ApiKey` del JSON privado |
| `BrevoSettings__ApiKey` | Copiar `BrevoSettings:ApiKey` del JSON privado |
| `BrevoSettings__FromEmail` | `pazjesu122@gmail.com`, verificado como remitente en Brevo |
| `BrevoSettings__FromName` | `CineSpace` |
| `Frontend__Url` | `https://TU-SITIO.netlify.app` — sin rutas |
| `Backend__PublicUrl` | `https://TU-SERVICIO.onrender.com` — sin rutas |

Los nombres usan DOBLE guion bajo. Los valores privados se guardan solo en Environment del servicio; no en Git, Dockerfile, Build Args ni Netlify. No añadir comillas alrededor del valor. `Supabase__AnonKey` no se utiliza; `Supabase__ProjectRef` es información del proyecto, no una autorización. No se requiere un token personal de Supabase para ejecutar la app.

Al editar directamente el campo Value de `ConnectionStrings__CineSpace`, debe empezar por `Host=` y terminar en el último parámetro, sin comillas dobles exteriores. Las comillas de un JSON o de un archivo `.env` delimitan el texto; no forman parte del valor que recibe Npgsql. Si se pegan literalmente en el panel, el parser falla antes de conectar (por ejemplo, índice 182), y también falla Google porque sus claves de sesión se guardan en PostgreSQL.

La conexión ya configurada usa Session Pooler, puerto 5432, `SSL Mode=Require`, `Timeout=15` y `Command Timeout=30`. Copiarla conserva su contraseña sin errores con caracteres especiales. En Render el tamaño del pool puede limitarse con `Maximum Pool Size=10` dentro de la cadena si necesitas ajustar conexiones. El despliegue NO ejecuta migraciones ni DROP: `update_DB.bat` sigue siendo una operación manual del propietario.

## 2. Netlify

Crear **Add new project → Import an existing project**, conectar el mismo repositorio:

| Campo | Valor |
| --- | --- |
| Base directory | Vacío |
| Build command | `bash scripts/build-netlify.sh` |
| Publish directory | `.publish/netlify/wwwroot` |
| Functions directory | Vacío |

`netlify.toml` ya establece estos valores y las cabeceras. El script instala el SDK oficial indicado en `global.json` si no está disponible, publica Client en Release y genera `_redirects` con el origen de Render antes del fallback SPA. No modificar los archivos publicados de Blazor: la PWA verifica sus hashes. La transformación automática de archivos está desactivada en la configuración.

En **Environment variables** agregar únicamente:

| Variable | Valor | Scope/context |
| --- | --- | --- |
| `RENDER_API_ORIGIN` | `https://TU-SERVICIO.onrender.com` | Builds / Production |

No agregar `/api`, `/` con rutas adicionales ni claves privadas. El script rechaza URLs HTTP, credenciales y rutas. Cambiar esta variable requiere un nuevo deploy de Netlify porque genera las reglas durante la compilación. `NODE_VERSION=22` y las opciones públicas de .NET ya están en `netlify.toml`; no necesitas ingresarlas otra vez.

Evitar conectar previews a la base de producción para operar cuentas/compras. Esta configuración usa un único dominio canónico de producción; los previews necesitarían su propio entorno/callback. Cuando Netlify asigne la URL definitiva, actualizar `Frontend__Url` en Render con ella y volver a desplegar el servicio.

## 3. Google y Brevo

En **Google Auth Platform → Clients → cliente web de CineSpace**, conservar el callback local y agregar exactamente:

```text
https://TU-SITIO.netlify.app/signin-google
```

Si completas **Authorized JavaScript origins**, usar `https://TU-SITIO.netlify.app`, sin ruta. El login usa el flujo de servidor; el secreto permanece en Render. El callback de producción es el dominio de NETLIFY porque allí se establecen las cookies que usan las peticiones `/api`. No registrar solo el dominio de Render para este esquema. Los usuarios pueden utilizar Google mientras su cuenta esté autorizada por el estado de prueba/publicación del consentimiento.

En Brevo verificar que el remitente esté activo y que su API key permita enviar emails transaccionales. Los enlaces de confirmación y recuperación se construyen con `Frontend__Url` de Render, por lo que al cambiarlo dejarán de apuntar a localhost. No se requiere configurar SMTP adicional: el envío usa Brevo HTTPS.

## 4. PWA

La app usa `android-chrome-192x192.png` y `android-chrome-512x512.png` aportados en Client/wwwroot. Manifest en `/manifest.webmanifest`, nombre CineSpace, apertura independiente, tema del cine y acceso desde la pantalla de inicio. En Chrome/Edge se instala desde el menú del navegador; en iPhone desde Compartir → Añadir a pantalla de inicio.

El service worker de producción guarda el shell, WebAssembly, estilos, fuentes e iconos. No guarda respuestas de `/api`, OAuth, cuentas, compras, ocupación de butacas, imágenes remotas ni datos del usuario. El generador PDF continúa descargándose cuando se necesita. El service worker de desarrollo no almacena recursos. Comprar, iniciar sesión, verificar QR y consultar datos necesitan conexión. Una actualización espera a que se cierren las pestañas de la versión anterior para evitar interrumpir una compra; cerrar y volver a abrir la app la activa.

## 5. Comprobaciones después del despliegue

1. `https://TU-SERVICIO.onrender.com/healthz` responde `Healthy`.
2. `https://TU-SITIO.netlify.app/healthz` responde lo mismo pasando por el proxy.
3. `/api/sistema/preparacion` en Netlify indica conexión y esquema preparados; el catálogo devuelve datos.
4. Recargar `/confiteria` y `/login` directamente conserva las rutas de la app.
5. Iniciar sesión local, recargar, cerrar sesión y comprobar que no aparece una sesión anterior.
6. Entrar con Google y comprobar que vuelve al dominio de Netlify; registrar una cuenta de prueba y abrir su correo de confirmación.
7. Probar reserva/compra, carga WebP, verificación QR y descarga PDF en escritorio y móvil.
8. DevTools → Application: manifest válido, service worker activo e instalación disponible; cerrar las pestañas y reabrir tras una actualización.

Las comprobaciones públicas finales de Netlify/Render y el consentimiento interactivo de Google requieren los enlaces reales. La compilación local no sustituye esas comprobaciones.

## Separación de frontend y backend

La imagen Docker no compila ni contiene Client. En Production no hay archivos estáticos de Blazor ni fallback SPA: `/`, `/login` y `/_framework/*` responden 404. Los endpoints `/api/*`, `/healthz` y `/signin-google` siguen disponibles. Un worker de retirada en `/service-worker.js` elimina la caché y el registro de la PWA del despliegue antiguo en Render, sin afectar la instalación de Netlify. En local se mantiene el inicio con un clic mediante Debug.

## Fuentes

- [Docker en Render](https://render.com/docs/docker)
- [Blueprint de Render](https://render.com/docs/blueprint-spec)
- [Proxies de Netlify](https://docs.netlify.com/manage/routing/redirects/rewrites-proxies/)
- [Blazor PWA](https://learn.microsoft.com/en-us/aspnet/core/blazor/progressive-web-app?view=aspnetcore-8.0)
