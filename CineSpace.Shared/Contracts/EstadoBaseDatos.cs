namespace CineSpace.Shared.Contracts;

public sealed record EstadoBaseDatos(bool Conexion, bool Preparada, string Mensaje);
