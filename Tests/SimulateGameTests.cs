using Microsoft.AspNetCore.Mvc;
using Mundialito.Controllers;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.Models;
using static Tests.BetsTestHarness;

namespace Tests;

/// <summary>
/// SimulateGame answers "what would the table look like if this game ended that way". Two
/// properties, and they pull against each other (#172 item 3):
///
/// It must not be able to persist. It used to mutate the tracked game and, through
/// BetsResolver, every bet's Points, and stay correct only because nothing else on the
/// request called Save() - one audit log call away from writing a made-up result.
///
/// It must still score. Reading untracked is what makes the first property true, but it also
/// breaks the second unless the bets are re-pointed at the simulated game: EF's identity map
/// used to make bet.Game and the simulated game one object, and UserWithPointsModel.AddBet
/// only counts a bet whose game is no longer pending a result.
///
/// The fakes model detachment by handing back copies, so both are checked here rather than
/// only in scripts/dev.sh verify, which CI does not run.
/// </summary>
[TestFixture]
public class SimulateGameTests
{
    private static Game PendingGame(int id) => new Game
    {
        GameId = id,
        Type = GameType.Groups,
        Date = Now.AddDays(-1),   // kicked off, no result entered
        HomeTeam = MakeTeam("Home"),
        AwayTeam = MakeTeam("Away"),
    };

    private static SimulateGameModel ExactMatch() =>
        new SimulateGameModel { HomeScore = 1, AwayScore = 0, CardsMark = "X", CornersMark = "1" };

    private static (GamesController controller, FakeBetsRepository bets, FakeGamesRepository games, Bet stored)
        Fixture()
    {
        var alice = MakeUser("alice");
        var game = PendingGame(100);
        // MakeBet is 1-0, cards X, corners 1 - the same result ExactMatch simulates.
        var bet = MakeBet(7, alice, game);
        var bets = new FakeBetsRepository(new[] { bet });
        var games = new FakeGamesRepository(new[] { game });
        return (MakeGamesController(bets, games, alice), bets, games, bet);
    }

    [Test]
    public void SimulateGame_ScoresTheBetsAgainstTheSimulatedResult()
    {
        var (controller, _, _, _) = Fixture();

        var table = (IEnumerable<UserWithPointsModel>)((OkObjectResult)controller.SimulateGame(100, ExactMatch()).Result!).Value!;

        var alice = table.Single(user => user.Username == "alice");
        Assert.Multiple(() =>
        {
            // Groups: 3 mark + 2 result + 1 cards + 1 corners + 2 bingo bonus.
            Assert.That(alice.Points, Is.EqualTo(9));
            Assert.That(alice.Results, Is.EqualTo(1));
        });
    }

    [Test]
    public void SimulateGame_WritesNothingToTheGameOrTheBets()
    {
        var (controller, bets, games, stored) = Fixture();

        controller.SimulateGame(100, ExactMatch());

        var game = games.GetGame(100);
        Assert.Multiple(() =>
        {
            Assert.That(game.HomeScore, Is.Null, "the simulated result reached the game");
            Assert.That(game.AwayScore, Is.Null);
            Assert.That(game.CardsMark, Is.Null);
            Assert.That(game.CornersMark, Is.Null);
            Assert.That(stored.Points, Is.Null, "the simulated points reached the bet");
            Assert.That(stored.GameMarkWin, Is.False);
            Assert.That(stored.MaxPoints, Is.False);
            Assert.That(bets.SaveCount, Is.Zero);
        });
    }

    [Test]
    public void SimulateGame_UnknownGame_ReturnsNotFound()
    {
        var (controller, _, _, _) = Fixture();
        Assert.That(controller.SimulateGame(999999, ExactMatch()).Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void SimulateGame_GameNotPendingAResult_IsRefused()
    {
        var alice = MakeUser("alice");
        var open = MakeGame(100, open: true);
        var bets = new FakeBetsRepository(Array.Empty<Bet>());
        var games = new FakeGamesRepository(new[] { open });
        var controller = MakeGamesController(bets, games, alice);

        Assert.That(controller.SimulateGame(100, ExactMatch()).Result, Is.InstanceOf<BadRequestObjectResult>());
    }
}
