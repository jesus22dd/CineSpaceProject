namespace CineSpace.Shared.Models;

// Identificadores originales conservados para los respaldos. El catálogo admite experiencias nuevas.
public static class TipoExperiencia
{
    public const int D2 = 0, D3 = 1, D4X = 2, XD3D = 3;
    public static string Codigo(int id) => id switch { D2 => "D2", D3 => "D3", D4X => "D4X", XD3D => "XD3D", _ => id.ToString(System.Globalization.CultureInfo.InvariantCulture) };
    public static int Leer(string valor) => valor switch { "D2" => D2, "D3" => D3, "D4X" => D4X, "XD3D" => XD3D, _ => int.Parse(valor, System.Globalization.CultureInfo.InvariantCulture) };
}
