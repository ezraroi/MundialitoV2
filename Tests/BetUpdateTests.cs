using Microsoft.AspNetCore.Mvc;
using Mundialito.Models;
using static Tests.BetsTestHarness;

namespace Tests;

/// <summary>
/// Guards the three defects in #171: a rejected update must not reach the database, the
/// ownership check must actually fire, and a bet's game must never come from the request.
/// The controller is driven directly with the real <see cref="Mundialito.Logic.BetValidator"/>
/// over list backed fakes - the fake hands back the same instance every read, exactly as
/// EF's identity map does, which is what made the aliasing bug reproducible without Postgres.
/// </summary>
[TestFixture]
public class BetUpdateTests
{
    private static UpdateBetModel Requested(int gameId) => new UpdateBetModel
    {
        GameId = gameId,
        HomeScore = 9,
        AwayScore = 9,
        CardsMark = "2",
        CornersMark = "2",
    };

    [Test]
    public async Task UpdateBet_OtherUsersBet_IsRejected()
    {
        var alice = MakeUser("alice");
        var bob = MakeUser("bob");
        var game = MakeGame(100, open: true);
        var bet = MakeBet(1, alice, game);
        var bets = new FakeBetsRepository(new[] { bet });
        var games = new FakeGamesRepository(new[] { game });
        var controller = MakeController(bets, games, MakeValidator(bets, games), bob);

        var result = await controller.UpdateBet(1, Requested(game.GameId));

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<UnauthorizedObjectResult>());
            Assert.That(bet.UserId, Is.EqualTo(alice.Id), "the bet changed hands");
            Assert.That(bet.HomeScore, Is.EqualTo(1));
            Assert.That(bet.AwayScore, Is.EqualTo(0));
            Assert.That(bets.SaveCount, Is.Zero);
        });
    }

    [Test]
    public async Task RejectedUpdate_LeavesTheBetUntouched()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: false);
        var bet = MakeBet(1, alice, game);
        var bets = new FakeBetsRepository(new[] { bet });
        var games = new FakeGamesRepository(new[] { game });
        var controller = MakeController(bets, games, MakeValidator(bets, games), alice);

        var result = await controller.UpdateBet(1, Requested(game.GameId));

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>());
            Assert.That(bet.HomeScore, Is.EqualTo(1), "a rejected update wrote to the bet");
            Assert.That(bet.AwayScore, Is.EqualTo(0));
            Assert.That(bet.CardsMark, Is.EqualTo("X"));
            Assert.That(bet.CornersMark, Is.EqualTo("1"));
            Assert.That(bets.SaveCount, Is.Zero, "a rejected update reached Save()");
        });
    }

    [Test]
    public async Task UpdateBet_IgnoresClientSuppliedGameId()
    {
        var alice = MakeUser("alice");
        var closed = MakeGame(100, open: false);
        var open = MakeGame(200, open: true);
        var bet = MakeBet(1, alice, closed);
        var bets = new FakeBetsRepository(new[] { bet });
        var games = new FakeGamesRepository(new[] { closed, open });
        var controller = MakeController(bets, games, MakeValidator(bets, games), alice);

        // The bet's own game is closed; the request points at an open one.
        var result = await controller.UpdateBet(1, Requested(open.GameId));

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<BadRequestObjectResult>(),
                "the deadline was checked against the game the client asked for");
            Assert.That(bet.GameId, Is.EqualTo(closed.GameId), "the bet moved games");
        });
    }

    [Test]
    public async Task UpdateBet_MissingBet_ReturnsNotFound()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: true);
        var bets = new FakeBetsRepository(Array.Empty<Mundialito.DAL.Bets.Bet>());
        var games = new FakeGamesRepository(new[] { game });
        var controller = MakeController(bets, games, MakeValidator(bets, games), alice);

        var result = await controller.UpdateBet(999, Requested(game.GameId));

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }
}
