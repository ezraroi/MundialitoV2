using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Mundialito.Auth.Authorization;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.GeneralBets;
using Mundialito.DAL.Players;
using Mundialito.Models;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class PlayersController : ControllerBase
{
    private const string ObjectType = "Player";
    private readonly IPlayersRepository playersRepository;
    private readonly IGeneralBetsRepository generalBetsRepository;
    private readonly IActionLogger actionLogger;
    private readonly ILogger logger;

    public PlayersController(ILogger<PlayersController> logger, IPlayersRepository playersRepository, IGeneralBetsRepository generalBetsRepository, IActionLogger actionLogger)
    {
        this.playersRepository = playersRepository;
        this.generalBetsRepository = generalBetsRepository;
        this.actionLogger = actionLogger;
        this.logger = logger;
    }

    [HttpGet]
    public IEnumerable<Player> GetAllPlayers()
    {
        return playersRepository.GetPlayers();
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public Player PostPlayer(PlayerModel player)
    {
        var res = playersRepository.InsertPlayer(player.ToPlayer());
        playersRepository.Save();
        logger.LogInformation("Added Player {0}", res.PlayerId);
        actionLogger.Log(ActionType.CREATE, ObjectType, string.Format("Added player '{0}'", res.Name));
        return res;
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public IActionResult DeletePlayer(int id)
    {
        // Load before mutating: DeleteStadium and DeleteTeam hand a bad id straight to the
        // repository and surface as a 500 where 404 was meant (#172 item 4).
        var player = playersRepository.GetPlayer(id);
        if (player == null)
            return NotFound(new ErrorMessage { Message = string.Format("Player with id '{0}' not found", id) });
        // FK_GeneralBets_Players_GoldBootPlayerId cascades. Without this check the delete
        // would not fail - it would take those users' general bets with it.
        var usages = generalBetsRepository.CountGeneralBetsOnPlayer(id);
        if (usages > 0)
        {
            var message = string.Format("'{0}' is picked as golden boot by {1} general {2} and cannot be deleted", player.Name, usages, usages == 1 ? "bet" : "bets");
            actionLogger.Log(ActionType.ERROR, ObjectType, message);
            return BadRequest(new ErrorMessage { Message = message });
        }
        logger.LogInformation("Deleting Player {0}", id);
        playersRepository.DeletePlayer(id);
        playersRepository.Save();
        logger.LogInformation("Deleted Player {0}", id);
        actionLogger.Log(ActionType.DELETE, ObjectType, string.Format("Deleted player '{0}'", player.Name));
        return NoContent();
    }

}
