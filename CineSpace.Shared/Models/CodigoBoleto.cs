using System.Text.RegularExpressions;

namespace CineSpace.Shared.Models;

public static class CodigoBoleto
{
    // Only the identifier is read from a QR. All ticket details come from the stored sale.
    public static string Extraer(string? valor)
    {
        if (string.IsNullOrWhiteSpace(valor) || valor.Length > 4096) return "";
        var texto = valor.Trim().ToUpperInvariant();
        if (Regex.IsMatch(texto, @"\A[A-Z0-9]{12}\z")) return texto;
        var simple = Regex.Match(texto, @"\A([A-Z]{3})[ -]?([0-9]{3})\z");
        if (simple.Success) return $"{simple.Groups[1].Value}-{simple.Groups[2].Value}";
        var qr = Regex.Match(texto, @"\ACINE SPACE\r?\nCODIGO DE VERIFICACION\r?\n([A-Z0-9]{12}|[A-Z]{3}-[0-9]{3})(?:\r?\n|\z)");
        return qr.Success ? qr.Groups[1].Value : "";
    }
}
