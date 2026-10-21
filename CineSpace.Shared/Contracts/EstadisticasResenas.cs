namespace CineSpace.Shared.Contracts;
public sealed record EstadisticasResenas(int Total, int Peliculas, int Usuarios, double Promedio, List<RecuentoCatalogo> Distribucion);
