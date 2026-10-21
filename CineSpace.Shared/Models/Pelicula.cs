using System.Collections.Generic;

namespace CineSpace.Shared.Models
{
    public class Pelicula
    {
        public int Id { get; set; }
        public int? TmdbId { get; set; }
        public bool ImagenManual { get; set; }
        public DateTime? FechaAgregadaUtc { get; set; }
        public DateTime? FechaEstreno { get; set; }
        public int? DistribuidoraId { get; set; }
        // Reservation context is copied per purchase; the catalog never owns a room or experience.
        [System.Text.Json.Serialization.JsonIgnore] public int? ExperienciaFuncion { get; set; }
        [System.Text.Json.Serialization.JsonIgnore] public DateTime? FechaFuncion { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public List<string> Generos { get; set; } = new();
        [System.Text.Json.Serialization.JsonIgnore]
        public IReadOnlyList<string> ListaGeneros => (Generos?.Count > 0 ? Generos : new List<string> { Genero })
            .Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        [System.Text.Json.Serialization.JsonIgnore] public string GenerosTexto => string.Join(" · ", ListaGeneros);
        public bool TieneGenero(string genero) => ListaGeneros.Contains(genero, StringComparer.OrdinalIgnoreCase);
        public List<string> HorariosDisponibles { get; set; } = new();
        public List<string> IdiomasDisponibles { get; set; } = new();
        public bool EnPreventa { get; set; }
        public bool DBoxDisponible { get; set; }
        public List<int> Experiencias { get; set; } = new();
        public int FuncionesDisponibles { get; set; }
        public string ImagenUrl { get; set; } = string.Empty;
        public string ImagenPanoramicaUrl { get; set; } = string.Empty;
        public int FocoPosterVertical { get; set; } = 50;
        public int FocoPanoramicaVertical { get; set; } = 35;
        public string HoraPrimeraFuncion { get; set; } = "14:00";
        public int DuracionMinutos { get; set; } = 120;
        public int LimpiezaMinutos { get; set; } = 20;
        public bool ProgramacionConfigurada { get; set; }
        public int NumeroSala { get; set; } = 0; // Número de sala asignada (0 = sin sala asignada)
        public string Sinopsis { get; set; } = string.Empty; // Sinopsis de la película
        public string TrailerUrl { get; set; } = string.Empty; // URL del tráiler de YouTube
        public Pelicula Copiar() => new()
        {
            Experiencias = new(Experiencias), FuncionesDisponibles = FuncionesDisponibles,
            FechaEstreno = FechaEstreno, DistribuidoraId = DistribuidoraId,
            Id = Id, TmdbId = TmdbId, ImagenManual = ImagenManual, FechaAgregadaUtc = FechaAgregadaUtc, ExperienciaFuncion = ExperienciaFuncion, FechaFuncion = FechaFuncion, Titulo = Titulo, Genero = Genero, NumeroSala = NumeroSala,
            Generos = ListaGeneros.ToList(), HorariosDisponibles = new(HorariosDisponibles ?? new()), IdiomasDisponibles = new(IdiomasDisponibles),
            EnPreventa = EnPreventa, DBoxDisponible = DBoxDisponible, ImagenUrl = ImagenUrl,
            ImagenPanoramicaUrl = ImagenPanoramicaUrl, FocoPosterVertical = FocoPosterVertical, FocoPanoramicaVertical = FocoPanoramicaVertical, Sinopsis = Sinopsis, TrailerUrl = TrailerUrl,
            HoraPrimeraFuncion = HoraPrimeraFuncion, DuracionMinutos = DuracionMinutos, LimpiezaMinutos = LimpiezaMinutos, ProgramacionConfigurada = ProgramacionConfigurada
        };
    }
}
