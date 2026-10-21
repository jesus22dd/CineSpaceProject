using System;

namespace CineSpace.Shared.Models
{
    public class CalificacionPelicula
    {
        public int PeliculaId { get; set; }
        public string NombrePelicula { get; set; } = string.Empty;
        public string EmailUsuario { get; set; } = string.Empty;
        public int Calificacion { get; set; } // 1-5 estrellas
        public DateTime FechaCalificacion { get; set; }
        public string Comentario { get; set; } = string.Empty;
        public bool Destacada { get; set; }

        // Para validar que la calificación esté en rango
        public bool EsCalificacionValida => Calificacion >= 1 && Calificacion <= 5;

        // Constructor para crear nuevas calificaciones
        public CalificacionPelicula()
        {
            FechaCalificacion = DateTime.Now;
        }
    }
}
