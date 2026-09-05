using Mundialito.DAL.DBCreators;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;

namespace Tests;

/// <summary>
/// DatabaseInitilaizer resolves the creator by reflection from App:TournamentDBCreatorName and
/// silently skips seeding when the lookup fails, so the type name and the by-name entity lookups
/// in GetGames are covered here rather than discovered on an empty production database.
/// </summary>
public class ChampionsLeague2027Tests
{
    private const string ConfiguredName = "ChampionsLeague2027";

    private static ITournamentCreator CreateByReflection()
    {
        var type = typeof(ITournamentCreator).Assembly.GetType("Mundialito.DAL.DBCreators." + ConfiguredName);
        Assert.That(type, Is.Not.Null, "App:TournamentDBCreatorName would not resolve");
        var creator = Activator.CreateInstance(type!) as ITournamentCreator;
        Assert.That(creator, Is.Not.Null);
        return creator!;
    }

    [Test]
    public void Teams_Are36_WithUniqueThreeLetterShortNames()
    {
        var teams = CreateByReflection().GetTeams();
        Assert.That(teams, Has.Count.EqualTo(36));
        Assert.That(teams.Select(team => team.Name).Distinct().Count(), Is.EqualTo(36));
        Assert.That(teams.Select(team => team.ShortName).Distinct().Count(), Is.EqualTo(36));
        Assert.That(teams.All(team => team.ShortName.Length == 3), Is.True);
    }

    [Test]
    public void TeamImages_AreLowerCase()
    {
        // Client/src/Teams/Team.js lower-cases Flag and Logo before rendering them.
        var teams = CreateByReflection().GetTeams();
        Assert.That(teams.All(team => team.Flag == team.Flag.ToLowerInvariant()), Is.True);
        Assert.That(teams.All(team => team.Logo == team.Logo.ToLowerInvariant()), Is.True);
    }

    [Test]
    public void Games_Are36_AndResolveAgainstSeededTeamsAndStadiums()
    {
        var creator = CreateByReflection();

        var id = 1;
        var teams = creator.GetTeams().ToDictionary(team => team.Name, team =>
        {
            team.TeamId = id++;
            return team;
        });
        var stadiums = creator.GetStadiums().ToDictionary(stadium => stadium.Name, stadium =>
        {
            stadium.StadiumId = id++;
            return stadium;
        });

        var games = creator.GetGames(stadiums, teams);

        Assert.That(games, Has.Count.EqualTo(36));
        Assert.That(games.All(game => game.HomeTeamId > 0 && game.AwayTeamId > 0 && game.StadiumId > 0), Is.True);
        Assert.That(games.All(game => game.HomeTeamId != game.AwayTeamId), Is.True);
        Assert.That(games.All(game => game.Date.Kind == DateTimeKind.Utc), Is.True);
    }

    [Test]
    public void Games_CoverEveryTeamOncePerMatchday()
    {
        var creator = CreateByReflection();
        var id = 1;
        var teams = creator.GetTeams().ToDictionary(team => team.Name, team =>
        {
            team.TeamId = id++;
            return team;
        });
        var stadiums = creator.GetStadiums().ToDictionary(stadium => stadium.Name, stadium =>
        {
            stadium.StadiumId = id++;
            return stadium;
        });

        var games = creator.GetGames(stadiums, teams);

        var matchday1 = games.Where(game => game.Date < new DateTime(2026, 10, 1, 0, 0, 0, DateTimeKind.Utc)).ToList();
        var matchday2 = games.Except(matchday1).ToList();
        Assert.That(matchday1, Has.Count.EqualTo(18));
        Assert.That(matchday2, Has.Count.EqualTo(18));

        foreach (var matchday in new[] { matchday1, matchday2 })
        {
            var appearances = matchday.SelectMany(game => new[] { game.HomeTeamId, game.AwayTeamId }).ToList();
            Assert.That(appearances.Distinct().Count(), Is.EqualTo(36));
        }
    }

    [Test]
    public void FirstKickoff_MatchesUefaSchedule()
    {
        // UEFA has AEK Athens v LASK at 2026-09-08T16:45:00Z, the earliest game of matchday 1.
        var creator = CreateByReflection();
        var id = 1;
        var teams = creator.GetTeams().ToDictionary(team => team.Name, team =>
        {
            team.TeamId = id++;
            return team;
        });
        var stadiums = creator.GetStadiums().ToDictionary(stadium => stadium.Name, stadium =>
        {
            stadium.StadiumId = id++;
            return stadium;
        });

        var first = creator.GetGames(stadiums, teams).Min(game => game.Date);

        Assert.That(first, Is.EqualTo(new DateTime(2026, 9, 8, 16, 45, 0, DateTimeKind.Utc)));
    }

    [Test]
    public void Stadiums_AreASingleSharedVenue()
    {
        Assert.That(CreateByReflection().GetStadiums(), Has.Count.EqualTo(1));
    }

    [Test]
    public void Players_IncludeAnOtherOption()
    {
        var players = CreateByReflection().GetPlayers();
        Assert.That(players, Is.Not.Empty);
        Assert.That(players.Select(player => player.Name), Contains.Item("Other"));
    }
}
