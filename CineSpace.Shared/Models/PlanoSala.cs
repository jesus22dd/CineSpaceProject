namespace CineSpace.Shared.Models;

public sealed class PlanoSala
{
    public int Numero { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public int Filas { get; set; } = 8;
    public int Columnas { get; set; } = 12;
    public HashSet<string> Asientos { get; set; } = new();
    public Dictionary<string, CategoriaAsiento> Categorias { get; set; } = new();
    public PlanoSala Copiar() => new() { Numero = Numero, Nombre = Nombre, Filas = Filas, Columnas = Columnas, Asientos = new(Asientos), Categorias = new(Categorias) };
    public static string Fila(int indice)
    {
        var nombre = string.Empty;
        for (var numero = indice + 1; numero > 0; numero = (numero - 1) / 26)
            nombre = (char)('A' + (numero - 1) % 26) + nombre;
        return nombre;
    }
    public static string Asiento(int fila, int columna) => $"{Fila(fila)}{columna + 1}";
    public static bool Posicion(string id, out int fila, out int columna)
    {
        fila = -1; columna = -1;
        if (string.IsNullOrEmpty(id)) return false;
        var corte = 0; var numeroFila = 0;
        while (corte < id.Length && id[corte] is >= 'A' and <= 'Z')
        {
            if (numeroFila > 1000) return false;
            numeroFila = numeroFila * 26 + id[corte++] - 'A' + 1;
        }
        if (corte == 0 || !int.TryParse(id[corte..], out var numeroColumna) || numeroColumna < 1) return false;
        fila = numeroFila - 1; columna = numeroColumna - 1; return true;
    }
    public bool Contiene(string id) => Posicion(id, out var fila, out var columna) && fila < Filas && columna < Columnas && id == Asiento(fila, columna);
    public void Validar()
    {
        if (string.IsNullOrWhiteSpace(Nombre) || Nombre.Trim().Length > 80) throw new ArgumentException("Escribe un nombre de hasta 80 caracteres para la sala.");
        if (Filas is < 1 or > 100 || Columnas is < 1 or > 100) throw new ArgumentException("La matriz admite de 1 a 100 filas y de 1 a 100 columnas.");
        if (Asientos.Count == 0) throw new ArgumentException("Marca al menos un asiento para guardar la sala.");
        if (Asientos.Any(id => !Contiene(id))) throw new ArgumentException("Hay asientos fuera de la matriz. Revisa el tamaño del plano.");
    }
}
