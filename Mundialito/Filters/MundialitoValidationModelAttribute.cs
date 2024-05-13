using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Mundialito.Filters;

public class MundialitoValidationModelAttribute : ActionFilterAttribute
{

    public override void OnActionExecuting(ActionExecutingContext actionContext)
    {
        if (actionContext.ModelState.IsValid == false)
        {

            var result = new ObjectResult(new
            {
                actionContext.ModelState
            })
            {
                StatusCode = (int)HttpStatusCode.BadRequest
            };
            actionContext.Result = result;
        }
    }

}
