using CineSpace.Server.Data;
using Microsoft.EntityFrameworkCore;
using ClosedXML.Excel;
namespace CineSpace.Server.Services;

public interface IExportacionResenasService { Task<byte[]> ExportarResenasAsync(CancellationToken ct); }

public sealed class ExportacionResenasService(CineSpaceDbContext db) : IExportacionResenasService
{
    public async Task<byte[]> ExportarResenasAsync(CancellationToken ct)
    {
        using var libro = new XLWorkbook(); var hoja = libro.Worksheets.Add("Reseñas");
        var columnas = new[] { "Película", "Correo del cliente", "Calificación", "Fecha", "Comentario", "Destacada" };
        for (var i = 0; i < columnas.Length; i++) hoja.Cell(1, i + 1).Value = columnas[i];
        hoja.Row(1).Style.Font.Bold = true;
        var fila = 2;
        await foreach (var r in db.Resenas.AsNoTracking().OrderByDescending(x => x.FechaUtc).Select(x => new { Pelicula = x.Pelicula.Titulo, x.Usuario.Email, x.Puntuacion, x.FechaUtc, x.Comentario, x.Destacada }).AsAsyncEnumerable().WithCancellation(ct))
        {
            hoja.Cell(fila, 1).Value = r.Pelicula; hoja.Cell(fila, 2).Value = r.Email ?? "";
            hoja.Cell(fila, 3).Value = r.Puntuacion; hoja.Cell(fila, 4).Value = ReglasCine.Local(r.FechaUtc).ToString("dd/MM/yyyy HH:mm");
            hoja.Cell(fila, 5).Value = r.Comentario; hoja.Cell(fila++, 6).Value = r.Destacada ? "Sí" : "No";
        }
        hoja.SheetView.FreezeRows(1); for (var i = 0; i < columnas.Length; i++) hoja.Column(i + 1).Width = i == 4 ? 70 : 30;
        using var memory = new MemoryStream(); libro.SaveAs(memory); return memory.ToArray();
    }
}
