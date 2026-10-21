using System;
using System.Collections.Generic;
using System.Linq;

namespace CineSpace.Shared.Models
{
    public class VentaTicket
    {
        public string Solicitud { get; set; } = Guid.NewGuid().ToString("N");
        public DateTime FechaVenta { get; set; }
        public string? FuncionId { get; set; }
        public DateTime? FechaFuncion { get; set; }
        public int PeliculaId { get; set; }
        public string Pelicula { get; set; } = string.Empty;
        public string Genero { get; set; } = string.Empty;
        public List<string> Generos { get; set; } = new();
        [System.Text.Json.Serialization.JsonIgnore]
        public IReadOnlyList<string> ListaGeneros => (Generos?.Count > 0 ? Generos : new List<string> { Genero })
            .Where(g => !string.IsNullOrWhiteSpace(g)).Select(g => g.Trim()).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        [System.Text.Json.Serialization.JsonIgnore] public string GenerosTexto => string.Join(" · ", ListaGeneros);
        public bool TieneGenero(string genero) => ListaGeneros.Contains(genero, StringComparer.OrdinalIgnoreCase);
        public int NumeroSala { get; set; }
        public int TipoSala { get; set; }
        public string TipoSalaNombre { get; set; } = "";
        public int TipoExperiencia { get; set; }
        public string ExperienciaNombre { get; set; } = "";
        public int TicketsMayores { get; set; }
        public int TicketsMenores { get; set; }
        public string Horario { get; set; } = string.Empty;
        public List<string> Asientos { get; set; } = new();
        public string NumeroFactura { get; set; } = string.Empty;
        public string Cliente { get; set; } = string.Empty;
        public string DocumentoCliente { get; set; } = string.Empty;
        public string MetodoPago { get; set; } = "";
        public bool EsPagoSimulado { get; set; }

        // Email del usuario que realizó la compra
        public string EmailUsuario { get; set; } = string.Empty;

        // Campo para saber si el usuario calificó esta película
        public bool FueCalificada { get; set; } = false;
        public bool PuedeCalificar { get; set; }
        public DateTime? FinFuncion { get; set; }
        public string ImagenPanoramicaUrl { get; set; } = string.Empty;

        // NUEVO: Código de validación para taquilleros
        public string CodigoValidacion { get; set; } = string.Empty;
        public DateTime? FechaVerificacion { get; set; }

        // Información detallada de categorías de asientos
        public Dictionary<CategoriaAsiento, int> AsientosMayoresPorCategoria { get; set; } = new Dictionary<CategoriaAsiento, int>();
        public Dictionary<CategoriaAsiento, int> AsientosMenoresPorCategoria { get; set; } = new Dictionary<CategoriaAsiento, int>();

        // Información de precios
        public Dictionary<CategoriaAsiento, decimal> PreciosMayoresPorCategoria { get; set; } = new Dictionary<CategoriaAsiento, decimal>();
        public Dictionary<CategoriaAsiento, decimal> PreciosMenoresPorCategoria { get; set; } = new Dictionary<CategoriaAsiento, decimal>();

        // Propiedades calculadas
        public int CantidadTickets => TicketsMayores + TicketsMenores;

        public decimal TotalVenta
        {
            get
            {
                decimal total = 0;

                // Si tenemos información detallada de precios por categoría
                if ((PreciosMayoresPorCategoria.Any() || PreciosMenoresPorCategoria.Any()) &&
                    AsientosMayoresPorCategoria.Values.Sum() == TicketsMayores &&
                    AsientosMenoresPorCategoria.Values.Sum() == TicketsMenores &&
                    AsientosMayoresPorCategoria.All(asiento => asiento.Value == 0 || PreciosMayoresPorCategoria.ContainsKey(asiento.Key)) &&
                    AsientosMenoresPorCategoria.All(asiento => asiento.Value == 0 || PreciosMenoresPorCategoria.ContainsKey(asiento.Key)))
                {
                    // Sumar precios de adultos por categoría
                    foreach (var categoria in AsientosMayoresPorCategoria.Keys)
                    {
                        if (PreciosMayoresPorCategoria.ContainsKey(categoria))
                        {
                            total += AsientosMayoresPorCategoria[categoria] * PreciosMayoresPorCategoria[categoria];
                        }
                    }

                    // Sumar precios de niños por categoría
                    foreach (var categoria in AsientosMenoresPorCategoria.Keys)
                    {
                        if (PreciosMenoresPorCategoria.ContainsKey(categoria))
                        {
                            total += AsientosMenoresPorCategoria[categoria] * PreciosMenoresPorCategoria[categoria];
                        }
                    }

                    return total;
                }

                // Compatibilidad con versiones anteriores o si no hay información detallada
                // Calcular basado en tipo de sala y experiencia
                try
                {
                    decimal precioMayores = GetPrecioBaseMayores();
                    decimal precioMenores = GetPrecioBaseMenores();
                    return (TicketsMayores * precioMayores) + (TicketsMenores * precioMenores);
                }
                catch
                {
                    // Si hay error, usar los precios base antiguos
                    return (TicketsMayores * 50) + (TicketsMenores * 30);
                }
            }
        }

        // Obtener precio base para mayores según tipo de sala y experiencia
        private decimal GetPrecioBaseMayores()
        {
            // Precios para categoría Regular
            return (TipoSala, TipoExperiencia) switch
            {
                (CineSpace.Shared.Models.TipoSala.Estandar, CineSpace.Shared.Models.TipoExperiencia.D2) => 35m,
                (CineSpace.Shared.Models.TipoSala.Estandar, CineSpace.Shared.Models.TipoExperiencia.D3) => 50m,
                (CineSpace.Shared.Models.TipoSala.Premium, CineSpace.Shared.Models.TipoExperiencia.D2) => 50m,
                (CineSpace.Shared.Models.TipoSala.Premium, CineSpace.Shared.Models.TipoExperiencia.D3) => 65m,
                (CineSpace.Shared.Models.TipoSala.IMAX, CineSpace.Shared.Models.TipoExperiencia.D2) => 70m,
                (CineSpace.Shared.Models.TipoSala.IMAX, CineSpace.Shared.Models.TipoExperiencia.D3) => 85m,
                (CineSpace.Shared.Models.TipoSala.IMAX, CineSpace.Shared.Models.TipoExperiencia.D4X) => 95m,
                (CineSpace.Shared.Models.TipoSala.XD, CineSpace.Shared.Models.TipoExperiencia.D2) => 75m,
                (CineSpace.Shared.Models.TipoSala.XD, CineSpace.Shared.Models.TipoExperiencia.D3) => 90m,
                (CineSpace.Shared.Models.TipoSala.XD, CineSpace.Shared.Models.TipoExperiencia.D4X) => 100m,
                _ => 50m  // Precio base por defecto
            };
        }

        // Obtener precio base para menores según tipo de sala y experiencia
        private decimal GetPrecioBaseMenores()
        {
            decimal precioBase = GetPrecioBaseMayores();
            return Math.Floor(precioBase * 0.75m / 5) * 5; // 25% descuento redondeado a 5
        }
    }
}
