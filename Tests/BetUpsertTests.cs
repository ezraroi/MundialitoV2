using Microsoft.AspNetCore.Mvc;
using Mundialito.Controllers;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.Models;
using static Tests.BetsTestHarness;

namespace Tests;

/// <summary>
/// PUT /api/games/{gameId}/mybet - the single idempotent bet write path (#170).
///
/// Two cases from the endpoint it replaces are absent on purpose rather than untested:
/// "you cannot touch another user's bet" and "you cannot move a bet to another game" have
/// no request that expresses them any more, because the body carries no bet id and no game
/// id. That is the point of the redesign - see #171 for what those checks used to allow.
/// </summary>
[TestFixture]
public class BetUpsertTests
{
    private static SaveBetModel Save(int home, int away) => new SaveBetModel
    {
        HomeScore = home,
        AwayScore = away,
        CardsMark = "2",
        CornersMark = "2",
    };

    private static (BetsController controller, FakeBetsRepository bets) Fixture(
        MundialitoUser caller, IEnumerable<Bet> existing, params Game[] games)
    {
        var bets = new FakeBetsRepository(existing);
        var repo = new FakeGamesRepository(games);
        return (MakeController(bets, repo, MakeValidator(bets, repo), caller), bets);
    }

    [Test]
    public async Task PutMyBet_NoExistingBet_Creates()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: true);
        var (controller, bets) = Fixture(alice, Array.Empty<Bet>(), game);

        var result = await controller.PutMyBet(game.GameId, Save(3, 1));

        var created = result.Result as ObjectResult;
        Assert.Multiple(() =>
        {
            Assert.That(created?.StatusCode, Is.EqualTo(201));
            Assert.That(bets.All, Has.Count.EqualTo(1));
            Assert.That(bets.All[0].UserId, Is.EqualTo(alice.Id));
            Assert.That(bets.All[0].GameId, Is.EqualTo(game.GameId));
            Assert.That(bets.All[0].HomeScore, Is.EqualTo(3));
            Assert.That(bets.All[0].AwayScore, Is.EqualTo(1));
            Assert.That(((BetViewModel)created!.Value!).HasBet, Is.True);
            Assert.That(((BetViewModel)created.Value!).Game.GameId, Is.EqualTo(game.GameId));
        });
    }

    [Test]
    public async Task PutMyBet_ExistingBet_Updates()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: true);
        var existing = MakeBet(7, alice, game);
        var (controller, bets) = Fixture(alice, new[] { existing }, game);

        var result = await controller.PutMyBet(game.GameId, Save(4, 2));

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
            Assert.That(bets.All, Has.Count.EqualTo(1));
            Assert.That(existing.BetId, Is.EqualTo(7), "the bet was replaced rather than updated");
            Assert.That(existing.HomeScore, Is.EqualTo(4));
            Assert.That(existing.AwayScore, Is.EqualTo(2));
        });
    }

    [Test]
    public async Task PutMyBet_SentTwice_IsIdempotent()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: true);
        var (controller, bets) = Fixture(alice, Array.Empty<Bet>(), game);

        var first = await controller.PutMyBet(game.GameId, Save(3, 1));
        var second = await controller.PutMyBet(game.GameId, Save(3, 1));

        Assert.Multiple(() =>
        {
            Assert.That((first.Result as ObjectResult)?.StatusCode, Is.EqualTo(201), "first save should create");
            Assert.That(second.Result, Is.InstanceOf<OkObjectResult>(), "second save should update");
            Assert.That(bets.All, Has.Count.EqualTo(1), "a double tap left two bets");
            Assert.That(((BetViewModel)((ObjectResult)second.Result!).Value!).BetId,
                Is.EqualTo(((BetViewModel)((ObjectResult)first.Result!).Value!).BetId));
        });
    }

    [Test]
    public async Task PutMyBet_ClosedGame_IsRefusedAndWritesNothing()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: false);
        var existing = MakeBet(7, alice, game);
        var (controller, bets) = Fixture(alice, new[] { existing }, game);

        var result = await controller.PutMyBet(game.GameId, Save(9, 9));

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<ConflictObjectResult>());
            Assert.That(existing.HomeScore, Is.EqualTo(1), "a refused save wrote to the bet");
            Assert.That(existing.AwayScore, Is.EqualTo(0));
            Assert.That(bets.SaveCount, Is.Zero, "a refused save reached Save()");
        });
    }

    [Test]
    public async Task PutMyBet_UnknownGame_ReturnsNotFound()
    {
        var alice = MakeUser("alice");
        var (controller, bets) = Fixture(alice, Array.Empty<Bet>(), MakeGame(100, open: true));

        var result = await controller.PutMyBet(999999, Save(1, 1));

        Assert.Multiple(() =>
        {
            Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
            Assert.That(bets.All, Is.Empty);
        });
    }

    [Test]
    public async Task GetMyBet_NoBet_ReturnsPlaceholderCarryingTheGame()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: false);
        var (controller, _) = Fixture(alice, Array.Empty<Bet>(), game);

        var view = (BetViewModel)((OkObjectResult)(await controller.GetMyBet(game.GameId)).Result!).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(view.HasBet, Is.False);
            Assert.That(view.HomeScore, Is.Null);
            // The placeholder used to hardcode this true and carry a Game stub with only an
            // id, so a long-closed game rendered as open and with no teams to draw.
            Assert.That(view.IsOpenForBetting, Is.False);
            Assert.That(view.Game.GameId, Is.EqualTo(game.GameId));
            Assert.That(view.Game.HomeTeam.Name, Is.EqualTo("Home"));
        });
    }

    [Test]
    public async Task GetMyBet_ExistingBet_ReportsHasBet()
    {
        var alice = MakeUser("alice");
        var game = MakeGame(100, open: true);
        var (controller, _) = Fixture(alice, new[] { MakeBet(7, alice, game) }, game);

        var view = (BetViewModel)((OkObjectResult)(await controller.GetMyBet(game.GameId)).Result!).Value!;

        Assert.Multiple(() =>
        {
            Assert.That(view.HasBet, Is.True);
            Assert.That(view.BetId, Is.EqualTo(7));
        });
    }

    [Test]
    public async Task GetMyBet_UnknownGame_ReturnsNotFound()
    {
        var alice = MakeUser("alice");
        var (controller, _) = Fixture(alice, Array.Empty<Bet>(), MakeGame(100, open: true));

        // It used to fabricate a bet on any id at all, including one that does not exist.
        var result = await controller.GetMyBet(999999);

        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }
}
