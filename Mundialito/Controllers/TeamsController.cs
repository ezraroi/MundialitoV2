using Mundialito.DAL.Teams;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mundialito.Models;
using Mundialito.DAL.Bets;
using Mundialito.Auth.Authorization;

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
    [Authorize(Policy = Policies.AdminOnly)]
    public Team PostTeam(TeamModel team)
    {
        var res = teamsRepository.InsertTeam(new Team
        {
            Name = team.Name,
            Flag = team.Flag,
            Logo = team.Logo,
            ShortName = team.ShortName,
            TournamentTeamId = team.TournamentTeamId,
            TeamPage = team.TeamPage,
            IntegrationsData = team.IntegrationsData
        });
        teamsRepository.Save();
        return res;
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public ActionResult<Team> PutTeam(int id, TeamModel team)
    {
        var teamToUpdate = teamsRepository.GetTeam(id);
        if (teamToUpdate == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Team with id '{0}' not found", id)});
        teamToUpdate.Name = team.Name;
        teamToUpdate.Flag = team.Flag;
        teamToUpdate.Logo = team.Logo;
        teamToUpdate.ShortName = team.ShortName;
        teamToUpdate.TournamentTeamId = team.TournamentTeamId;
        teamToUpdate.TeamPage = team.TeamPage;
        teamToUpdate.IntegrationsData = team.IntegrationsData;
        teamsRepository.Save();
        // The stored row, not the object the caller sent - those differ, and the caller's
        // copy carries no TeamId on create.
        return Ok(teamToUpdate);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
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

