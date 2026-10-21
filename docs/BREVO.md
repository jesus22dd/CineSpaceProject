# Correo de cuentas con Brevo

La integración usa HTTPS `POST /v3/smtp/email`, con los campos privados `BrevoSettings:ApiKey`, `FromEmail` y `FromName`. No requiere host, puerto o contraseña SMTP. El remitente configurado es el del propietario y el nombre visible es **CineSpace**.

Permisos de la clave: acceso restringido, **Emails transaccionales → Todos**; los recursos ajenos al envío permanecen en Ninguno. [Claves API de Brevo](https://help.brevo.com/hc/en-us/articles/209467485-Create-and-manage-your-API-keys).

Registro crea una cuenta Cliente pendiente y envía la confirmación. Los enlaces de Identity duran dos horas. La recuperación no revela si existe el correo; el token de restablecimiento deja de ser válido al cambiar la contraseña. Las cuentas Google usan la identidad verificada del proveedor, sin contraseña local generada.

Se comprobó un envío real de confirmación aceptado por Brevo y se probaron localmente confirmación, acceso posterior y restablecimiento de contraseña. No se leyó la bandeja de entrada del propietario, por lo que la entrega final sigue pendiente de su comprobación.

En producción configura las tres variables `BrevoSettings__ApiKey`, `BrevoSettings__FromEmail`, `BrevoSettings__FromName` y `Frontend__Url` con el origen HTTPS real para que los enlaces lleven al sitio publicado. Si cambias el remitente, verifícalo antes en Brevo. [Remitentes](https://help.brevo.com/hc/en-us/articles/208836149-Create-a-new-sender-From-name-and-From-email).
