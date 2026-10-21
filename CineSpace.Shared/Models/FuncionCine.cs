using System.Text.Json.Serialization;
namespace CineSpace.Shared.Models;

public sealed class FuncionCine
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string ProgramacionId { get; set; } = "";
    public int PeliculaId { get; set; }
    public int NumeroSala { get; set; }
    public int ExperienciaId { get; set; }
    public DateTime Inicio { get; set; }
    public int DuracionMinutos { get; set; }
    public int LimpiezaMinutos { get; set; }
    public bool EnPreventa { get; set; }
    public int Capacidad { get; set; }
    public int ButacasOcupadas { get; set; }
    [JsonIgnore] public DateTime Fin => Inicio.AddMinutes(DuracionMinutos);
    [JsonIgnore] public DateTime SalaLista => Fin.AddMinutes(LimpiezaMinutos);
    public FuncionCine Copiar() => (FuncionCine)MemberwiseClone();
}

public sealed class ProgramacionSala
{
    public int CantidadFunciones { get; set; }
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public DateTime? FechaAgregadaUtc { get; set; }
    public int PeliculaId { get; set; }
    public int NumeroSala { get; set; }
    public int ExperienciaId { get; set; }
    public DateTime Desde { get; set; } = DateTime.Today;
    public DateTime Hasta { get; set; } = DateTime.Today;
    public ProgramacionFunciones Horario { get; set; } = new();
    public ProgramacionSala Copiar() => new() { CantidadFunciones = CantidadFunciones, Id=Id, FechaAgregadaUtc=FechaAgregadaUtc, PeliculaId=PeliculaId, NumeroSala=NumeroSala, ExperienciaId=ExperienciaId, Desde=Desde, Hasta=Hasta, Horario=Horario.Copiar() };
}

public sealed class EstadoCartelera
{
    public int Version { get; set; } = 3;
    public List<ProgramacionSala> Programaciones { get; set; } = new();
    public List<FuncionCine> Funciones { get; set; } = new();
    public List<VentaTicket> Compras { get; set; } = new();
}
