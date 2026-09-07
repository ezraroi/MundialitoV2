using System.Security.Claims;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Mundialito.Configuration;
using Mundialito.Controllers;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.ActionLogs;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.DAL.Teams;
using Mundialito.Logic;

namespace Tests;

/// <summary>
/// Hand rolled fakes and entity builders shared by the bet controller tests. The house
/// style is fakes rather than a mocking framework, and there is no test database - see
/// the note on <see cref="FakeBetsRepository"/> for why that is enough here.
/// </summary>
public static class BetsTestHarness
{
    public static readonly DateTime Now = new DateTime(2026, 7, 2, 12, 0, 0, DateTimeKind.Utc);

    public sealed class FixedClock : IDateTimeProvider
    {
        public DateTime UTCNow => Now;
    }

    /// <summary>
    /// Backed by a plain <see cref="List{T}"/>, so every read of a bet hands back the same
    /// instance - exactly how EF's identity map behaves, and the reason the aliasing bug in
    /// #171 is reproducible without Postgres. It also makes a premature mutation immediately
    /// visible in the list, which is strictly stronger than what a real SaveChanges would show.
    /// </summary>
    public sealed class FakeBetsRepository : IBetsRepository
    {
        private readonly List<Bet> bets;
        private int nextBetId;

        public FakeBetsRepository(IEnumerable<Bet> bets)
        {
            this.bets = bets.ToList();
            nextBetId = this.bets.Count == 0 ? 1 : this.bets.Max(b => b.BetId) + 1;
        }

        public int SaveCount { get; private set; }
        public IReadOnlyList<Bet> All => bets;

        public IEnumerable<Bet> GetBets() => bets;
        public Bet GetBet(int betId) => bets.FirstOrDefault(b => b.BetId == betId)!;
        public IEnumerable<Bet> GetGameBets(int gameId) => bets.Where(b => b.GameId == gameId);
        public Bet GetUserBetOnGame(string username, int gameId) =>
            bets.SingleOrDefault(b => b.User.UserName == username && b.GameId == gameId)!;

        public Bet InsertBet(Bet bet)
        {
            bet.BetId = nextBetId++;
            bets.Add(bet);
            return bet;
        }

        public void Save() => SaveCount++;

        public IEnumerable<Bet> GetUserBets(string username) => throw new NotImplementedException();
        public void DeleteBet(int betId) => throw new NotImplementedException();
        public void UpdateBet(Bet bet) => throw new NotImplementedException();
    }

    public sealed class FakeGamesRepository : IGamesRepository
    {
        private readonly List<Game> games;
        public FakeGamesRepository(IEnumerable<Game> games) => this.games = games.ToList();

        public Game GetGame(int gameId) => games.FirstOrDefault(g => g.GameId == gameId)!;
        public IEnumerable<Game> GetGames() => games;

        public Game InsertGame(Game game) => throw new NotImplementedException();
        public void DeleteGame(int gameId) => throw new NotImplementedException();
        public void UpdateGame(Game game) => throw new NotImplementedException();
        public IEnumerable<Game> GetStadiumGames(int id) => throw new NotImplementedException();
        public void Save() => throw new NotImplementedException();
        public void Dispose() { }
    }

    /// <summary>Records what was logged so a test can assert the audit trail survived the
    /// move out of <see cref="BetValidator"/>. Nothing here can reach a business repository,
    /// which is the property the real ActionLogger buys with its own scope.</summary>
    public sealed class FakeActionLogger : IActionLogger
    {
        private readonly List<(ActionType Type, string ObjectType, string Message)> entries = new();
        public IReadOnlyList<(ActionType Type, string ObjectType, string Message)> Entries => entries;

        public void Log(ActionType actionType, string objectType, string message) =>
            entries.Add((actionType, objectType, message));
    }

    private sealed class FakeUserStore : IUserStore<MundialitoUser>
    {
        public void Dispose() { }
        public Task<string> GetUserIdAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task<string?> GetUserNameAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task SetUserNameAsync(MundialitoUser user, string? userName, CancellationToken t) => throw new NotImplementedException();
        public Task<string?> GetNormalizedUserNameAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task SetNormalizedUserNameAsync(MundialitoUser user, string? normalizedName, CancellationToken t) => throw new NotImplementedException();
        public Task<IdentityResult> CreateAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task<IdentityResult> UpdateAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task<IdentityResult> DeleteAsync(MundialitoUser user, CancellationToken t) => throw new NotImplementedException();
        public Task<MundialitoUser?> FindByIdAsync(string userId, CancellationToken t) => throw new NotImplementedException();
        public Task<MundialitoUser?> FindByNameAsync(string normalizedUserName, CancellationToken t) => throw new NotImplementedException();
    }

    /// <summary>
    /// The controller only ever calls <c>FindByNameAsync</c>, which is virtual, and
    /// <c>UserManager</c>'s constructor null guards only its store. That is the whole reason
    /// the bet write actions had no unit test before - the old fixture passed
    /// <c>userManager: null!</c> and could only exercise actions that never touched it.
    /// </summary>
    public sealed class FakeUserManager : UserManager<MundialitoUser>
    {
        private readonly MundialitoUser? user;

        public FakeUserManager(MundialitoUser? user)
            : base(new FakeUserStore(), null!, null!, null!, null!, null!, null!, null!, null!) => this.user = user;

        public override Task<MundialitoUser?> FindByNameAsync(string userName) => Task.FromResult(user);
    }

    public static MundialitoUser MakeUser(string userName) =>
        new MundialitoUser { Id = userName + "-id", UserName = userName, FirstName = "F", LastName = "L" };

    public static Team MakeTeam(string name) =>
        new Team { Name = name, Flag = "flag.png", Logo = "logo.png", ShortName = "AAA" };

    // CloseTime = Date - 15min; a game is open while Now < CloseTime.
    public static Game MakeGame(int id, bool open) => new Game
    {
        GameId = id,
        Type = GameType.Groups,
        Date = open ? Now.AddDays(1) : Now.AddDays(-1),
        HomeTeam = MakeTeam("Home"),
        AwayTeam = MakeTeam("Away"),
    };

    public static Bet MakeBet(int betId, MundialitoUser owner, Game game) => new Bet
    {
        BetId = betId,
        UserId = owner.Id,
        User = owner,
        GameId = game.GameId,
        Game = game,
        HomeScore = 1,
        AwayScore = 0,
        CornersMark = "1",
        CardsMark = "X",
        MaxPoints = false,
    };

    public static BetValidator MakeValidator(IBetsRepository bets, IGamesRepository games) =>
        new BetValidator(games, bets, new FixedClock());

    public static BetsController MakeController(
        IBetsRepository bets,
        IGamesRepository games,
        IBetValidator validator,
        MundialitoUser? caller,
        IActionLogger? actionLogger = null,
        string? callerName = null)
    {
        var httpContext = new DefaultHttpContext
        {
            User = new ClaimsPrincipal(new ClaimsIdentity(
                new[] { new Claim(ClaimTypes.Name, callerName ?? caller?.UserName ?? "anonymous") }, "TestAuth"))
        };
        return new BetsController(
            logger: NullLogger<BetsController>(),
            betsRepository: bets,
            betValidator: validator,
            dateTimeProvider: new FixedClock(),
            actionLogger: actionLogger ?? new FakeActionLogger(),
            gamesRepository: games,
            userManager: new FakeUserManager(caller),
            httpContextAccessor: new HttpContextAccessor { HttpContext = httpContext },
            config: Options.Create(new Config()),
            emailSender: null!);
    }

    private static ILogger<T> NullLogger<T>() => new NoopLogger<T>();

    private sealed class NoopLogger<T> : ILogger<T>
    {
        public IDisposable? BeginScope<TState>(TState state) where TState : notnull => null;
        public bool IsEnabled(LogLevel logLevel) => false;
        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception,
            Func<TState, Exception?, string> formatter) { }
    }
}
