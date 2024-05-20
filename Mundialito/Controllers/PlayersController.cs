using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mundialito.DAL.Players;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PlayersController : ControllerBase
{
    private readonly IPlayersRepository playersRepository;
    private readonly ILogger logger;

    public PlayersController(ILogger<PlayersController> logger, IPlayersRepository playersRepository)
    {
        this.playersRepository = playersRepository;
        this.logger = logger;
    }

    [HttpGet]
    public IEnumerable<Player> GetAllPlayers()
    {
        return playersRepository.GetPlayers();
    }

}

