namespace CineSpace.Shared.Models;

public sealed class SeleccionButacas
{
    private readonly List<string> orden = new();
    public IReadOnlyList<string> Orden => orden.AsReadOnly();
    public HashSet<string> CopiarAsientos() => new(orden, StringComparer.Ordinal);

    // Selection order is explicit: HashSet enumeration cannot represent a queue after removals.
    public string? Alternar(string asiento, int limite)
    {
        if (limite <= 0 || string.IsNullOrWhiteSpace(asiento)) return null;
        if (orden.Remove(asiento)) return null;
        string? retirada = null;
        while (orden.Count >= limite) { retirada ??= orden[0]; orden.RemoveAt(0); }
        orden.Add(asiento);
        return retirada;
    }
    public void Limpiar() => orden.Clear();
    public void Excluir(IEnumerable<string> asientos)
    {
        var excluidos = asientos.ToHashSet(StringComparer.Ordinal);
        orden.RemoveAll(excluidos.Contains);
    }
}
