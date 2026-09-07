using Mundialito.DAL.Games;
using Mundialito.Models;
using Mundialito.DAL.Bets;
using Mundialito.Logic;
using Mundialito.DAL.ActionLogs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mundialito.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.GeneralBets;
using Mundialito.Auth.Authorization;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GamesController : ControllerBase
{
    private const string ObjectType = "Game";
    private readonly IGamesRepository gamesRepository;
    private readonly IBetsRepository betsRepository;
    private readonly IBetsResolver betsResolver;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogger actionLogger;
    private readonly Config config;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UserManager<MundialitoUser> userManager;
    private readonly TableBuilder tableBuilder;
    private readonly IGeneralBetsRepository generalBetsRepository;
    private readonly ILogger logger;

    public GamesController(ILogger<GamesController> logger, IGamesRepository gamesRepository, IBetsRepository betsRepository, IBetsResolver betsResolver, IDateTimeProvider dateTimeProvider, IActionLogger actionLogger, IOptions<Config> config, IHttpContextAccessor httpContextAccessor, UserManager<MundialitoUser> userManager, TableBuilder tableBuilder, IGeneralBetsRepository generalBetsRepository)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.config = config.Value;
        this.gamesRepository = gamesRepository;
        this.betsRepository = betsRepository;
        this.betsResolver = betsResolver;
        this.dateTimeProvider = dateTimeProvider;
        this.dateTimeProvider = dateTimeProvider;
        this.actionLogger = actionLogger;
        this.userManager = userManager;
        this.tableBuilder = tableBuilder;
        this.generalBetsRepository = generalBetsRepository;
        this.logger = logger;
    }

    [HttpGet]
    public IEnumerable<GameViewModel> GetAllGames()
    {
        var res = gamesRepository.GetGames().Select(game => new GameViewModel(game)).ToList();
        AddUserBetsData(res);
        return res;
    }

    [HttpGet("Stadium/{stadiumId}")]
    public IEnumerable<GameViewModel> GetStadiumGames(int stadiumId)
    {
        var res = gamesRepository.GetStadiumGames(stadiumId).Select(game => new GameViewModel(game)).ToList();
        AddUserBetsData(res);
        return res;
    }

    [HttpGet("{id}")]
    public ActionResult<GameViewModel> GetGameByID(int id)
    {
        var item = gamesRepository.GetGame(id);
        if (item == null)
            return NotFound(new ErrorMessage { Message = string.Format("Game with id '{0}' not found", id) });

        var res = new GameViewModel(item);
        res.UserHasBet = betsRepository.GetUserBetOnGame(httpContextAccessor.HttpContext?.User.Identity.Name, id) != null;
        return Ok(res);
    }

    /// <summary>
    /// What the table would look like if this game ended that way. A simulation must not be
    /// able to reach the database, so everything it touches is read detached and mutated in
    /// memory. It used to work on tracked entities and simply never call Save(), which made
    /// it correct only for as long as nothing else in the request saved - a single audit log
    /// call on this path would have written a made-up result and every recalculated point.
    /// </summary>
    [HttpPost("{id}/simulate")]
    [Authorize(Policy = Policies.AdminOnly)]
    public ActionResult<IEnumerable<UserModel>> SimulateGame(int id, SimulateGameModel simulateGameModel)
    {
        var item = gamesRepository.GetGameNoTracking(id);
        if (item == null)
            return NotFound(new ErrorMessage { Message = string.Format("Game with id '{0}' not found", id) });
        if (!item.IsPendingUpdate(dateTimeProvider.UTCNow))
            return BadRequest(new ErrorMessage { Message = string.Format("Game with id '{0}' is not in pending update state", id) });
        if (simulateGameModel.HomeScore == null || simulateGameModel.AwayScore == null)
            return BadRequest(new ErrorMessage { Message = "HomeScore and AwayScore must be provided" });
        if (simulateGameModel.CornersMark == null || simulateGameModel.CardsMark == null)
            return BadRequest(new ErrorMessage { Message = "CornersMark and CardsMark must be provided" });
        logger.LogInformation("Simulating game {} with {}", id, simulateGameModel);
        var bets = betsRepository.GetBetsNoTracking().ToList();
        item.AwayScore = simulateGameModel.AwayScore;
        item.HomeScore = simulateGameModel.HomeScore;
        item.CardsMark = simulateGameModel.CardsMark;
        item.CornersMark = simulateGameModel.CornersMark;
        var gameBets = bets.Where(bet => bet.GameId == id).ToList();
        // With tracked reads, EF's identity map made bet.Game and the simulated game one
        // object, so the table saw the simulated result through both. Detached they are two,
        // and the table would score the game as still unplayed - so say it explicitly.
        gameBets.ForEach(bet => bet.Game = item);
        betsResolver.ResolveBets(item, gameBets);
        return Ok(tableBuilder.GetTable(userManager.Users.Select((user) => new UserWithPointsModel(user)), bets, generalBetsRepository.GetGeneralBets()));
    }

    [HttpGet("{id}/Bets")]
    public ActionResult<IEnumerable<BetViewModel>> GetGameBets(int id)
    {
        var game = gamesRepository.GetGame(id);
        if (game == null)
            return NotFound(new ErrorMessage { Message = string.Format("Game with id '{0}' not found", id) });
        if (game.IsOpen(dateTimeProvider.UTCNow))
            return BadRequest(new ErrorMessage { Message = string.Format("Game '{0}' is stil open for betting", id) });
        return Ok(betsRepository.GetGameBets(id).Select(item => new BetViewModel(item, dateTimeProvider.UTCNow)).OrderByDescending(bet => bet.Points));
    }

    [HttpGet("Open")]
    public IEnumerable<GameViewModel> GetOpenGames()
    {
        var res = gamesRepository.GetGames().Where(game => game.IsOpen(dateTimeProvider.UTCNow)).Select(game => new GameViewModel(game)).ToList();
        AddUserBetsData(res);
        return res;
    }

    [HttpPost]
    [Authorize(Policy = Policies.AdminOnly)]
    public ActionResult<NewGameModel> PostGame(NewGameModel game)
    {
        if (game.AwayTeam.TeamId == game.HomeTeam.TeamId)
        {
            return BadRequest(new ErrorMessage { Message = "Home team and Away team can not be the same team" });
        }
        var newGame = new Game
        {
            HomeTeamId = game.HomeTeam.TeamId,
            AwayTeamId = game.AwayTeam.TeamId,
            StadiumId = game.Stadium.StadiumId,
            Date = GameDateTime.ToUtc(game.Date),
            IntegrationsData = game.IntegrationsData,
            Type = game.Type
        };
        var res = gamesRepository.InsertGame(newGame);
        gamesRepository.Save();
        logger.LogInformation("Posting new Game: {0}", game);
        game.GameId = res.GameId;
        game.IsOpen = true;
        game.IsPendingUpdate = false;
        actionLogger.Log(ActionType.CREATE, ObjectType, string.Format("Posting new game: {0}", newGame));
        AddMonkeyBet(res);
        return game;
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public ActionResult<PutGameModelResult> PutGame(int id, PutGameModel game)
    {
        var item = gamesRepository.GetGame(id);
        if (item == null)
            return NotFound(new ErrorMessage { Message = string.Format("No such game with id '{0}'", id) });

        if (item.IsOpen(dateTimeProvider.UTCNow) && (game.HomeScore != null || game.AwayScore != null || game.CornersMark != null || game.CardsMark != null))
            return BadRequest(new ErrorMessage { Message = "Open game can not be updated with results" });
        logger.LogInformation("Resolving bet {} with {}", id, game);
        item.AwayScore = game.AwayScore;
        item.HomeScore = game.HomeScore;
        item.CardsMark = game.CardsMark;
        item.CornersMark = game.CornersMark;
        item.Date = GameDateTime.ToUtc(game.Date);
        item.IntegrationsData = game.IntegrationsData;
        item.Type = game.Type;
        gamesRepository.Save();
        if (item.IsBetResolved(dateTimeProvider.UTCNow))
        {
            actionLogger.Log(ActionType.UPDATE, ObjectType, string.Format("Will resolve bets of game {0}", item.GameId));
            logger.LogInformation("Will reoslve Game {0} bets", id);
            var bets = betsRepository.GetGameBets(item.GameId);
            betsResolver.ResolveBets(item, bets);
            if (bets.Count() > 0)
            {
                betsRepository.Save();
            }
        }
        actionLogger.Log(ActionType.UPDATE, ObjectType, string.Format("Updating Game {0}", item));
        logger.LogInformation("Bet {} was resolved", id);
        return Ok(new PutGameModelResult(item, dateTimeProvider.UTCNow));
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public IActionResult DeleteGame(int id)
    {
        var game = gamesRepository.GetGame(id);
        if (game == null)
            return NotFound(new ErrorMessage { Message = string.Format("No such game with id '{0}'", id) });
        logger.LogInformation("Deleting Game {0}", id);
        gamesRepository.DeleteGame(id);
        gamesRepository.Save();
        actionLogger.Log(ActionType.DELETE, ObjectType, String.Format("Deleting Game {0}", id));
        logger.LogInformation("Game {0} was deleted", id);
        return Ok();
    }

    private void AddUserBetsData(IEnumerable<GameViewModel> res)
    {
        var allBets = betsRepository.GetUserBets(httpContextAccessor.HttpContext?.User.Identity.Name).ToDictionary(bet => bet.GameId, bet => bet);
        foreach (var game in res)
        {
            game.UserHasBet = allBets.ContainsKey(game.GameId);
        }
    }

    private void AddMonkeyBet(Game res)
    {
        var monkeyUserName = config.MonkeyUserName;
        if (string.IsNullOrEmpty(monkeyUserName))
        {
            return;
        }
        // Adding the monkey bet is a best-effort side effect of creating a game; a failure here
        // (missing monkey, DB issue) must never break game creation, so it is logged and swallowed.
        try
        {
            var monkeyUser = userManager.Users.FirstOrDefault(user => user.UserName == monkeyUserName);
            if (monkeyUser == null)
            {
                logger.LogError("Monkey user {0} was not found, will not add monkey bet", monkeyUserName);
                return;
            }
            logger.LogInformation("Adding monkey user bet");
            var randomResults = new RandomResults();
            var result = randomResults.GetRandomResult();
            betsRepository.InsertBet(new Bet()
            {
                GameId = res.GameId,
                UserId = monkeyUser.Id,
                HomeScore = result.Key,
                AwayScore = result.Value,
                CardsMark = randomResults.GetRandomMark(),
                CornersMark = randomResults.GetRandomMark()
            });
            betsRepository.Save();
            logger.LogInformation("Monkey bet was saved");
        }
        catch (Exception e)
        {
            logger.LogError("Failed to add monkey bet for game {0}: {1}", res.GameId, e.Message);
        }
    }
}

