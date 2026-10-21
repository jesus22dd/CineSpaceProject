namespace CineSpace.Shared.Models;

// Códigos originales conservados para leer ventas y respaldos anteriores.
// Los nuevos tipos reciben identificadores del catálogo de salas.
public static class TipoSala
{
    public const int Estandar = 0, Premium = 1, IMAX = 2, XD = 3;
    public static string Codigo(int id) => id switch { Estandar => "Estandar", Premium => "Premium", IMAX => "IMAX", XD => "XD", _ => id.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    public static int Leer(string valor) => valor.Trim() switch { "Estandar" or "Estándar" => Estandar, "Premium" => Premium, "IMAX" => IMAX, "XD" => XD, var codigo => int.Parse(codigo, System.Globalization.CultureInfo.InvariantCulture) };
    public static string NombreOriginal(int id) => id == Estandar ? "Estándar" : id is >= Premium and <= XD ? Codigo(id) : $"Tipo de sala {id}";
    public static List<TipoSalaCine> Originales() => new()
    {
        new() { Id = Estandar, Nombre = "Estándar", Descripcion = "Sala de cine convencional." },
        new() { Id = Premium, Nombre = "Premium", Descripcion = "Sala con servicios y comodidad premium." },
        new() { Id = IMAX, Nombre = "IMAX", Descripcion = "Sala con pantalla y proyección de gran formato IMAX." },
        new() { Id = XD, Nombre = "XD", Descripcion = "Sala con pantalla de gran formato y sonido envolvente." }
    };
}
