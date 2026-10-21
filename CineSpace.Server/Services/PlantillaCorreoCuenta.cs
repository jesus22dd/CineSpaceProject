using System.Globalization;
using System.Net;

namespace CineSpace.Server.Services;

public sealed record ContenidoCorreoCuenta(string Asunto, string Html, string Texto);

public static class PlantillaCorreoCuenta
{
    public static ContenidoCorreoCuenta Confirmacion(string nombre, string url, TimeSpan vigencia) => Crear(
        nombre, url, vigencia,
        "Confirma tu cuenta de CineSpace",
        "Bienvenido a CineSpace",
        "Confirma tu correo electrónico",
        "Estás a un paso de tu próxima función. Confirma tu correo para activar tu cuenta y empezar a reservar.",
        "Confirmar mi cuenta",
        "Si no creaste una cuenta en CineSpace, puedes ignorar este mensaje.");

    public static ContenidoCorreoCuenta Recuperacion(string nombre, string url, TimeSpan vigencia) => Crear(
        nombre, url, vigencia,
        "Restablece tu contraseña de CineSpace",
        "Seguridad de tu cuenta",
        "Restablece tu contraseña",
        "Recibimos una solicitud para restablecer la contraseña de tu cuenta. Usa el botón para elegir una nueva.",
        "Crear nueva contraseña",
        "Si no solicitaste este cambio, ignora este mensaje. Tu contraseña seguirá siendo la misma.");

    private static ContenidoCorreoCuenta Crear(string nombre, string url, TimeSpan vigencia,
        string asunto, string categoria, string titulo, string mensaje, string accion, string seguridad)
    {
        static string H(string valor) => WebUtility.HtmlEncode(valor);
        var saludo = string.IsNullOrWhiteSpace(nombre) ? "Hola." : $"Hola, {nombre.Trim()}.";
        var minutos = Math.Max(1, (int)vigencia.TotalMinutes);
        var duracion = minutos % 60 == 0
            ? $"{minutos / 60} {(minutos == 60 ? "hora" : "horas")}"
            : $"{minutos.ToString(CultureInfo.InvariantCulture)} {(minutos == 1 ? "minuto" : "minutos")}";
        var validez = $"Este enlace es personal y estará disponible durante {duracion}. Si caduca, solicita uno nuevo desde CineSpace.";
        var texto = $"CineSpace\n{titulo}\n\n{saludo}\n\n{mensaje}\n\n{accion}:\n{url}\n\n{validez}\n\n{seguridad}\n\nEquipo CineSpace";
        var html = $$"""
            <!doctype html>
            <html lang="es">
            <head>
              <meta charset="utf-8">
              <meta name="viewport" content="width=device-width, initial-scale=1">
              <meta name="color-scheme" content="light">
              <meta name="supported-color-schemes" content="light">
              <title>{{H(asunto)}}</title>
              <style>
                :root { color-scheme:light; supported-color-schemes:light; }
                body { margin:0 !important; padding:0 !important; }
                table { border-collapse:collapse; font-family:Arial,Helvetica,sans-serif; }
                a { text-decoration:underline; }
                @media only screen and (max-width:600px) {
                  .mail-outer { padding:20px 12px !important; }
                  .mail-body { padding:28px 24px !important; }
                  .mail-title { font-size:25px !important; }
                }
              </style>
            </head>
            <body bgcolor="#f3f4f6" style="margin:0;padding:0;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;color:#20232b;-webkit-text-size-adjust:100%;">
              <div style="display:none;font-size:1px;color:#f3f4f6;line-height:1px;max-height:0;max-width:0;opacity:0;overflow:hidden;mso-hide:all;">{{H(titulo)}}. Tu enlace estará disponible durante {{H(duracion)}}.</div>
              <table role="presentation" width="100%" bgcolor="#f3f4f6" style="width:100%;background-color:#f3f4f6;font-family:Arial,Helvetica,sans-serif;">
                <tr><td class="mail-outer" align="center" style="padding:40px 16px;">
                  <!--[if mso]><table role="presentation" width="560" align="center"><tr><td><![endif]-->
                  <table role="presentation" width="100%" bgcolor="#ffffff" style="width:100%;max-width:560px;background-color:#ffffff;border:1px solid #e2e5e9;border-radius:12px;">
                    <tr><td style="padding:24px 32px;border-bottom:1px solid #eceef1;">
                      <p style="margin:0;color:#72273f;font-size:24px;line-height:30px;font-weight:700;letter-spacing:-.6px;">CineSpace</p>
                    </td></tr>
                    <tr><td class="mail-body" style="padding:32px;color:#20232b;">
                      <p style="margin:0 0 12px;color:#72273f;font-size:12px;line-height:18px;font-weight:700;">{{H(categoria)}}</p>
                      <h1 class="mail-title" style="margin:0 0 24px;color:#20232b;font-size:28px;line-height:1.25;font-weight:700;letter-spacing:-.5px;">{{H(titulo)}}</h1>
                      <p style="margin:0 0 12px;color:#20232b;font-size:15px;line-height:24px;">{{H(saludo)}}</p>
                      <p style="margin:0 0 28px;color:#4b5260;font-size:15px;line-height:24px;">{{H(mensaje)}}</p>
                      <table role="presentation" style="margin:0 0 24px;"><tr><td align="center" bgcolor="#922f4d" style="background-color:#922f4d;border-radius:6px;mso-padding-alt:14px 24px;">
                        <a href="{{H(url)}}" style="display:inline-block;padding:14px 24px;border:1px solid #922f4d;border-radius:6px;font-family:Arial,Helvetica,sans-serif;font-size:15px;line-height:20px;font-weight:700;text-decoration:none;background-color:#922f4d;color:#ffffff;">{{H(accion)}}</a>
                      </td></tr></table>
                      <p style="margin:0 0 12px;color:#626a78;font-size:13px;line-height:21px;">{{H(validez)}}</p>
                      <p style="margin:0 0 28px;color:#626a78;font-size:13px;line-height:21px;">¿El botón no abre? <a href="{{H(url)}}" style="color:#72273f;text-decoration:underline;font-weight:600;">{{H(accion)}}</a>.</p>
                      <table role="presentation" width="100%" style="width:100%;border-top:1px solid #eceef1;"><tr><td style="padding-top:20px;">
                        <p style="margin:0;color:#626a78;font-size:13px;line-height:21px;">{{H(seguridad)}}</p>
                      </td></tr></table>
                    </td></tr>
                  </table>
                  <!--[if mso]></td></tr></table><![endif]-->
                  <p style="margin:20px 0 0;color:#697180;font-family:Arial,Helvetica,sans-serif;font-size:12px;line-height:20px;">Equipo CineSpace<br>Este es un mensaje automático relacionado con tu cuenta.</p>
                </td></tr>
              </table>
            </body>
            </html>
            """;
        return new(asunto, html, texto);
    }
}
