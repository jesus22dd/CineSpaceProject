using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Npgsql;

namespace CineSpace.Server.Filters;

public sealed class ErrorApiHandler(IProblemDetailsService problems, ILogger<ErrorApiHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext context, Exception error, CancellationToken ct)
    {
        var (status, message) = error switch
        {
            ArgumentException => (400, error.Message),
            UnauthorizedAccessException => (403, "No tienes permiso para realizar esta operacion."),
            KeyNotFoundException => (404, "El registro ya no existe."),
            CineSpace.Server.Services.OperacionCineException => (409, error.Message),
            DbUpdateException { InnerException: PostgresException { SqlState: "23505" } } => (409, "El registro ya existe o una butaca acaba de ocuparse. Actualiza los datos e intentalo de nuevo."),
            DbUpdateException { InnerException: PostgresException { SqlState: "23503" or "23001" } } => (409, "El registro esta en uso y no se puede eliminar."),
            DbUpdateException { InnerException: PostgresException { SqlState: "23514" } } => (409, "Los datos no cumplen las reglas del cine. Revisa horarios, cantidades y precios."),
            _ => (500, "No pudimos completar la operacion. Intentalo de nuevo.")
        };
        if (status == 500) logger.LogError(error, "Error interno de API; TraceId {TraceId}", context.TraceIdentifier);
        context.Response.StatusCode = status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = context, ProblemDetails = new ProblemDetails { Status = status, Title = message, Extensions = { ["traceId"] = context.TraceIdentifier } }
        });
    }
}
