namespace CineSpace.Shared.Models;
public sealed record GaleriaPelicula(IReadOnlyList<ImagenPelicula> Posters, IReadOnlyList<ImagenPelicula> Panoramicas);
