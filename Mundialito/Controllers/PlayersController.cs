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

    public PlayersController(IPlayersRepository playersRepository)
    {
        if (playersRepository == null)
            throw new ArgumentNullException("playersRepository");
        this.playersRepository = playersRepository;
    }

    [HttpGet]
    public IEnumerable<Player> GetAllPlayers()
    {
        return playersRepository.GetPlayers();
    }

}

