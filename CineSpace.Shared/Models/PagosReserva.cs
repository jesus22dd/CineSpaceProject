using System.Globalization;
using System.Text.RegularExpressions;

namespace CineSpace.Shared.Models;

public static class PagosReserva
{
    public static bool MetodoPermitido(string? metodo, bool permiteEfectivo) => metodo is "Tarjeta" or "QR" || metodo == "Efectivo" && permiteEfectivo;
    public static string Nombre(string metodo) => metodo switch { "Tarjeta" or "Tarjeta de débito" => "Tarjeta de débito", "QR" or "Pago QR" => "Pago QR", "Efectivo" => "Efectivo", _ => "Sin registrar" };

    public static string ValidarDebito(string titular, string numero, string vencimiento, string cvv, DateTime hoy)
    {
        if (titular.Trim().Length < 3) return "Escribe el nombre del titular de prueba.";
        if (numero.Replace(" ", "").Replace("-", "") != "4242424242424242") return "Usa el número de prueba 4242 4242 4242 4242.";
        if (!Regex.IsMatch(vencimiento, @"^(0[1-9]|1[0-2])/[0-9]{2}$")) return "Escribe el vencimiento en formato MM/AA.";
        var mes = int.Parse(vencimiento[..2], CultureInfo.InvariantCulture);
        var anio = 2000 + int.Parse(vencimiento[3..], CultureInfo.InvariantCulture);
        if (new DateTime(anio, mes, 1) < new DateTime(hoy.Year, hoy.Month, 1)) return "Usa una fecha de vencimiento actual o futura.";
        return cvv == "123" ? "" : "Usa el CVV de prueba 123.";
    }
}
