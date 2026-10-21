using System.Security.Cryptography;
using System.Text.Json;
using CineSpace.Shared.Models;

namespace CineSpace.Server.Services;

public static class ReglasCine
{
    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web);
    public static readonly TimeZoneInfo Zona = TimeZoneInfo.FindSystemTimeZoneById("America/La_Paz");
    public static DateTime Local(DateTime utc) => DateTime.SpecifyKind(TimeZoneInfo.ConvertTimeFromUtc(DateTime.SpecifyKind(utc, DateTimeKind.Utc), Zona), DateTimeKind.Unspecified);
    public static DateTime Utc(DateTime local) => TimeZoneInfo.ConvertTimeToUtc(DateTime.SpecifyKind(local, DateTimeKind.Unspecified), Zona);
    public static DateTime Ahora => Local(DateTime.UtcNow);
    public static string Serializar<T>(T value) => JsonSerializer.Serialize(value, Json);
    public static T Leer<T>(string value) where T : new() => JsonSerializer.Deserialize<T>(value, Json) ?? new();
    public static string Codigo(int longitud = 8) => string.Concat(Enumerable.Range(0, longitud).Select(_ => "ABCDEFGHJKLMNPQRSTUVWXYZ23456789"[RandomNumberGenerator.GetInt32(32)]));
    public static string Texto(string? value, int maximo, string campo, bool requerido = true)
    {
        var text = value?.Trim() ?? "";
        if ((requerido && text.Length == 0) || text.Length > maximo) throw new ArgumentException($"Revisa {campo}; admite hasta {maximo} caracteres.");
        return text;
    }
    public static decimal Precio(decimal precio, int descuento)
    {
        if (precio is <= 0 or > 10000 || descuento is < 0 or > 90) throw new ArgumentException("Revisa el precio y el descuento de la sala.");
        return decimal.Round(precio * (100 - descuento) / 100m, 2, MidpointRounding.AwayFromZero);
    }
    public static string MetodoPago(string? metodo, bool personal)
    {
        if (metodo is "Tarjeta" or "Tarjeta de débito") return "Tarjeta de débito";
        if (metodo is "QR" or "Pago QR") return "Pago QR";
        if (personal && metodo == "Efectivo") return "Efectivo";
        throw new ArgumentException("Selecciona un metodo de pago permitido.");
    }
    public static void Butacas(IEnumerable<string> seleccion, PlanoSala plano, int cantidad)
    {
        var butacas = seleccion.ToArray();
        if ((cantidad < 1 || cantidad > plano.Asientos.Count) || butacas.Length != cantidad || butacas.Distinct(StringComparer.Ordinal).Count() != cantidad || butacas.Any(x => !plano.Asientos.Contains(x)))
            throw new ArgumentException("La cantidad de tickets debe coincidir con butacas validas y diferentes.");
    }
}
