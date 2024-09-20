using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.DAL.GeneralBets;
using Mundialito.Logic;
using Mundialito.Models;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
public class StatsController : ControllerBase
{
    private readonly IGamesRepository gamesRepository;
    private readonly IBetsRepository betsRepository;
    private readonly IGeneralBetsRepository generalBetsRepository;
    private readonly UserManager<MundialitoUser> userManager;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly TableBuilder tableBuilder;
    private readonly MundialitoDbContext mundialitoDbContext;

    public StatsController(IGamesRepository gamesRepository, IBetsRepository betsRepository, UserManager<MundialitoUser> userManager, IHttpContextAccessor httpContextAccessor, TableBuilder tableBuilder, IGeneralBetsRepository generalBetsRepository, MundialitoDbContext mundialitoDbContext)
    {
        this.gamesRepository = gamesRepository;
        this.betsRepository = betsRepository;
        this.userManager = userManager;
        this.httpContextAccessor = httpContextAccessor;
        this.tableBuilder = tableBuilder;
        this.generalBetsRepository = generalBetsRepository;
        this.mundialitoDbContext = mundialitoDbContext;
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<StatsModel>> GetMyStats() 
    {
        var loggedUser = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (loggedUser == null)
            return Unauthorized(new ErrorMessage { Message = "You must be logged in" });
        var followees = await mundialitoDbContext.UserFollows
            .Where(uf => uf.Follower.UserName == loggedUser.UserName)
            .Select(uf => uf.Followee)
            .ToListAsync();
        return await CalcStats(loggedUser , loggedUser, followees);
    }

    [HttpGet("{username}")]
    [Authorize]
    public async Task<ActionResult<StatsModel>> GetStats(string username)
    {
        var requestedUser = await userManager.FindByNameAsync(username);
        if (requestedUser == null)
            return NotFound(new ErrorMessage { Message = "No such user" });
        var loggedUser = await userManager.FindByNameAsync(httpContextAccessor.HttpContext?.User.Identity.Name);
        if (loggedUser == null)
            return Unauthorized(new ErrorMessage { Message = "You must be logged in" });
        return await CalcStats(requestedUser, loggedUser, Enumerable.Empty<MundialitoUser>());
    }

    private async Task<ActionResult<StatsModel>> CalcStats(MundialitoUser requestedUser, MundialitoUser loggedUser, IEnumerable<MundialitoUser> followees)
    {
        var games = (float)gamesRepository.GetGames().Where((game) => game.IsBetResolved()).Count();
        var users = (float)userManager.Users.Count();
        var bets = betsRepository.GetBets().Where((bet) => bet.IsResolved()).ToList();
        var table = tableBuilder.GetTable(userManager.Users.Select((user) => new UserWithPointsModel(user)), bets, generalBetsRepository.GetGeneralBets());
        var leader = await userManager.FindByNameAsync(table.First().Username);
        if (leader == null)
            return StatusCode(500);
        return Ok(new StatsModel
        {
            PointsPerMatch = getPointsPerMatch(games, users, bets, requestedUser, loggedUser, leader, followees),
            CornersPointsPerMatch = getCornersPointsPerMatch(games, users, bets, requestedUser, loggedUser, leader, followees),
            CardsPointsPerMatch = getCardsPointsPerMatch(games, users, bets, requestedUser, loggedUser, leader, followees),
            MarkProbability = getMarkPerMatch(games, users, bets, requestedUser, loggedUser, leader, followees),
            ResultProbability = getResultProbability(games, users, bets, requestedUser, loggedUser, leader, followees),
            NumOfBingos = getNumOfBingo(games, users, bets, requestedUser, loggedUser, leader, followees)
        });
    }

    private PerGameModel getPointsPerMatch(float games, float users, List<Bet> bets, MundialitoUser user, MundialitoUser loggedUser, MundialitoUser leader, IEnumerable<MundialitoUser> followees)
    {
        var requestedPoints = bets.Where((bet) => bet.User.UserName == user.UserName).Select((bet) => bet.Points).Sum().GetValueOrDefault();
        var followeesPoints = bets.Where((bet) => followees.Where((fol) => fol.UserName == bet.User.UserName).Any()).Select((bet) => bet.Points).Sum().GetValueOrDefault();
        var loggedPoints = bets.Where((bet) => bet.User.UserName == loggedUser.UserName).Select((bet) => bet.Points).Sum().GetValueOrDefault();
        var leaderPoints = bets.Where((bet) => bet.User.UserName == leader.UserName).Select((bet) => bet.Points).Sum().GetValueOrDefault();
        var allPoins = bets.Select((bet) => bet.Points).Sum().GetValueOrDefault();
        return new PerGameModel
        {
            You = getSafe(loggedPoints, games),
            User = getSafe(requestedPoints, games),
            Leader = getSafe(leaderPoints, games),
            Overall = getSafe(getSafe(allPoins, users), games),
            Followees = getSafe(getSafe(followeesPoints, followees.Count()), games),
            BestResult = new BestResult {
                Name = leader.FirstName + " " + leader.LastName,
                Value = getSafe(leaderPoints, games),
            },
            Category = "Points per Match"
        };
    }

    private PerGameModel getCornersPointsPerMatch(float games, float users, List<Bet> bets, MundialitoUser user, MundialitoUser loggedUser, MundialitoUser leader, IEnumerable<MundialitoUser> followees)
    {
        var best = bets.GroupBy((bet) => bet.User.FirstName + " " + bet.User.LastName).ToDictionary(g => g.Key, g => g.Where((bet) => bet.CornersWin).Count()).OrderByDescending(x => x.Value).FirstOrDefault();
        var requestedCorners = bets.Where((bet) => bet.User.UserName == user.UserName).Where((bet) => bet.CornersWin).Count();
        var followeesCorners = bets.Where((bet) => followees.Where((fol) => fol.UserName == bet.User.UserName).Any()).Where((bet) => bet.CornersWin).Count();
        var loggedCorners = bets.Where((bet) => bet.User.UserName == loggedUser.UserName).Where((bet) => bet.CornersWin).Count();
        var leaderCorners = bets.Where((bet) => bet.User.UserName == leader.UserName).Where((bet) => bet.CornersWin).Count();
        var allCorners = bets.Where((bet) => bet.CornersWin).Count();
        return new PerGameModel
        {
            You = getSafe(loggedCorners, games),
            User = getSafe(requestedCorners, games),
            Leader = getSafe(leaderCorners, games),
            Overall = getSafe(getSafe(allCorners, users), games),
            Followees = getSafe(getSafe(followeesCorners, followees.Count()), games),
            BestResult = new BestResult{
                Value = getSafe(best.Value, games),
                Name = best.Key,
            },
            Category = "Corners points per Match (out of 1 point)"
        };
    }

    private PerGameModel getCardsPointsPerMatch(float games, float users, List<Bet> bets, MundialitoUser user, MundialitoUser loggedUser, MundialitoUser leader, IEnumerable<MundialitoUser> followees)
    {
        var best = bets.GroupBy((bet) => bet.User.FirstName + " " + bet.User.LastName).ToDictionary(g => g.Key, g => g.Where((bet) => bet.CardsWin).Count()).OrderByDescending(x => x.Value).FirstOrDefault();
        var requestedCards = bets.Where((bet) => bet.User.UserName == user.UserName).Where((bet) => bet.CardsWin).Count();
        var followeesCards = bets.Where((bet) => followees.Where((fol) => fol.UserName == bet.User.UserName).Any()).Where((bet) => bet.CardsWin).Count();
        var loggedCards = bets.Where((bet) => bet.User.UserName == loggedUser.UserName).Where((bet) => bet.CardsWin).Count();
        var leaderCards = bets.Where((bet) => bet.User.UserName == leader.UserName).Where((bet) => bet.CardsWin).Count();
        var allCards = bets.Where((bet) => bet.CardsWin).Count();
        return new PerGameModel
        {
            You = getSafe(loggedCards, games),
            User = getSafe(requestedCards, games),
            Leader = getSafe(leaderCards, games),
            Overall = getSafe(getSafe(allCards, users), games),
            Followees = getSafe(getSafe(followeesCards, followees.Count()), games),
            BestResult = new BestResult{
                Value = getSafe(best.Value, games),
                Name = best.Key,
            },
            Category = "Cards points per Match (out of 1 point)"
        };
    }

    private PerGameModel getMarkPerMatch(float games, float users, List<Bet> bets, MundialitoUser user, MundialitoUser loggedUser, MundialitoUser leader, IEnumerable<MundialitoUser> followees)
    {
        var best = bets.GroupBy((bet) => bet.User.FirstName + " " + bet.User.LastName).ToDictionary(g => g.Key, g => g.Where((bet) => bet.GameMarkWin).Count()).OrderByDescending(x => x.Value).FirstOrDefault();
        var requested = bets.Where((bet) => bet.User.UserName == user.UserName).Where((bet) => bet.GameMarkWin).Count();
        var followeesMarks = bets.Where((bet) => followees.Where((fol) => fol.UserName == bet.User.UserName).Any()).Where((bet) => bet.GameMarkWin).Count();
        var logged = bets.Where((bet) => bet.User.UserName == loggedUser.UserName).Where((bet) => bet.GameMarkWin).Count();
        var leaderMarks = bets.Where((bet) => bet.User.UserName == leader.UserName).Where((bet) => bet.GameMarkWin).Count();
        var all = bets.Where((bet) => bet.GameMarkWin).Count();
        return new PerGameModel
        {
            You = getSafe(logged, games),
            User = getSafe(requested, games),
            Leader = getSafe(leaderMarks, games),
            Overall = getSafe(getSafe(all, users), games),
            Followees = getSafe(getSafe(followeesMarks, followees.Count()), games),
            BestResult = new BestResult{
                Value = getSafe(best.Value, games),
                Name = best.Key,
            },
            Category = "Match mark win probability"
        };
    }

    private PerGameModel getResultProbability(float games, float users, List<Bet> bets, MundialitoUser user, MundialitoUser loggedUser, MundialitoUser leader, IEnumerable<MundialitoUser> followees)
    {
        var best = bets.GroupBy((bet) => bet.User.FirstName + " " + bet.User.LastName).ToDictionary(g => g.Key, g => g.Where((bet) => bet.ResultWin).Count()).OrderByDescending(x => x.Value).FirstOrDefault();
        var requested = bets.Where((bet) => bet.User.UserName == user.UserName).Where((bet) => bet.ResultWin).Count();
        var followeesRes = bets.Where((bet) => followees.Where((fol) => fol.UserName == bet.User.UserName).Any()).Where((bet) => bet.ResultWin).Count();
        var logged = bets.Where((bet) => bet.User.UserName == loggedUser.UserName).Where((bet) => bet.ResultWin).Count();
        var leaderRes = bets.Where((bet) => bet.User.UserName == leader.UserName).Where((bet) => bet.ResultWin).Count();
        var all = bets.Where((bet) => bet.ResultWin).Count();
        return new PerGameModel
        {
            You = getSafe(logged, games),
            User = getSafe(requested, games),
            Leader = getSafe(leaderRes, games),
            Overall = getSafe(getSafe(all, users), games),
            Followees = getSafe(getSafe(followeesRes, followees.Count()), games),
            BestResult = new BestResult{
                Value = getSafe(best.Value, games),
                Name = best.Key,
            },
            Category = "Match result win probability"
        };
    }

    private PerGameModel getNumOfBingo(float games, float users, List<Bet> bets, MundialitoUser user, MundialitoUser loggedUser, MundialitoUser leader, IEnumerable<MundialitoUser> followees)
    {
        var best = bets.GroupBy((bet) => bet.User.FirstName + " " + bet.User.LastName).ToDictionary(g => g.Key, g => g.Where((bet) => bet.Points == 7).Count()).OrderByDescending(x => x.Value).FirstOrDefault();
        var requested = bets.Where((bet) => bet.User.UserName == user.UserName).Where((bet) => bet.Points == 7).Count();
        var followeesRes = bets.Where((bet) => followees.Where((fol) => fol.UserName == bet.User.UserName).Any()).Where((bet) => bet.Points == 7).Count();
        var logged = bets.Where((bet) => bet.User.UserName == loggedUser.UserName).Where((bet) => bet.Points == 7).Count();
        var leaderBingo = bets.Where((bet) => bet.User.UserName == leader.UserName).Where((bet) => bet.Points == 7).Count();
        var all = bets.Where((bet) => bet.Points == 7).Count();
        return new PerGameModel
        {
            You = logged,
            User = requested,
            Leader = leaderBingo,
            Overall = getSafe(all, users),
            Followees = getSafe(followeesRes, followees.Count()),
            BestResult = new BestResult{
                Value = best.Value, 
                Name = best.Key,
            },
            Category = "Total Bingos"
        };
    }

    private float getSafe(float a, float b)
    {
        if (b == 0)
            return 0;
        return (float)Math.Round( a / b, 2);
    }
}