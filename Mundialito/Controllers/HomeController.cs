using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;
using Mundialito.Models;

namespace Mundialito.Controllers;

public class HomeController : Controller
{
    private readonly Config _config;

    public HomeController(IOptions<Config> config)
    {
        _config = config.Value;
    }
    
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Index()
    {
        return View(_config);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}
