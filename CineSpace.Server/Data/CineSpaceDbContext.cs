using CineSpace.Server.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.DataProtection.EntityFrameworkCore;
using System.Text.RegularExpressions;
namespace CineSpace.Server.Data;
public sealed class CineSpaceDbContext(DbContextOptions<CineSpaceDbContext> options)
    : IdentityDbContext<Usuario, IdentityRole<Guid>, Guid>(options), IDataProtectionKeyContext
{
    public DbSet<DataProtectionKey> DataProtectionKeys => Set<DataProtectionKey>();
    public DbSet<TipoSalaRegistro> TiposSala => Set<TipoSalaRegistro>();
    public DbSet<ExperienciaRegistro> Experiencias => Set<ExperienciaRegistro>();
    public DbSet<GeneroRegistro> Generos => Set<GeneroRegistro>();
    public DbSet<CategoriaRegistro> Categorias => Set<CategoriaRegistro>();
    public DbSet<DistribuidoraRegistro> Distribuidoras => Set<DistribuidoraRegistro>();
    public DbSet<DisenoRegistro> Disenos => Set<DisenoRegistro>();
    public DbSet<SalaRegistro> Salas => Set<SalaRegistro>();
    public DbSet<SalaExperienciaRegistro> SalasExperiencias => Set<SalaExperienciaRegistro>();
    public DbSet<PeliculaRegistro> Peliculas => Set<PeliculaRegistro>();
    public DbSet<PeliculaGeneroRegistro> PeliculasGeneros => Set<PeliculaGeneroRegistro>();
    public DbSet<ProgramacionRegistro> Programaciones => Set<ProgramacionRegistro>();
    public DbSet<FuncionRegistro> Funciones => Set<FuncionRegistro>();
    public DbSet<VentaRegistro> Ventas => Set<VentaRegistro>();
    public DbSet<ButacaVendidaRegistro> ButacasVendidas => Set<ButacaVendidaRegistro>();
    public DbSet<ResenaRegistro> Resenas => Set<ResenaRegistro>();
    public DbSet<ComboRegistro> Combos => Set<ComboRegistro>();
    public DbSet<PedidoRegistro> Pedidos => Set<PedidoRegistro>();
    public DbSet<PedidoLineaRegistro> PedidoLineas => Set<PedidoLineaRegistro>();
    public DbSet<TrabajadorRegistro> Trabajadores => Set<TrabajadorRegistro>();
    public DbSet<BannerRegistro> Banners => Set<BannerRegistro>();
    public DbSet<EstrenoRegistro> Estrenos => Set<EstrenoRegistro>();
    public DbSet<AjusteRegistro> Ajustes => Set<AjusteRegistro>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        base.OnModelCreating(builder);
        builder.HasDefaultSchema("cine");
        builder.Entity<Usuario>(usuario =>
        {
            usuario.Property(u => u.Nombre).HasMaxLength(100);
            usuario.Property(u => u.Apellido).HasMaxLength(100);
            usuario.Property(u => u.Ci).HasMaxLength(30);
            usuario.HasIndex(u => u.NormalizedEmail).IsUnique();
            usuario.HasIndex(u => u.Ci).IsUnique().HasFilter("ci IS NOT NULL AND ci <> ''");
        });
        builder.Entity<Usuario>().ToTable("usuarios");
        builder.Entity<IdentityRole<Guid>>().ToTable("roles");
        builder.Entity<IdentityUserRole<Guid>>().ToTable("usuarios_roles");
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("usuarios_claims");
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("usuarios_logins");
        builder.Entity<IdentityUserToken<Guid>>().ToTable("usuarios_tokens");
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("roles_claims");
        builder.Entity<DataProtectionKey>().ToTable("claves_proteccion");

        Catalogo<TipoSalaRegistro>(builder, "tipos_sala");
        Catalogo<ExperienciaRegistro>(builder, "experiencias");
        Catalogo<GeneroRegistro>(builder, "generos");
        Catalogo<CategoriaRegistro>(builder, "categorias");
        Catalogo<DistribuidoraRegistro>(builder, "distribuidoras");
        Catalogo<DisenoRegistro>(builder, "disenos");
        Catalogo<ComboRegistro>(builder, "combos");
        builder.Entity<SalaRegistro>().ToTable("salas");
        builder.Entity<SalaRegistro>().HasIndex(x => x.Nombre).IsUnique();
        builder.Entity<SalaRegistro>().HasOne(x => x.Diseno).WithMany().HasForeignKey(x => x.DisenoId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalaRegistro>().HasOne(x => x.TipoSala).WithMany().HasForeignKey(x => x.TipoSalaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<SalaExperienciaRegistro>().ToTable("salas_experiencias").HasKey(x => new { x.SalaId, x.ExperienciaId });
        builder.Entity<SalaExperienciaRegistro>().HasOne(x => x.Sala).WithMany(x => x.Experiencias).HasForeignKey(x => x.SalaId);
        builder.Entity<SalaExperienciaRegistro>().HasOne(x => x.Experiencia).WithMany().HasForeignKey(x => x.ExperienciaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PeliculaRegistro>().ToTable("peliculas");
        builder.Entity<PeliculaRegistro>().HasIndex(x => x.TmdbId).IsUnique();
        builder.Entity<PeliculaRegistro>().HasIndex(x => x.CreadoUtc);
        builder.Entity<PeliculaRegistro>().HasOne(x => x.Distribuidora).WithMany().HasForeignKey(x => x.DistribuidoraId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PeliculaGeneroRegistro>().ToTable("peliculas_generos").HasKey(x => new { x.PeliculaId, x.GeneroId });
        builder.Entity<PeliculaGeneroRegistro>().HasOne(x => x.Pelicula).WithMany(x => x.Generos).HasForeignKey(x => x.PeliculaId);
        builder.Entity<PeliculaGeneroRegistro>().HasOne(x => x.Genero).WithMany().HasForeignKey(x => x.GeneroId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProgramacionRegistro>().ToTable("programaciones");
        builder.Entity<ProgramacionRegistro>().HasOne(x => x.Pelicula).WithMany().HasForeignKey(x => x.PeliculaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProgramacionRegistro>().HasOne(x => x.Sala).WithMany().HasForeignKey(x => x.SalaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProgramacionRegistro>().HasOne(x => x.Experiencia).WithMany().HasForeignKey(x => x.ExperienciaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FuncionRegistro>().ToTable("funciones");
        builder.Entity<FuncionRegistro>().HasOne(x => x.Programacion).WithMany().HasForeignKey(x => x.ProgramacionId).OnDelete(DeleteBehavior.Cascade);
        builder.Entity<FuncionRegistro>().HasOne(x => x.Pelicula).WithMany().HasForeignKey(x => x.PeliculaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FuncionRegistro>().HasOne(x => x.Sala).WithMany().HasForeignKey(x => x.SalaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FuncionRegistro>().HasOne(x => x.Experiencia).WithMany().HasForeignKey(x => x.ExperienciaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<FuncionRegistro>().HasIndex(x => new { x.SalaId, x.InicioUtc }).IsUnique();
        builder.Entity<FuncionRegistro>().HasIndex(x => new { x.PeliculaId, x.InicioUtc });
        builder.Entity<FuncionRegistro>().HasIndex(x => new { x.InicioUtc, x.ExperienciaId })
            .HasDatabaseName("funciones_fecha_experiencia").IncludeProperties(x => new { x.SalaId, x.PeliculaId });
        builder.Entity<VentaRegistro>().ToTable("ventas");
        builder.Entity<VentaRegistro>().HasIndex(x => x.NumeroFactura).IsUnique();
        builder.Entity<VentaRegistro>().HasIndex(x => x.Codigo).IsUnique();
        builder.Entity<VentaRegistro>().HasIndex(x => new { x.UsuarioId, x.Solicitud }).IsUnique();
        builder.Entity<VentaRegistro>().HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<VentaRegistro>().HasOne(x => x.Funcion).WithMany().HasForeignKey(x => x.FuncionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ButacaVendidaRegistro>().ToTable("butacas_vendidas").HasKey(x => new { x.FuncionId, x.Asiento });
        builder.Entity<ButacaVendidaRegistro>().HasOne(x => x.Venta).WithMany(x => x.Butacas).HasForeignKey(x => x.VentaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ButacaVendidaRegistro>().HasOne(x => x.Funcion).WithMany().HasForeignKey(x => x.FuncionId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ResenaRegistro>().ToTable("resenas");
        builder.Entity<ResenaRegistro>().HasIndex(x => new { x.UsuarioId, x.PeliculaId }).IsUnique();
        builder.Entity<ResenaRegistro>().HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ResenaRegistro>().HasOne(x => x.Pelicula).WithMany().HasForeignKey(x => x.PeliculaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ComboRegistro>().HasOne(x => x.Categoria).WithMany().HasForeignKey(x => x.CategoriaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PedidoRegistro>().ToTable("pedidos");
        builder.Entity<PedidoRegistro>().HasIndex(x => x.Numero).IsUnique();
        builder.Entity<PedidoRegistro>().HasIndex(x => x.Token).IsUnique();
        builder.Entity<PedidoRegistro>().HasIndex(x => x.Solicitud).IsUnique();
        builder.Entity<PedidoRegistro>().HasIndex(x => new { x.Estado, x.CreadoUtc });
        builder.Entity<PedidoRegistro>().HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PedidoRegistro>().HasOne(x => x.EntregadoPor).WithMany().HasForeignKey(x => x.EntregadoPorId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<PedidoLineaRegistro>().ToTable("pedido_lineas");
        builder.Entity<PedidoLineaRegistro>().HasOne(x => x.Pedido).WithMany(x => x.Lineas).HasForeignKey(x => x.PedidoId);
        builder.Entity<PedidoLineaRegistro>().HasOne(x => x.Combo).WithMany().HasForeignKey(x => x.ComboId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<TrabajadorRegistro>().ToTable("trabajadores");
        builder.Entity<TrabajadorRegistro>().HasIndex(x => x.UsuarioId).IsUnique();
        builder.Entity<TrabajadorRegistro>().HasOne(x => x.Usuario).WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<BannerRegistro>().ToTable("banners");
        builder.Entity<BannerRegistro>().HasOne(x => x.Pelicula).WithMany().HasForeignKey(x => x.PeliculaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<EstrenoRegistro>().ToTable("estrenos");
        builder.Entity<EstrenoRegistro>().HasIndex(x => new { x.Titulo, x.Fecha }).IsUnique();
        builder.Entity<EstrenoRegistro>().HasIndex(x => x.PeliculaId).IsUnique();
        builder.Entity<EstrenoRegistro>().HasOne(x => x.Pelicula).WithMany().HasForeignKey(x => x.PeliculaId).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<AjusteRegistro>().ToTable("ajustes").HasKey(x => x.Clave);
        // El prototipo usa 0 para 2D y Estandar; no representa una clave sin asignar.
        builder.Entity<ExperienciaRegistro>().Property(x => x.Id).HasSentinel(-1);
        builder.Entity<TipoSalaRegistro>().Property(x => x.Id).HasSentinel(-1);
        builder.Entity<SalaExperienciaRegistro>().Property(x => x.ExperienciaId).HasSentinel(-1).ValueGeneratedNever();
        builder.Entity<FuncionRegistro>().HasOne<SalaExperienciaRegistro>().WithMany().HasForeignKey(x => new { x.SalaId, x.ExperienciaId }).OnDelete(DeleteBehavior.Restrict);
        builder.Entity<ProgramacionRegistro>().HasOne<SalaExperienciaRegistro>().WithMany().HasForeignKey(x => new { x.SalaId, x.ExperienciaId }).OnDelete(DeleteBehavior.Restrict);

        foreach (var entity in builder.Model.GetEntityTypes())
        foreach (var property in entity.GetProperties())
        {
            property.SetColumnName(Regex.Replace(property.Name, "([a-z0-9])([A-Z])", "$1_$2").ToLowerInvariant());
            if (property.ClrType == typeof(decimal)) { property.SetPrecision(12); property.SetScale(2); }
            if (property.Name.EndsWith("Json", StringComparison.Ordinal)) property.SetColumnType("jsonb");
        }
    }

    private static void Catalogo<T>(ModelBuilder builder, string table) where T : RegistroCatalogo
    {
        builder.Entity<T>().HasBaseType((Type?)null).ToTable(table);
        builder.Entity<T>().Property(x => x.Nombre).HasMaxLength(200);
        builder.Entity<T>().HasIndex(x => x.Nombre).IsUnique();
    }
}
