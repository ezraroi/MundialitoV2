using Mundialito.DAL;
using Mundialito.DAL.Games;
using Mundialito.Models;
using Mundialito.DAL.Bets;
using System.Diagnostics;
using Mundialito.Logic;
using Mundialito.DAL.ActionLogs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mundialito.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.AspNetCore.Identity;
using Mundialito.DAL.Accounts;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class GamesController : ControllerBase
{
    private const String ObjectType = "Game";
    private readonly IGamesRepository gamesRepository;
    private readonly IBetsRepository betsRepository;
    private readonly IBetsResolver betsResolver;
    private readonly IDateTimeProvider dateTimeProvider;
    private readonly IActionLogsRepository actionLogsRepository;
    private readonly Config config;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly UserManager<MundialitoUser> userManager;

    public GamesController(IGamesRepository gamesRepository, IBetsRepository betsRepository, IBetsResolver betsResolver, IDateTimeProvider dateTimeProvider, IActionLogsRepository actionLogsRepository, IOptions<Config> config, IHttpContextAccessor httpContextAccessor, UserManager<MundialitoUser> userManager)
    {
        this.httpContextAccessor = httpContextAccessor;
        this.config = config.Value;
        this.gamesRepository = gamesRepository;
        this.betsRepository = betsRepository;
        this.betsResolver = betsResolver;
        this.dateTimeProvider = dateTimeProvider;
        this.dateTimeProvider = dateTimeProvider;
        this.actionLogsRepository = actionLogsRepository;
        this.userManager = userManager;
    }

    [HttpGet]
    public IEnumerable<GameViewModel> GetAllGames()
    {
        var res = gamesRepository.GetGames().Select(game => new GameViewModel(game)).ToList();
        AddUserBetsData(res);
        return res;
    }

    [HttpGet(("Stadium/{stadiumId}"))]
    public IEnumerable<GameViewModel> GetStadiumGames(int stadiumId)
    {
        var res = gamesRepository.GetStadiumGames(stadiumId).Select(game => new GameViewModel(game)).ToList();
        AddUserBetsData(res);
        return res;
    }

    [HttpGet(("{id}"))]
    public ActionResult<GameViewModel> GetGameByID(int id)
    {
        var item = gamesRepository.GetGame(id);
        if (item == null)
            return NotFound(string.Format("Game with id '{0}' not found", id));

        var res = new GameViewModel(item);
        res.UserHasBet = betsRepository.GetUserBetOnGame(httpContextAccessor.HttpContext?.User.Identity.Name, id) != null;
        return Ok(res);
    }

    [HttpGet("{id}/Bets")]
    public ActionResult<IEnumerable<BetViewModel>> GetGameBets(int id)
    {
        var game = gamesRepository.GetGame(id);
        if (game == null)
            return NotFound(string.Format("Game with id '{0}' not found", id));

        if (game.IsOpen(dateTimeProvider.UTCNow))
            return BadRequest(String.Format("Game '{0}' is stil open for betting", id));

        return Ok(betsRepository.GetGameBets(id).Select(item => new BetViewModel(item, dateTimeProvider.UTCNow)).OrderByDescending(bet => bet.Points));
    }

    [HttpGet("{id}/MyBet/")]
    public async Task<ActionResult<BetViewModel>> GetGameUserBet(int id)
    {
        var user = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (user == null)
        {
            return Unauthorized();
        }
        var game = GetGameByID(id);
        var uid = user.Id;
        var item = betsRepository.GetGameBets(id).SingleOrDefault(bet => bet.User.Id == uid);
        if (item == null)
        {
            Trace.TraceInformation("No bet found for game {0} and user {1}, creating empty Bet", game.Result, uid);
            return Ok(new BetViewModel() { BetId = -1, HomeScore = null, AwayScore = null, IsOpenForBetting = true, IsResolved = false, Game = new BetGame() { GameId = id } });
        }
        return Ok(new BetViewModel(item, dateTimeProvider.UTCNow));
    }

    [HttpGet("Open")]
    public IEnumerable<GameViewModel> GetOpenGames()
    {
        var res = gamesRepository.GetGames().Where(game => game.IsOpen(dateTimeProvider.UTCNow)).Select(game => new GameViewModel(game));
        AddUserBetsData(res);
        return res;
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public ActionResult<NewGameModel> PostGame(NewGameModel game)
    {
        if (game.AwayTeam.TeamId == game.HomeTeam.TeamId)
        {
            return BadRequest("Home team and Away team can not be the same team");
        }
        var newGame = new Game();
        newGame.HomeTeamId = game.HomeTeam.TeamId;
        newGame.AwayTeamId = game.AwayTeam.TeamId;
        newGame.StadiumId = game.Stadium.StadiumId;
        newGame.Date = game.Date;
        var res = gamesRepository.InsertGame(newGame);
        Trace.TraceInformation("Posting new Game: {0}", game);
        gamesRepository.Save();
        game.GameId = res.GameId;
        game.IsOpen = true;
        game.IsPendingUpdate = false;
        AddLog(ActionType.CREATE, String.Format("Posting new game: {0}", newGame));
        AddMonkeyBet(res);
        return game;
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public ActionResult<PutGameModelResult> PutGame(int id, PutGameModel game)
    {
        var item = gamesRepository.GetGame(id);
        if (item == null)
            return NotFound(string.Format("No such game with id '{0}'", id));

        if (item.IsOpen(dateTimeProvider.UTCNow) && (game.HomeScore != null || game.AwayScore != null || game.CornersMark != null || game.CardsMark != null))
            return BadRequest("Open game can not be updated with results");

        item.AwayScore = game.AwayScore;
        item.HomeScore = game.HomeScore;
        item.CardsMark = game.CardsMark;
        item.CornersMark = game.CornersMark;
        item.Date = game.Date;
        gamesRepository.Save();
        if (item.IsBetResolved(dateTimeProvider.UTCNow))
        {
            AddLog(ActionType.UPDATE, String.Format("Will resolve bets of game {0}", item.GameId));
            Trace.TraceInformation("Will reoslve Game {0} bets", id);
            betsResolver.ResolveBets(item);
        }
        AddLog(ActionType.UPDATE, String.Format("Updating Game {0}", item));
        return Ok(new PutGameModelResult(item, dateTimeProvider.UTCNow));
    }

    [HttpDelete("{id}")]
    [Authorize(Roles = "Admin")]
    public IActionResult DeleteGame(int id)
    {
        var game = gamesRepository.GetGame(id);
        if (game == null)
            return NotFound(string.Format("No such game with id '{0}'", id));
        Trace.TraceInformation("Deleting Game {0}", id);
        gamesRepository.DeleteGame(id);
        gamesRepository.Save();
        AddLog(ActionType.DELETE, String.Format("Deleting Game {0}", id));
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

    private void AddLog(ActionType actionType, String message)
    {
        try
        {
            actionLogsRepository.InsertLogAction(ActionLog.Create(actionType, ObjectType, message, httpContextAccessor.HttpContext?.User.Identity.Name));
            actionLogsRepository.Save();
        }
        catch (Exception e)
        {
            Trace.TraceError("Exception during log. Exception: {0}", e.Message);
        }
    }

    private void AddMonkeyBet(Game res)
    {
        var monkeyUserName = config.MonkeyUserName;
        if (!String.IsNullOrEmpty(monkeyUserName))
        {
            var monkeyUser = userManager.FindByNameAsync(monkeyUserName).ContinueWith(task =>
                {
                    if (task.Result == null)
                    {
                        Trace.TraceError("Monkey user {0} was not found, will not add monkey bet", monkeyUserName);
                        return;
                    }
                    var randomResults = new RandomResults();
                    var result = randomResults.GetRandomResult();
                    betsRepository.InsertBet(new Bet()
                    {
                        GameId = res.GameId,
                        UserId = task.Result.Id,
                        HomeScore = result.Key,
                        AwayScore = result.Value,
                        CardsMark = randomResults.GetRandomMark(),
                        CornersMark = randomResults.GetRandomMark()
                    });
                    betsRepository.Save();
                }
            );


        }
    }
}


