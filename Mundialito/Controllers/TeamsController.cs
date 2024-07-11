using Mundialito.DAL.Teams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mundialito.Models;
using Mundialito.DAL.Bets;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ITeamsRepository teamsRepository;
    private readonly IBetsRepository betsRepository;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly ILogger logger;


    public TeamsController(ILogger<TeamsController> logger, ITeamsRepository teamsRepository, IBetsRepository betsRepository, IHttpContextAccessor httpContextAccessor)
    {
        this.teamsRepository = teamsRepository;
        this.betsRepository = betsRepository;
        this.httpContextAccessor = httpContextAccessor;
        this.logger = logger;
    }

    [HttpGet]
    public IEnumerable<Team> GetAllTeams()
    {
        return teamsRepository.GetTeams();
    }

    [HttpGet("{id}")]
    public ActionResult<Team> GetTeamById(int id)
    {
        var item = teamsRepository.GetTeam(id);
        if (item == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Team with id '{0}' not found", id)});
        return Ok(item);
    }

    [HttpGet("{id}/Games")]
    public IEnumerable<GameViewModel> GetTeamGames(int id)
    {
        var games = teamsRepository.GetTeamGames(id);
        var res = games.Select((game) => new GameViewModel(game)).ToList();
        AddUserBetsData(res);
        return res; 
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public Team PostTeam(Team team)
    {
        var res = teamsRepository.InsertTeam(team);
        teamsRepository.Save();
        return res;
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public Team PutTeam(int id, Team team)
    {
        var teamToUpdate = teamsRepository.GetTeam(id);
        teamToUpdate.Name = team.Name;
        teamToUpdate.Flag = team.Flag;
        teamToUpdate.Logo = team.Logo;
        teamToUpdate.ShortName = team.ShortName;
        teamToUpdate.TournamentTeamId = team.TournamentTeamId;
        teamToUpdate.TeamPage = team.TeamPage;
        teamToUpdate.IntegrationsData = team.IntegrationsData;
        teamsRepository.Save();
        return team;
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public void DeleteTeam(int id)
    {
        logger.LogInformation("Deleting Team {0}", id);
        teamsRepository.DeleteTeam(id);
        teamsRepository.Save();
        logger.LogInformation("Deleted Team {0}", id);
    }

    private void AddUserBetsData(IEnumerable<GameViewModel> res)
    {
        var allBets = betsRepository.GetUserBets(httpContextAccessor.HttpContext?.User.Identity.Name).ToDictionary(bet => bet.GameId, bet => bet);
        foreach (var game in res)
        {
            game.UserHasBet = allBets.ContainsKey(game.GameId);
        }
    }

}

