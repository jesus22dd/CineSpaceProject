namespace CineSpace.Server.Services;
public sealed class OperacionCineException(string mensaje) : InvalidOperationException(mensaje);
