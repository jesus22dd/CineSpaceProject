using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
namespace CineSpace.Server.Filters;
public sealed class ResultadoVacioFilter : IResultFilter
{
    public void OnResultExecuting(ResultExecutingContext context)
    {
        if (context.Result is EmptyResult && !HttpMethods.IsGet(context.HttpContext.Request.Method)) context.Result = new NoContentResult();
    }
    public void OnResultExecuted(ResultExecutedContext context) { }
}
