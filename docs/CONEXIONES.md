# Conexión y actualización manual

La aplicación usa Npgsql Session pooler, con SSL Mode=Require, y ASP.NET Identity en el esquema cine. Storage es independiente y recibe las imágenes desde la API del cine.

## BAT del propietario

Haz doble clic en **update_DB.bat**. El programa compila el servidor y ejecuta el único `supabase/migrations/20261020173345_CineSpaceDB.sql` mediante la conexión privada de `CineSpace.Server/appsettings.Development.json`. No necesita Supabase CLI ni token personal adicional.

La actualización elimina y reconstruye exclusivamente el esquema cine con todos sus datos de prueba. Conserva auth y storage. Comprueba el host, usuario y Project Ref del cine antes de ejecutar SQL y evita dos actualizaciones simultáneas. Una transacción contiene estructura y datos: un error revierte la reconstrucción en lugar de dejar tablas parcialmente creadas.

La ventana muestra el resultado y los conteos de cuentas, películas, salas y funciones. En caso de error SQL indica el código de PostgreSQL y su posición, sin imprimir credenciales. El modo `update_DB.bat comprobar` solo consulta el estado; `enlazar` se conserva como alias de comprobación para los comandos anteriores. No se gestiona el historial de Supabase CLI: este flujo ejecuta siempre el SQL consolidado solicitado por el propietario.

El archivo privado supabase/.env.cli.local se conserva, pero ya no es requisito del BAT. No copies credenciales a Client ni a Shared.

## Iniciar el sistema

Después de preparar la base, ejecuta iniciar_CineSpace.bat o el perfil CineSpace completo de Visual Studio. Solo arranca CineSpace.Server: aloja también el frontend. URL local: https://localhost:7043. El lanzador verifica la base y no la modifica. Para cerrar su servidor: detener_CineSpace.bat.

## Google, correo e imágenes

Callback autorizado local: https://localhost:7043/signin-google. El frontend y el backend usan ahora el mismo origen local. Los usuarios de prueba de Google deben estar autorizados en su consola mientras la aplicación continúe en modo de prueba.

Brevo utiliza HTTPS para confirmación y recuperación. El bucket público cinespace-media permite lectura de imágenes; las escrituras usan la clave del servidor y se convierten a WebP. El despliegue usa variables de entorno privadas; véase CONFIGURACION_ENTORNOS.md.
