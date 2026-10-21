
using System.Collections.Generic;

namespace CineSpace.Shared.Models
{
    public class Usuario
    {
        public Guid Id { get; set; }
        public bool TienePassword { get; set; } = true;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string CI { get; set; } = string.Empty;
        public DateTime? FechaRegistroUtc { get; set; }
        public RolUsuario Rol { get; set; } = RolUsuario.Cliente;

        // Historial de compras
        public List<VentaTicket> HistorialCompras { get; set; } = new List<VentaTicket>();

        // Calificaciones de películas
        public List<CalificacionPelicula> Calificaciones { get; set; } = new List<CalificacionPelicula>();

        // Método para agregar una calificación
        public void AgregarCalificacion(CalificacionPelicula calificacion)
        {
            // Eliminar cualquier calificación previa de la misma película
            Calificaciones.RemoveAll(c => c.PeliculaId == calificacion.PeliculaId);

            // Agregar la nueva calificación
            Calificaciones.Add(calificacion);
        }

        // Método para obtener la calificación de una película
        public CalificacionPelicula? ObtenerCalificacion(int peliculaId)
        {
            return Calificaciones.Find(c => c.PeliculaId == peliculaId);
        }
    }
}
