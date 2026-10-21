using QRCoder;
using System;
using System.IO;
using CineSpace.Shared.Models;
using Microsoft.JSInterop;

namespace CineSpace.Client.Services
{
    public class QRService
    {
        private readonly IJSRuntime _jsRuntime;

        public QRService(IJSRuntime jsRuntime)
        {
            _jsRuntime = jsRuntime;
        }

        public string GenerarQRDesdeTicket(VentaTicket ticket)
        {
            // Un identificador compacto mantiene el QR legible tambien en la esquina del PDF.
            // La verificacion siempre obtiene los detalles y el estado desde la API.
            var datos = $"CINE SPACE\nCODIGO DE VERIFICACION\n{ticket.CodigoValidacion}\nFUNCION: {ticket.FuncionId}";
            using var generador = new QRCodeGenerator();
            using var datosQR = generador.CreateQrCode(datos, QRCodeGenerator.ECCLevel.Q, forceUtf8: true, utf8BOM: false, eciMode: QRCodeGenerator.EciMode.Utf8);
            using var qr = new PngByteQRCode(datosQR);
            return Convert.ToBase64String(qr.GetGraphic(10));
        }
        // Descargar QR como imagen
        public string GenerarQRRetiro(string enlace)
        {
            using var generator = new QRCodeGenerator();
            using var data = generator.CreateQrCode(enlace, QRCodeGenerator.ECCLevel.Q);
            using var qr = new PngByteQRCode(data);
            return Convert.ToBase64String(qr.GetGraphic(8));
        }

        public async Task DescargarQRAsync(string qrBase64, string nombreArchivo)
        {
            await _jsRuntime.InvokeVoidAsync("downloadImage", ObtenerUrlImagenQR(qrBase64), nombreArchivo);
        }

        // Obtener QR como URL de imagen
        public string ObtenerUrlImagenQR(string qrBase64)
        {
            return $"data:image/png;base64,{qrBase64}";
        }
    }
}
