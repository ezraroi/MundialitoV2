using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;
using Mundialito.Controllers;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.DAL.Teams;
using Mundialito.Logic;

namespace Tests;

/// <summary>
/// Guards the fix for the bet-privacy IDOR: bets on games that are still open for
/// betting must only be visible to their owner. Bets become public once the game closes.
/// </summary>
[TestFixture]
public class BetsControllerAuthorizationTests
{
    private static readonly DateTime Now = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    private sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UTCNow => Now;
    }

    private sealed class FakeBetsRepository : IBetsRepository
    {
        private readonly List<Bet> bets;
        public FakeBetsRepository(IEnumerable<Bet> bets) => this.bets = bets.ToList();

        public IEnumerable<Bet> GetBets() => bets;
        public Bet GetBet(int betId) => bets.FirstOrDefault(b => b.BetId == betId);

        public IEnumerable<Bet> GetUserBets(string username) => throw new NotImplementedException();
        public Bet GetUserBetOnGame(string username, int gameId) => throw new NotImplementedException();
        public IEnumerable<Bet> GetGameBets(int gameId) => throw new NotImplementedException();
        public Bet InsertBet(Bet bet) => throw new NotImplementedException();
        public void DeleteBet(int betId) => throw new NotImplementedException();
        public void UpdateBet(Bet bet) => throw new NotImplementedException();
        public void Save() => throw new NotImplementedException();
    }

    private static Team MakeTeam(string name) =>
        new Team { Name = name, Flag = "flag.png", Logo = "logo.png", ShortName = "AAA" };

    // CloseTime = Date - 15min; a game is open while Now < CloseTime.
    private static Game MakeGame(int id, bool open) => new Game
    {
        GameId = id,
        Type = GameType.Groups,
        Date = open ? Now.AddDays(1) : Now.AddDays(-1),
        HomeTeam = MakeTeam("Home"),
        AwayTeam = MakeTeam("Away"),
    };

    private static Bet MakeBet(int betId, string ownerUserName, bool gameOpen) => new Bet
    {
        BetId = betId,
        UserId = ownerUserName,
        User = new MundialitoUser { UserName = ownerUserName, FirstName = "F", LastName = "L" },
        GameId = betId * 100,
        Game = MakeGame(betId * 100, gameOpen),
        HomeScore = 1,
        AwayScore = 0,
        CornersMark = "1",
        CardsMark = "X",
        MaxPoints = false,
    };

    private static BetsController MakeController(IBetsRepository repo, string currentUserName)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(
                new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, currentUserName) }, "TestAuth"))
        };
        return new BetsController(
            logger: null!,
            betsRepository: repo,
            betValidator: null!,
            dateTimeProvider: new FixedClock(),
            actionLogsRepository: null!,
            gamesRepository: null!,
            userManager: null!,
            httpContextAccessor: new HttpContextAccessor { HttpContext = httpContext },
            config: Options.Create(new Config()),
            emailSender: null!);
    }

    [Test]
    public void GetBetById_OpenGame_NonOwner_IsHidden()
    {
        var controller = MakeController(new FakeBetsRepository(new[] { MakeBet(1, "alice", gameOpen: true) }), "bob");
        var result = controller.GetBetById(1);
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetBetById_OpenGame_Owner_IsVisible()
    {
        var controller = MakeController(new FakeBetsRepository(new[] { MakeBet(1, "alice", gameOpen: true) }), "alice");
        var result = controller.GetBetById(1);
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public void GetBetById_ClosedGame_NonOwner_IsVisible()
    {
        var controller = MakeController(new FakeBetsRepository(new[] { MakeBet(1, "alice", gameOpen: false) }), "bob");
        var result = controller.GetBetById(1);
        Assert.That(result.Result, Is.InstanceOf<OkObjectResult>());
    }

    [Test]
    public void GetBetById_Missing_ReturnsNotFound()
    {
        var controller = MakeController(new FakeBetsRepository(Array.Empty<Bet>()), "bob");
        var result = controller.GetBetById(999);
        Assert.That(result.Result, Is.InstanceOf<NotFoundObjectResult>());
    }

    [Test]
    public void GetAllBets_ExcludesOthersOpenBets_KeepsOwnAndClosed()
    {
        var bets = new[]
        {
            MakeBet(1, "alice", gameOpen: true),   // other user's OPEN bet -> hidden
            MakeBet(2, "alice", gameOpen: false),  // other user's CLOSED bet -> visible
            MakeBet(3, "bob",   gameOpen: true),   // own OPEN bet -> visible
            MakeBet(4, "bob",   gameOpen: false),  // own CLOSED bet -> visible
        };
        var controller = MakeController(new FakeBetsRepository(bets), "bob");

        var returnedIds = controller.GetAllBets().Select(b => b.BetId).OrderBy(x => x).ToArray();

        Assert.That(returnedIds, Is.EqualTo(new[] { 2, 3, 4 }));
    }
}
