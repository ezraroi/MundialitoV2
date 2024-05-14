using Mundialito.DAL.Teams;
using System.Diagnostics;
using Mundialito.DAL.Games;
using Mundialito.DAL.ActionLogs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mundialito.DAL;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class TeamsController : ControllerBase
{
    private readonly ITeamsRepository teamsRepository;
    private readonly IActionLogsRepository actionLogsRepository;

    public TeamsController(ITeamsRepository teamsRepository, IActionLogsRepository actionLogsRepository)
    {
        if (teamsRepository == null)
            throw new ArgumentNullException("teamsRepository");
        this.teamsRepository = teamsRepository;

        if (actionLogsRepository == null)
            throw new ArgumentNullException("actionLogsRepository");
        this.actionLogsRepository = actionLogsRepository;
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
            return NotFound(string.Format("Team with id '{0}' not found", id));
        return Ok(item);
    }

    [HttpGet("{id}/Games")]
    public IEnumerable<Game> GetTeamGames(int id)
    {
        return teamsRepository.GetTeamGames(id);
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
        teamsRepository.Save();
        return team;
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public void DeleteTeam(int id)
    {
        Trace.TraceInformation("Deleting Team {0}", id);
        teamsRepository.DeleteTeam(id);
        teamsRepository.Save();
    }

}

