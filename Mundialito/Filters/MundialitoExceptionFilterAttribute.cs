using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Mundialito.DAL;

namespace Mundialito.Filters;

public class MundialitoExceptionFilterAttribute : ExceptionFilterAttribute
{
    public override void OnException(ExceptionContext context)
    {
        var status = HttpStatusCode.InternalServerError;
        if (context.Exception is NotImplementedException)
        {
            status = HttpStatusCode.NotImplemented;
        }
        else if (context.Exception is ObjectNotFoundException)
        {
            status = HttpStatusCode.NotFound;
        }
        else if (context.Exception is UnauthorizedAccessException)
        {
            status = HttpStatusCode.Forbidden;
        }
        else if (context.Exception is ArgumentException)
        {
            status = HttpStatusCode.BadRequest;
        }

        var result = new ObjectResult(new
        {
            context.Exception.Message, // Or a different generic message
            context.Exception.Source,
            ExceptionType = context.Exception.GetType().FullName,
        })
        {
            StatusCode = (int)status
        };


        context.Result = result;
    }


}
