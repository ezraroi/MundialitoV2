using Microsoft.AspNetCore.Mvc;
using Mundialito.Controllers;
using static Tests.BetsTestHarness;

namespace Tests;

/// <summary>
/// Guards the fix for the bet-privacy IDOR: bets on games that are still open for
/// betting must only be visible to their owner. Bets become public once the game closes.
/// </summary>
[TestFixture]
public class BetsControllerAuthorizationTests
{
    private static BetsController Viewing(IEnumerable<Mundialito.DAL.Bets.Bet> bets, string viewer)
    {
        var repo = new FakeBetsRepository(bets);
        return MakeController(repo, new FakeGamesRepository(Array.Empty<Mundialito.DAL.Games.Game>()),
            validator: null!, caller: null, callerName: viewer);
    }

    private static Mundialito.DAL.Bets.Bet OwnedBet(int betId, string owner, bool gameOpen) =>
        MakeBet(betId, MakeUser(owner), MakeGame(betId * 100, gameOpen));

    [Test]
    public void GetBetById_OpenGame_NonOwner_IsHidden()
    {
        var controller = Viewing(new[] { OwnedBet(1, "alice", gameOpen: true) }, "bob");
        Assert.That(controller.GetBetById(1).Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetBetById_OpenGame_Owner_IsVisible()
    {
        var controller = Viewing(new[] { OwnedBet(1, "alice", gameOpen: true) }, "alice");
        Assert.That(controller.GetBetById(1).Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public void GetBetById_ClosedGame_NonOwner_IsVisible()
    {
        var controller = Viewing(new[] { OwnedBet(1, "alice", gameOpen: false) }, "bob");
        Assert.That(controller.GetBetById(1).Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public void GetBetById_Missing_ReturnsNotFound()
    {
        var controller = Viewing(Array.Empty<Mundialito.DAL.Bets.Bet>(), "bob");
        Assert.That(controller.GetBetById(999).Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetAllBets_ExcludesOthersOpenBets_KeepsOwnAndClosed()
    {
        var bets = new[]
        {
            OwnedBet(1, "alice", gameOpen: true),   // other user's OPEN bet -> hidden
            OwnedBet(2, "alice", gameOpen: false),  // other user's CLOSED bet -> visible
            OwnedBet(3, "bob",   gameOpen: true),   // own OPEN bet -> visible
            OwnedBet(4, "bob",   gameOpen: false),  // own CLOSED bet -> visible
        };
        var controller = Viewing(bets, "bob");

        var returnedIds = controller.GetAllBets().Select(b => b.BetId).OrderBy(x => x).ToArray();

        Assert.That(returnedIds, Is.EqualTo(new[] { 2, 3, 4 }));
    }
}
