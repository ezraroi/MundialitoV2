using Microsoft.AspNetCore.Mvc;
using Mundialito.Controllers;
using Mundialito.DAL.Games;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;
using Mundialito.Models;

namespace Tests;

/// <summary>
/// The admin write endpoints load a row and start assigning to it. A bad id therefore threw
/// NullReferenceException and surfaced as a 500 where 404 was meant (#172 item 4). These are
/// the same "load, check, then mutate" order the bet path was given in #171.
/// </summary>
[TestFixture]
public class AdminWriteTests
{
    private sealed class FakeTeamsRepository : ITeamsRepository
    {
        private readonly List<Team> teams;
        public FakeTeamsRepository(IEnumerable<Team> teams) => this.teams = teams.ToList();

        public int SaveCount { get; private set; }
        public Team GetTeam(int teamId) => teams.FirstOrDefault(t => t.TeamId == teamId)!;
        public Team InsertTeam(Team team) { teams.Add(team); return team; }
        public void Save() => SaveCount++;

        public IEnumerable<Team> GetTeams() => teams;
        public IEnumerable<Game> GetTeamGames(int teamId) => throw new NotImplementedException();
        public void DeleteTeam(int teamId) => throw new NotImplementedException();
        public void UpdateTeam(Team team) => throw new NotImplementedException();
        public void Dispose() { }
    }

    private sealed class FakeStadiumsRepository : IStadiumsRepository
    {
        private readonly List<Stadium> stadiums;
        public FakeStadiumsRepository(IEnumerable<Stadium> stadiums) => this.stadiums = stadiums.ToList();

        public int SaveCount { get; private set; }
        public Stadium GetStadium(int stadiumId) => stadiums.FirstOrDefault(s => s.StadiumId == stadiumId)!;
        public Stadium InsertStadium(Stadium stadium) { stadiums.Add(stadium); return stadium; }
        public void Save() => SaveCount++;

        public IEnumerable<Stadium> GetStadiums() => stadiums;
        public void DeleteStadium(int stadiumId) => throw new NotImplementedException();
        public void UpdateStadium(Stadium stadium) => throw new NotImplementedException();
        public void Dispose() { }
    }

    private static Team ExistingTeam() => new Team
    {
        TeamId = 1, Name = "Arsenal", ShortName = "ARS", Flag = "flag.png", Logo = "logo.png",
    };

    private static TeamModel TeamBody() => new TeamModel
    {
        Name = "Renamed", ShortName = "REN", Flag = "new-flag.png", Logo = "new-logo.png",
    };

    private static Stadium ExistingStadium() =>
        new Stadium { StadiumId = 1, Name = "Emirates", City = "London", Capacity = 60000 };

    private static StadiumModel StadiumBody() =>
        new StadiumModel { Name = "Renamed", City = "Manchester", Capacity = 55000 };

    [Test]
    public void PutTeam_UnknownId_ReturnsNotFoundAndWritesNothing()
    {
        var repo = new FakeTeamsRepository(new[] { ExistingTeam() });
        var controller = new TeamsController(logger: null!, teamsRepository: repo, betsRepository: null!, httpContextAccessor: null!);

        var result = controller.PutTeam(999999, TeamBody());

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
            Assert.That(repo.SaveCount, Is.Zero);
        });
    }

    [Test]
    public void PutTeam_ReturnsTheStoredRowRatherThanTheRequest()
    {
        var stored = ExistingTeam();
        var repo = new FakeTeamsRepository(new[] { stored });
        var controller = new TeamsController(logger: null!, teamsRepository: repo, betsRepository: null!, httpContextAccessor: null!);

        var team = (Team)((OkObjectResult)controller.PutTeam(1, TeamBody()).Result!).Value!;

        Assert.Multiple(() =>
        {
            // It used to echo the request body back, which carries no id at all now.
            Assert.That(team.TeamId, Is.EqualTo(1));
            Assert.That(team.Name, Is.EqualTo("Renamed"));
            Assert.That(stored.Name, Is.EqualTo("Renamed"));
            Assert.That(repo.SaveCount, Is.EqualTo(1));
        });
    }

    [Test]
    public void PostTeam_IgnoresAnythingButTheWritableFields()
    {
        var repo = new FakeTeamsRepository(Array.Empty<Team>());
        var controller = new TeamsController(logger: null!, teamsRepository: repo, betsRepository: null!, httpContextAccessor: null!);

        var created = controller.PostTeam(TeamBody());

        Assert.Multiple(() =>
        {
            // TeamModel has no TeamId to copy across, so a caller cannot choose the key.
            Assert.That(created.TeamId, Is.Zero);
            Assert.That(created.Name, Is.EqualTo("Renamed"));
            Assert.That(created.HomeMatches, Is.Null);
            Assert.That(created.AwayMatches, Is.Null);
        });
    }

    [Test]
    public void PutStadium_UnknownId_ReturnsNotFoundAndWritesNothing()
    {
        var repo = new FakeStadiumsRepository(new[] { ExistingStadium() });
        var controller = new StadiumsController(logger: null!, stadiumsRepository: repo);

        var result = controller.PutStadium(999999, StadiumBody());

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
            Assert.That(repo.SaveCount, Is.Zero);
        });
    }

    [Test]
    public void PutStadium_ReturnsTheStoredRowRatherThanTheRequest()
    {
        var repo = new FakeStadiumsRepository(new[] { ExistingStadium() });
        var controller = new StadiumsController(logger: null!, stadiumsRepository: repo);

        var stadium = (Stadium)((OkObjectResult)controller.PutStadium(1, StadiumBody()).Result!).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(stadium.StadiumId, Is.EqualTo(1));
            Assert.That(stadium.Name, Is.EqualTo("Renamed"));
            Assert.That(repo.SaveCount, Is.EqualTo(1));
        });
    }
}
