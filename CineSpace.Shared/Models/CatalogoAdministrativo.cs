namespace CineSpace.Shared.Models;

public sealed class EtiquetaCatalogo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public bool Sistema { get; set; }
    public EtiquetaCatalogo Copiar() => new() { Id = Id, Nombre = Nombre, Sistema = Sistema };
}

public sealed class ComboConfiteria
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public int CategoriaId { get; set; }
    public string Foto { get; set; } = "";
    public bool Publicado { get; set; } = true;
    public decimal Precio { get; set; }
    public ComboConfiteria Copiar() => new() { Id = Id, Nombre = Nombre, Descripcion = Descripcion, CategoriaId = CategoriaId, Foto = Foto, Publicado = Publicado, Precio = Precio };
}

public sealed class EstadoCatalogos
{
    public List<Distribuidora> Distribuidoras { get; set; } = new();
    public int UltimaDistribuidora { get; set; }
    public List<EtiquetaCatalogo> Categorias { get; set; } = new();
    public List<EtiquetaCatalogo> Generos { get; set; } = new();
    public List<ComboConfiteria> Combos { get; set; } = new();
    public Dictionary<string, int> AliasGeneros { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public Dictionary<int, int> GenerosPeliculas { get; set; } = new();
    public Dictionary<int, List<int>> GenerosMultiplesPeliculas { get; set; } = new();
    public int UltimaCategoria { get; set; }
    public int UltimoGenero { get; set; }
    public int UltimoCombo { get; set; }
    public EstadoCatalogos Copiar() => new()
    {
        Distribuidoras = Distribuidoras.Select(d => d.Copiar()).ToList(), UltimaDistribuidora = UltimaDistribuidora,
        Categorias = Categorias.Select(x => x.Copiar()).ToList(), Generos = Generos.Select(x => x.Copiar()).ToList(), Combos = Combos.Select(x => x.Copiar()).ToList(),
        AliasGeneros = new(AliasGeneros, StringComparer.OrdinalIgnoreCase), GenerosPeliculas = new(GenerosPeliculas),
        GenerosMultiplesPeliculas = GenerosMultiplesPeliculas.ToDictionary(x => x.Key, x => new List<int>(x.Value)),
        UltimaCategoria = UltimaCategoria, UltimoGenero = UltimoGenero, UltimoCombo = UltimoCombo
    };
}

public sealed class Distribuidora
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Contacto { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
    public Distribuidora Copiar() => new() { Id = Id, Nombre = Nombre, Contacto = Contacto, Email = Email, Telefono = Telefono };
}
