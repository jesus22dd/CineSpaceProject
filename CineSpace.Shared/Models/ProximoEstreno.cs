namespace CineSpace.Shared.Models;

public sealed record ProximoEstreno(int Id, string Titulo, string ImagenUrl, string Genero, DateOnly FechaEstreno, string Sinopsis = "", bool Publicado = true);
