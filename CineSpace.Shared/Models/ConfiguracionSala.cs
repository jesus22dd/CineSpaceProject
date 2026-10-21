using System;
using System.Collections.Generic;
using System.Linq;

namespace CineSpace.Shared.Models
{
    /// <summary>
    /// Clase que define la configuración de una sala de cine
    /// </summary>
    public class ConfiguracionSala
    {
        public int TipoSala { get; set; }
        public int NumeroSala { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public int NumeroFilas { get; set; }
        public int AsientosPorFila { get; set; }
        public List<int> ExperienciasDisponibles { get; set; } = new();

        // Mapa de asientos por categoría (Regular, Comfort)
        public Dictionary<CategoriaAsiento, List<string>> MapaAsientosPorCategoria { get; set; }

        public ConfiguracionSala(int numeroSala)
        {
            NumeroSala = numeroSala;
            Nombre = $"Sala {numeroSala}";
            MapaAsientosPorCategoria = new Dictionary<CategoriaAsiento, List<string>>();
            ExperienciasDisponibles = new List<int>();

            // Configuración basada en el número de sala
            if (numeroSala >= 1 && numeroSala <= 17)
            {
                ConfigurarSalaEstandar();
            }
            else if (numeroSala >= 18 && numeroSala <= 24)
            {
                ConfigurarSala3DInmersivo();
            }
            else if (numeroSala >= 25 && numeroSala <= 28)
            {
                ConfigurarSala4DXSensorial();
            }
            else if (numeroSala >= 29 && numeroSala <= 30)
            {
                ConfigurarSalaXD3D();
            }
            else
            {
                // Por defecto, configurar como sala estándar
                ConfigurarSalaEstandar();
            }
        }

        private void ConfigurarSalaEstandar()
        {
            TipoSala = CineSpace.Shared.Models.TipoSala.Estandar;
            NumeroFilas = 6;  // A-F
            AsientosPorFila = 10;

            // Experiencias disponibles: solo 2D
            ExperienciasDisponibles = new List<int> { TipoExperiencia.D2 };

            // Asignar categorías de asientos - Solo fila D es Comfort, el resto son Regular
            MapaAsientosPorCategoria[CategoriaAsiento.Regular] = GenerarAsientos(new List<char> { 'A', 'B', 'C', 'E', 'F' });
            MapaAsientosPorCategoria[CategoriaAsiento.Comfort] = GenerarAsientos(new List<char> { 'D' });
            MapaAsientosPorCategoria[CategoriaAsiento.VIP] = new List<string>(); // No hay VIP en ninguna sala
        }

        private void ConfigurarSala3DInmersivo()
        {
            TipoSala = CineSpace.Shared.Models.TipoSala.Premium;
            // Usar la misma configuración de filas y asientos que estándar
            NumeroFilas = 6;  // A-F
            AsientosPorFila = 10;

            // Experiencias disponibles: 2D y 3D
            ExperienciasDisponibles = new List<int> { TipoExperiencia.D2, TipoExperiencia.D3 };

            // Asignar categorías de asientos - Solo fila D es Comfort, el resto son Regular
            MapaAsientosPorCategoria[CategoriaAsiento.Regular] = GenerarAsientos(new List<char> { 'A', 'B', 'C', 'E', 'F' });
            MapaAsientosPorCategoria[CategoriaAsiento.Comfort] = GenerarAsientos(new List<char> { 'D' });
            MapaAsientosPorCategoria[CategoriaAsiento.VIP] = new List<string>(); // No hay VIP
        }

        private void ConfigurarSala4DXSensorial()
        {
            TipoSala = CineSpace.Shared.Models.TipoSala.IMAX;
            // Usar la misma configuración de filas y asientos que estándar
            NumeroFilas = 6;  // A-F
            AsientosPorFila = 10;

            // Experiencias disponibles: 2D, 3D y 4DX
            ExperienciasDisponibles = new List<int> { TipoExperiencia.D2, TipoExperiencia.D3, TipoExperiencia.D4X };

            // Asignar categorías de asientos - Solo fila D es Comfort, el resto son Regular
            MapaAsientosPorCategoria[CategoriaAsiento.Regular] = GenerarAsientos(new List<char> { 'A', 'B', 'C', 'E', 'F' });
            MapaAsientosPorCategoria[CategoriaAsiento.Comfort] = GenerarAsientos(new List<char> { 'D' });
            MapaAsientosPorCategoria[CategoriaAsiento.VIP] = new List<string>(); // No hay VIP
        }

        private void ConfigurarSalaXD3D()
        {
            TipoSala = CineSpace.Shared.Models.TipoSala.XD;
            // Usar la misma configuración de filas y asientos que estándar
            NumeroFilas = 6;  // A-F
            AsientosPorFila = 10;

            // Experiencias disponibles: 3D y XD3D
            ExperienciasDisponibles = new List<int> { TipoExperiencia.D3, TipoExperiencia.XD3D };

            // Asignar categorías de asientos - Solo fila D es Comfort, el resto son Regular
            MapaAsientosPorCategoria[CategoriaAsiento.Regular] = GenerarAsientos(new List<char> { 'A', 'B', 'C', 'E', 'F' });
            MapaAsientosPorCategoria[CategoriaAsiento.Comfort] = GenerarAsientos(new List<char> { 'D' });
            MapaAsientosPorCategoria[CategoriaAsiento.VIP] = new List<string>(); // No hay VIP
        }

        // Generar lista de asientos para las filas especificadas
        private List<string> GenerarAsientos(List<char> filas)
        {
            var asientos = new List<string>();
            foreach (char fila in filas)
            {
                for (int i = 1; i <= AsientosPorFila; i++)
                {
                    asientos.Add($"{fila}{i}");
                }
            }
            return asientos;
        }

        // Determinar la categoría de un asiento
        public CategoriaAsiento ObtenerCategoriaAsiento(string idAsiento)
        {
            foreach (var categoria in MapaAsientosPorCategoria.Keys)
            {
                if (MapaAsientosPorCategoria[categoria].Contains(idAsiento))
                {
                    return categoria;
                }
            }

            // Por defecto, retornar como Regular
            return CategoriaAsiento.Regular;
        }

        // Obtener todos los asientos de la sala
        public List<string> ObtenerTodosLosAsientos()
        {
            return MapaAsientosPorCategoria.Values
                .SelectMany(lista => lista)
                .ToList();
        }

        // Verificar si un tipo de experiencia está disponible para esta sala
        public bool ExperienciaDisponible(int experiencia)
        {
            return ExperienciasDisponibles.Contains(experiencia);
        }
    }
}
