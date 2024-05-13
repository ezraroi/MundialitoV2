

using Microsoft.AspNetCore.Mvc;

namespace Mundialito.Controllers;

[ApiController]
[Route("[controller]")]
public class RestController : ControllerBase{

[HttpGet("hello")]
[ProducesResponseType(StatusCodes.Status200OK)]
[ProducesResponseType(StatusCodes.Status400BadRequest)]
public ActionResult<String> Hello()
{
    return CreatedAtAction("hello", "Hello!!!");
}
}