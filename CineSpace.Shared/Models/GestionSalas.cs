using System.Text.Json.Serialization;
namespace CineSpace.Shared.Models;

public sealed class TipoSalaCine
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public TipoSalaCine Copiar() => new() { Id = Id, Nombre = Nombre, Descripcion = Descripcion };
}

public sealed class ExperienciaCine
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public ExperienciaCine Copiar() => new() { Id = Id, Nombre = Nombre, Descripcion = Descripcion };
}

public sealed class DisenoButacas
{
    public int Id { get; set; }
    public PlanoSala Plano { get; set; } = new();
    public DisenoButacas Copiar() => new() { Id = Id, Plano = Plano.Copiar() };
}

public sealed class SalaCine
{
    public bool TieneProgramacion { get; set; }
    public int Capacidad { get; set; }
    public int Numero { get; set; }
    public string Nombre { get; set; } = "";
    public int DisenoId { get; set; }
    public int Tipo { get; set; }
    public List<int> Experiencias { get; set; } = new();
    public decimal Precio { get; set; } = 35m;
    public bool Oferta { get; set; }
    public int Descuento { get; set; }
    [JsonIgnore] public decimal PrecioFinal => decimal.Round(Precio * (Oferta ? (100 - Descuento) / 100m : 1m), 2, MidpointRounding.AwayFromZero);
    public SalaCine Copiar() => new() { TieneProgramacion = TieneProgramacion, Capacidad = Capacidad, Numero = Numero, Nombre = Nombre, DisenoId = DisenoId, Tipo = Tipo, Experiencias = new(Experiencias), Precio = Precio, Oferta = Oferta, Descuento = Descuento };
}

public sealed class EstadoGestionSalas
{
    public int Version { get; set; } = 2;
    public int ProximaExperiencia { get; set; } = 4;
    public int ProximoTipoSala { get; set; } = 4;
    // Valores iniciales para los estados v2 que todavía no incluían este catálogo.
    public List<TipoSalaCine> TiposSala { get; set; } = TipoSala.Originales();
    public int ProximoDiseno { get; set; } = 1;
    public int ProximaSala { get; set; } = 31;
    public List<ExperienciaCine> Experiencias { get; set; } = new();
    public List<DisenoButacas> Disenos { get; set; } = new();
    public List<SalaCine> Salas { get; set; } = new();
}
