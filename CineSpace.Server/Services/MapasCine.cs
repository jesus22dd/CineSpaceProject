using CineSpace.Server.Models;
using CineSpace.Shared.Models;

namespace CineSpace.Server.Services;

public static class MapasCine
{
    public static Pelicula Pelicula(PeliculaRegistro x) => new()
    {
        Id = x.Id, TmdbId = x.TmdbId, Titulo = x.Titulo, Sinopsis = x.Sinopsis, ImagenUrl = x.Poster, ImagenPanoramicaUrl = x.Fondo,
        TrailerUrl = x.TrailerUrl, FocoPosterVertical = x.FocoPoster, FocoPanoramicaVertical = x.FocoFondo, ImagenManual = x.TmdbId is null,
        DuracionMinutos = x.Duracion, LimpiezaMinutos = x.Limpieza, FechaEstreno = x.Estreno?.ToDateTime(TimeOnly.MinValue),
        DistribuidoraId = x.DistribuidoraId, FechaAgregadaUtc = x.CreadoUtc, IdiomasDisponibles = ReglasCine.Leer<List<string>>(x.IdiomasJson),
        EnPreventa = x.Preventa, Genero = x.Generos.Select(g => g.Genero.Nombre).FirstOrDefault() ?? "",
        Generos = x.Generos.Select(g => g.Genero.Nombre).OrderBy(g => g).ToList(), HorariosDisponibles = []
    };
    public static SalaCine Sala(SalaRegistro x) => new()
    {
        Numero = x.Id, Nombre = x.Nombre, DisenoId = x.DisenoId, Tipo = x.TipoSalaId, Precio = x.Precio,
        Descuento = x.Descuento, Oferta = x.Descuento > 0, Experiencias = x.Experiencias.Select(e => e.ExperienciaId).ToList(),
        Capacidad = x.Diseno is null ? 0 : ReglasCine.Leer<PlanoSala>(x.Diseno.PlanoJson).Asientos.Count
    };
    public static ProgramacionSala Programacion(ProgramacionRegistro x) => new()
    {
        Id = x.Id.ToString("N"), PeliculaId = x.PeliculaId, NumeroSala = x.SalaId, ExperienciaId = x.ExperienciaId,
        Desde = x.Desde.ToDateTime(TimeOnly.MinValue), Hasta = x.Hasta.ToDateTime(TimeOnly.MinValue),
        FechaAgregadaUtc = x.CreadoUtc, Horario = ReglasCine.Leer<ProgramacionFunciones>(x.HorarioJson)
    };
    public static FuncionCine Funcion(FuncionRegistro x) => new()
    {
        Id = x.Id.ToString("N"), ProgramacionId = x.ProgramacionId.ToString("N"), PeliculaId = x.PeliculaId,
        NumeroSala = x.SalaId, ExperienciaId = x.ExperienciaId, Inicio = ReglasCine.Local(x.InicioUtc), DuracionMinutos = x.Duracion,
        LimpiezaMinutos = x.Limpieza, EnPreventa = x.Preventa
    };
    public static ComboConfiteria Combo(ComboRegistro x) => new()
    { Id = x.Id, Nombre = x.Nombre, Descripcion = x.Descripcion, CategoriaId = x.CategoriaId, Foto = x.Foto, Publicado = x.Publicado, Precio = x.Precio };
    public static Distribuidora Distribuidora(DistribuidoraRegistro x) => new()
    { Id = x.Id, Nombre = x.Nombre, Contacto = x.Contacto, Email = x.Email, Telefono = x.Telefono };
    public static BannerInicio Banner(BannerRegistro x) => new()
    { Id = x.Id, PeliculaId = x.PeliculaId, Imagen = x.Imagen, Etiqueta = (EtiquetaBanner)x.Etiqueta, FocoVertical = x.Foco, Activo = x.Activo };
    public static ProximoEstreno Estreno(EstrenoRegistro x) => new(x.Id, x.Titulo, x.Imagen, x.Genero, x.Fecha, x.Sinopsis, x.Publicado);
    public static VentaTicket Venta(VentaRegistro x)
    {
        var venta = ReglasCine.Leer<VentaTicket>(x.SnapshotJson);
        venta.NumeroFactura = x.NumeroFactura; venta.CodigoValidacion = x.Codigo; venta.FuncionId = x.FuncionId.ToString("N");
        venta.FechaVenta = ReglasCine.Local(x.FechaUtc); venta.MetodoPago = x.MetodoPago;
        venta.FechaVerificacion = x.VerificadaUtc.HasValue ? ReglasCine.Local(x.VerificadaUtc.Value) : null;
        return venta;
    }
    public static PedidoConfiteria Pedido(PedidoRegistro x) => new()
    {
        Numero = x.Numero, Token = x.Token, Solicitud = x.Solicitud, CodigoRetiro = x.Codigo, NombreRetiro = x.NombreRetiro,
        EmailUsuario = x.Usuario?.Email ?? "", FechaCreacion = ReglasCine.Local(x.CreadoUtc), FechaRetiro = x.RetiradoUtc.HasValue ? ReglasCine.Local(x.RetiradoUtc.Value) : null,
        EntregadoPor = x.EntregadoPor is null ? "" : $"{x.EntregadoPor.Nombre} {x.EntregadoPor.Apellido}", MedioPago = x.MetodoPago,
        Estado = Enum.Parse<EstadoPedidoConfiteria>(x.Estado), Lineas = x.Lineas.Select(l => new LineaVentaConfiteria(l.Nombre, l.Categoria, l.Cantidad, l.PrecioUnitario)).ToList()
    };
}
