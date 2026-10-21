namespace CineSpace.Server.Models;

public abstract class RegistroCatalogo
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public string Descripcion { get; set; } = "";
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
}
public sealed class TipoSalaRegistro : RegistroCatalogo { public TipoSalaRegistro() => Id = -1; }
public sealed class ExperienciaRegistro : RegistroCatalogo { public ExperienciaRegistro() => Id = -1; }
public sealed class GeneroRegistro : RegistroCatalogo;
public sealed class CategoriaRegistro : RegistroCatalogo;
public sealed class DistribuidoraRegistro : RegistroCatalogo
{
    public string Contacto { get; set; } = "";
    public string Email { get; set; } = "";
    public string Telefono { get; set; } = "";
}
public sealed class DisenoRegistro : RegistroCatalogo
{
    public int Filas { get; set; }
    public int Columnas { get; set; }
    public string PlanoJson { get; set; } = "{}";
}
public sealed class SalaRegistro
{
    public int Id { get; set; }
    public string Nombre { get; set; } = "";
    public int DisenoId { get; set; }
    public int TipoSalaId { get; set; }
    public decimal Precio { get; set; }
    public int Descuento { get; set; }
    public bool Activa { get; set; } = true;
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DisenoRegistro Diseno { get; set; } = null!;
    public TipoSalaRegistro TipoSala { get; set; } = null!;
    public ICollection<SalaExperienciaRegistro> Experiencias { get; set; } = [];
}
public sealed class SalaExperienciaRegistro
{
    public int SalaId { get; set; }
    public int ExperienciaId { get; set; }
    public SalaRegistro Sala { get; set; } = null!;
    public ExperienciaRegistro Experiencia { get; set; } = null!;
}
public sealed class PeliculaRegistro
{
    public int Id { get; set; }
    public int? TmdbId { get; set; }
    public string Titulo { get; set; } = "";
    public string Sinopsis { get; set; } = "";
    public string Poster { get; set; } = "";
    public string Fondo { get; set; } = "";
    public string TrailerUrl { get; set; } = "";
    public int FocoPoster { get; set; } = 50;
    public int FocoFondo { get; set; } = 35;
    public int Duracion { get; set; } = 120;
    public int Limpieza { get; set; } = 20;
    public DateOnly? Estreno { get; set; }
    public int? DistribuidoraId { get; set; }
    public string IdiomasJson { get; set; } = "[]";
    public bool Preventa { get; set; }
    public bool Publicada { get; set; } = true;
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DistribuidoraRegistro? Distribuidora { get; set; }
    public ICollection<PeliculaGeneroRegistro> Generos { get; set; } = [];
}
public sealed class PeliculaGeneroRegistro
{
    public int PeliculaId { get; set; }
    public int GeneroId { get; set; }
    public PeliculaRegistro Pelicula { get; set; } = null!;
    public GeneroRegistro Genero { get; set; } = null!;
}
public sealed class ProgramacionRegistro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PeliculaId { get; set; }
    public int SalaId { get; set; }
    public int ExperienciaId { get; set; }
    public DateOnly Desde { get; set; }
    public DateOnly Hasta { get; set; }
    public string HorarioJson { get; set; } = "{}";
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public PeliculaRegistro Pelicula { get; set; } = null!;
    public SalaRegistro Sala { get; set; } = null!;
    public ExperienciaRegistro Experiencia { get; set; } = null!;
}
public sealed class FuncionRegistro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProgramacionId { get; set; }
    public int PeliculaId { get; set; }
    public int SalaId { get; set; }
    public int ExperienciaId { get; set; }
    public DateTime InicioUtc { get; set; }
    public DateTime SalaListaUtc { get; set; }
    public int Duracion { get; set; }
    public int Limpieza { get; set; }
    public bool Preventa { get; set; }
    public ProgramacionRegistro Programacion { get; set; } = null!;
    public PeliculaRegistro Pelicula { get; set; } = null!;
    public SalaRegistro Sala { get; set; } = null!;
    public ExperienciaRegistro Experiencia { get; set; } = null!;
}
public sealed class VentaRegistro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string NumeroFactura { get; set; } = "";
    public string Codigo { get; set; } = "";
    public Guid UsuarioId { get; set; }
    public Guid? OperadorId { get; set; }
    public Guid FuncionId { get; set; }
    public string Solicitud { get; set; } = "";
    public decimal Total { get; set; }
    public int Cantidad { get; set; }
    public string MetodoPago { get; set; } = "";
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public DateTime? VerificadaUtc { get; set; }
    public string SnapshotJson { get; set; } = "{}";
    public Usuario Usuario { get; set; } = null!;
    public FuncionRegistro Funcion { get; set; } = null!;
    public ICollection<ButacaVendidaRegistro> Butacas { get; set; } = [];
}
public sealed class ButacaVendidaRegistro
{
    public Guid FuncionId { get; set; }
    public string Asiento { get; set; } = "";
    public Guid VentaId { get; set; }
    public VentaRegistro Venta { get; set; } = null!;
    public FuncionRegistro Funcion { get; set; } = null!;
}
public sealed class ResenaRegistro
{
    public bool Destacada { get; set; }
    public Guid Id { get; set; } = Guid.NewGuid();
    public int PeliculaId { get; set; }
    public Guid UsuarioId { get; set; }
    public int Puntuacion { get; set; }
    public string Comentario { get; set; } = "";
    public DateTime FechaUtc { get; set; } = DateTime.UtcNow;
    public PeliculaRegistro Pelicula { get; set; } = null!;
    public Usuario Usuario { get; set; } = null!;
}
public sealed class ComboRegistro : RegistroCatalogo
{
    public int CategoriaId { get; set; }
    public decimal Precio { get; set; }
    public string Foto { get; set; } = "";
    public bool Publicado { get; set; } = true;
    public CategoriaRegistro Categoria { get; set; } = null!;
}
public sealed class PedidoRegistro
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Numero { get; set; } = "";
    public string Token { get; set; } = "";
    public string Solicitud { get; set; } = "";
    public string Codigo { get; set; } = "";
    public Guid? UsuarioId { get; set; }
    public string PropietarioInvitado { get; set; } = "";
    public string NombreRetiro { get; set; } = "";
    public string Estado { get; set; } = "Pendiente";
    public string MetodoPago { get; set; } = "";
    public decimal Total { get; set; }
    public DateTime CreadoUtc { get; set; } = DateTime.UtcNow;
    public DateTime? RetiradoUtc { get; set; }
    public Guid? EntregadoPorId { get; set; }
    public Usuario? Usuario { get; set; }
    public Usuario? EntregadoPor { get; set; }
    public ICollection<PedidoLineaRegistro> Lineas { get; set; } = [];
}
public sealed class PedidoLineaRegistro
{
    public int Id { get; set; }
    public Guid PedidoId { get; set; }
    public int ComboId { get; set; }
    public string Nombre { get; set; } = "";
    public string Categoria { get; set; } = "";
    public int Cantidad { get; set; }
    public decimal PrecioUnitario { get; set; }
    public PedidoRegistro Pedido { get; set; } = null!;
    public ComboRegistro Combo { get; set; } = null!;
}
public sealed class TrabajadorRegistro
{
    public int Id { get; set; }
    public Guid UsuarioId { get; set; }
    public DateOnly Nacimiento { get; set; }
    public string Telefono { get; set; } = "";
    public string Direccion { get; set; } = "";
    public string Nacionalidad { get; set; } = "";
    public int Cargo { get; set; }
    public string Horario { get; set; } = "";
    public decimal Sueldo { get; set; }
    public Usuario Usuario { get; set; } = null!;
}
public sealed class BannerRegistro
{
    public int Id { get; set; }
    public int PeliculaId { get; set; }
    public string Imagen { get; set; } = "";
    public int Etiqueta { get; set; }
    public int Foco { get; set; } = 35;
    public int Orden { get; set; }
    public bool Activo { get; set; } = true;
    public PeliculaRegistro Pelicula { get; set; } = null!;
}
public sealed class EstrenoRegistro
{
    public int Id { get; set; }
    public int? PeliculaId { get; set; }
    public PeliculaRegistro? Pelicula { get; set; }
    public string Titulo { get; set; } = "";
    public string Imagen { get; set; } = "";
    public string Genero { get; set; } = "";
    public DateOnly Fecha { get; set; }
    public string Sinopsis { get; set; } = "";
    public bool Publicado { get; set; } = true;
}
public sealed class AjusteRegistro
{
    public string Clave { get; set; } = "";
    public string Valor { get; set; } = "";
}
