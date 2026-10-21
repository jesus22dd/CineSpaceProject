using System;

namespace CineSpace.Shared.Models
{
    // Enumeración para los cargos de los trabajadores
    public enum Cargo
    {
        Taquillero,
        Limpieza,
        Proyeccionista,
        Gerente,
        Seguridad,
        Concesiones
    }

    public class Trabajador
    {
        // Identificación
        public int Id { get; set; }
        public DateTime? FechaRegistroUtc { get; set; }
        public string Nombre { get; set; } = string.Empty;
        public string Apellido { get; set; } = string.Empty;
        public string CI { get; set; } = string.Empty;

        // Datos personales
        public DateTime FechaNacimiento { get; set; }
        public string Telefono { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Direccion { get; set; } = string.Empty;
        public string Nacionalidad { get; set; } = string.Empty;

        // Datos laborales
        public Cargo CargoTrabajador { get; set; }
        public string HorarioTrabajo { get; set; } = string.Empty;
        public decimal Sueldo { get; set; }

        // Método para calcular la edad del trabajador
        public int ObtenerEdad()
        {
            var hoy = DateTime.Today;
            var edad = hoy.Year - FechaNacimiento.Year;

            if (FechaNacimiento.Date > hoy.AddYears(-edad))
            {
                edad--;
            }

            return edad;
        }

        // Método estático para obtener el sueldo según el cargo
        public static decimal ObtenerSueldoPorCargo(Cargo cargo)
        {
            return cargo switch
            {
                Cargo.Taquillero => 2500m,
                Cargo.Limpieza => 2300m,
                Cargo.Proyeccionista => 2800m,
                Cargo.Gerente => 5000m,
                Cargo.Seguridad => 2600m,
                Cargo.Concesiones => 2400m,
                _ => 2000m
            };
        }

        // Método para obtener el nombre completo
        public string ObtenerNombreCompleto()
        {
            return $"{Nombre} {Apellido}";
        }
        public bool EsMayorDeEdad(int edadMinima = 18)
        {
            return ObtenerEdad() >= edadMinima;
        }
    }
}
