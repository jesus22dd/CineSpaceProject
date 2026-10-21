using CineSpace.Shared.Models;

namespace CineSpace.Client.Services;

public static class AnaliticaVentas
{
    public static List<VentaTicket> Filtrar(IEnumerable<VentaTicket> ventas, FiltroVentas filtro, DateTime hoy)
    {
        var inicio = filtro.Periodo switch {
            "hoy" => hoy.Date, "semana" => hoy.Date.AddDays(-(int)hoy.DayOfWeek),
            "mes" => new DateTime(hoy.Year, hoy.Month, 1), _ => DateTime.MinValue
        };
        return ventas.Where(v => v.FechaVenta >= inicio && v.FechaVenta < hoy.Date.AddDays(1)
            && (filtro.Genero == "todos" || v.TieneGenero(filtro.Genero))
            && (filtro.TipoSala == "todos" || TipoSala.Codigo(v.TipoSala) == filtro.TipoSala)
            && (filtro.Experiencia == "todos" || TipoExperiencia.Codigo(v.TipoExperiencia) == filtro.Experiencia))
            .OrderByDescending(v => v.FechaVenta).ToList();
    }
    public static IReadOnlyList<DatoGrafico> Publico(IEnumerable<VentaTicket> ventas) => new[] {
        new DatoGrafico("Adultos", ventas.Sum(v => v.TicketsMayores)),
        new DatoGrafico("Niños", ventas.Sum(v => v.TicketsMenores))
    }.Where(x => x.Valor > 0).ToList();
    public static IReadOnlyList<DatoGrafico> Generos(IEnumerable<VentaTicket> ventas) => ventas.SelectMany(v => v.ListaGeneros.Select(nombre => new { Nombre = nombre, Venta = v })).GroupBy(v => v.Nombre)
        .Select(g => new DatoGrafico(g.Key, g.Sum(v => v.Venta.CantidadTickets), g.Count(), g.Sum(v => v.Venta.CantidadTickets)))
        .OrderByDescending(x => x.Valor).ThenBy(x => x.Nombre).Take(5).ToList();
    public static IReadOnlyList<DatoGrafico> Salas(IEnumerable<VentaTicket> ventas, Func<int,string>? nombre = null) => ventas.GroupBy(v => v.NumeroSala)
        .Select(g => new DatoGrafico(nombre?.Invoke(g.Key) ?? $"Sala {g.Key}", g.Count(), g.Count(), g.Sum(v => v.CantidadTickets)))
        .OrderByDescending(x => x.Valor).ThenBy(x => x.Nombre).Take(5).ToList();
    public static IReadOnlyList<DatoGrafico> Experiencias(IEnumerable<VentaTicket> ventas) => Agrupar(ventas, v => NombreExperiencia(v));
    private static IReadOnlyList<DatoGrafico> Agrupar(IEnumerable<VentaTicket> ventas, Func<VentaTicket, string> nombre) => ventas.GroupBy(nombre)
        .Select(g => new DatoGrafico(g.Key, g.Count(), g.Count(), g.Sum(v => v.CantidadTickets)))
        .OrderByDescending(x => x.Valor).ThenBy(x => x.Nombre).Take(5).ToList();
    public static IReadOnlyList<DatoGrafico> Categorias(IEnumerable<VentaConfiteria> ventas) => ventas
        .SelectMany(v => v.Lineas.Select(l => new { v.NumeroVenta, Linea = l })).GroupBy(x => x.Linea.Categoria)
        .Select(g => new DatoGrafico(g.Key, g.Sum(x => x.Linea.Cantidad * x.Linea.PrecioUnitario), g.Select(x => x.NumeroVenta).Distinct().Count()))
        .OrderByDescending(x => x.Valor).ThenBy(x => x.Nombre).Take(5).ToList();
    public static IReadOnlyList<DateTime> Dias(DateTime hoy, int cantidad) => Enumerable.Range(0, cantidad).Select(i => hoy.Date.AddDays(i - cantidad + 1)).ToArray();
    public static IReadOnlyList<SerieGrafico> Ingresos(IEnumerable<VentaTicket> cine, IEnumerable<VentaConfiteria> confiteria, IReadOnlyList<DateTime> dias, bool incluirTotal = false)
    {
        var ingresosCine = cine.GroupBy(v => v.FechaVenta.Date).ToDictionary(g => g.Key, g => g.Sum(v => v.TotalVenta));
        var ingresosConfiteria = confiteria.GroupBy(v => v.FechaVenta.Date).ToDictionary(g => g.Key, g => g.Sum(v => v.Total));
        var valoresCine = dias.Select(d => ingresosCine.GetValueOrDefault(d)).ToArray();
        var valoresConfiteria = dias.Select(d => ingresosConfiteria.GetValueOrDefault(d)).ToArray();
        var series = new List<SerieGrafico> { new("Cine", valoresCine), new("Confitería", valoresConfiteria) };
        if (incluirTotal) series.Add(new("Total", valoresCine.Zip(valoresConfiteria, (a, b) => a + b).ToArray()));
        return series;
    }
    public static string Sala(int tipo) => TipoSala.NombreOriginal(tipo);
    public static string NombreTipoSala(VentaTicket venta) => string.IsNullOrWhiteSpace(venta.TipoSalaNombre) ? Sala(venta.TipoSala) : venta.TipoSalaNombre;
    public static string NombreExperiencia(VentaTicket venta) => string.IsNullOrEmpty(venta.ExperienciaNombre) ? Experiencia(venta.TipoExperiencia) : venta.ExperienciaNombre;
    public static string Experiencia(int tipo) => tipo switch { TipoExperiencia.D2 => "2D", TipoExperiencia.D3 => "3D", TipoExperiencia.D4X => "4DX", TipoExperiencia.XD3D => "XD3D", _ => tipo.ToString() };
}
