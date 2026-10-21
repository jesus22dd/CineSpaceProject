using System.ComponentModel.DataAnnotations;
using CineSpace.Shared.Models;

namespace CineSpace.Shared.Contracts;

public sealed class ConsultaPagina
{
    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 8;
    public string Buscar { get; set; } = "";
    public string Orden { get; set; } = "recientes";
    public int? Categoria { get; set; }
    public int? Genero { get; set; }
    public int? Sala { get; set; }
    public int? Experiencia { get; set; }
    public string Estado { get; set; } = "";
    public string Rol { get; set; } = "";
    public string Idioma { get; set; } = "";
    public int? Pelicula { get; set; }
    public int Minimo { get; set; }
    public int Maximo { get; set; }
    public bool Destacadas { get; set; }
    public string Campo { get; set; } = "Fecha";
    public DateOnly? Desde { get; set; }
    public DateOnly? Hasta { get; set; }
    public int Salto => (Math.Max(1, Pagina) - 1) * Math.Clamp(Tamano, 1, 100);
}

public sealed record Pagina<T>(List<T> Items, int Total, int Numero, int Tamano)
{
    public int Paginas => Math.Max(1, (int)Math.Ceiling(Total / (double)Tamano));
    public static Pagina<T> Vacia(int tamano = 8) => new([], 0, 1, tamano);
}

public sealed record ResultadoOperacion(bool Exito, string Mensaje);
public sealed record SesionCuenta(Usuario? Usuario, Trabajador? Trabajador);
public sealed record AccesoCuenta([Required, EmailAddress] string Email, [Required, MaxLength(128)] string Password, bool Recordarme = false);
public sealed record CambioClave([Required, MaxLength(128)] string Actual, [Required, MinLength(10), MaxLength(128)] string Nueva);
public sealed record SolicitudCorreo([Required, EmailAddress] string Email);
public sealed record RestablecerClave([Required, EmailAddress] string Email, [Required] string Token, [Required, MinLength(10), MaxLength(128)] string Password);
public sealed record ConfirmarCuenta([Required] string Usuario, [Required] string Token);
public sealed record TokenAntiforgery(string Token);
public sealed record ImagenGuardada(string Url, string Ruta);
public sealed record CambiarEstado(bool Activo);
public sealed record MoverElemento(int Direccion);
public sealed record PagoPedidoSolicitud([Required] string MetodoPago, bool QrConfirmado = false,
    [MaxLength(100)] string Titular = "", [MaxLength(23)] string Numero = "",
    [MaxLength(5)] string Vencimiento = "", [MaxLength(3)] string Cvv = "");
public sealed record SolicitudPedido(string Nombre, string Solicitud, List<LineaPedidoSolicitud> Items, decimal? TotalEsperado = null, PagoPedidoSolicitud? Pago = null);
public sealed record LineaPedidoSolicitud(int ComboId, int Cantidad);
public sealed record FiltroOpcion(string Id, string Nombre);
public sealed record RecuentoCatalogo(int Id, int Uso);
public sealed record GeneroDisponible(int Id, string Nombre, int Cantidad);

public sealed class ReferenciasCatalogo
{
    public List<TipoSalaCine> TiposSala { get; set; } = [];
    public List<ExperienciaCine> Experiencias { get; set; } = [];
    public List<EtiquetaCatalogo> Generos { get; set; } = [];
    public List<EtiquetaCatalogo> Categorias { get; set; } = [];
    public List<Distribuidora> Distribuidoras { get; set; } = [];
    public List<SalaCine> Salas { get; set; } = [];
    public List<DisenoButacas> Disenos { get; set; } = [];
    public Dictionary<int, int> UsosDistribuidoras { get; set; } = [];
    public Dictionary<int, int> UsosGeneros { get; set; } = [];
    public Dictionary<int, int> UsosCategorias { get; set; } = [];
    public Dictionary<int, int> UsosDisenos { get; set; } = [];
}

public sealed class ResumenSistema
{
    public int Usuarios { get; set; }
    public int Clientes { get; set; }
    public int Administradores { get; set; }
    public int Trabajadores { get; set; }
    public int Peliculas { get; set; }
    public int Salas { get; set; }
    public int Tickets { get; set; }
    public decimal Ingresos { get; set; }
    public decimal IngresosConfiteria { get; set; }
    public decimal Sueldos { get; set; }
    public double EdadPromedio { get; set; }
    public List<SerieAnalitica> Series { get; set; } = [];
}
public sealed record SerieAnalitica(string Nombre, decimal Total, int Cantidad);
