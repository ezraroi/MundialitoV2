using Mundialito.DAL.Accounts;
using Mundialito.DAL.Bets;
using Mundialito.DAL.Games;
using Mundialito.DAL.Teams;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.Models;


public class BetViewModel
{
    public BetViewModel()
    {

    }

    public BetViewModel(Bet bet, DateTime now)
    {
        BetId = bet.BetId;
        HomeScore = bet.HomeScore;
        AwayScore = bet.AwayScore;
        CornersWin = bet.CornersWin;
        CardsWin = bet.CardsWin;
        CornersMark = bet.CornersMark;
        CardsMark = bet.CardsMark;
        GameMarkWin = bet.GameMarkWin;
        ResultWin = bet.ResultWin;
        IsOpenForBetting = bet.IsOpenForBetting(now);
        IsResolved = bet.IsResolved(now);
        Points = bet.Points.HasValue ? bet.Points.Value : 0;
        Game = new BetGame(bet.Game);
        User = new BetUser(bet.User);
    }

    [JsonPropertyName("BetId")]
    public int BetId { get; set; }

    [JsonPropertyName("HomeScore")]
    public int? HomeScore { get; set; }

    [JsonPropertyName("AwayScore")]
    public int? AwayScore { get; set; }

    [JsonPropertyName("CornersMark")]
    public string CornersMark { get; set; }

    [JsonPropertyName("CardsMark")]
    public string CardsMark { get; set; }

    [JsonPropertyName("CornersWin")]
    public bool CornersWin { get; set; }

    [JsonPropertyName("CardsWin")]
    public bool CardsWin { get; set; }

    [JsonPropertyName("GameMarkWin")]
    public bool GameMarkWin { get; set; }

    [JsonPropertyName("ResultWin")]
    public bool ResultWin { get; set; }

    [JsonPropertyName("Points")]
    public int Points { get; set; }

    [JsonPropertyName("User")]
    public BetUser User { get; set;}

    [JsonPropertyName("Game")]
    public BetGame Game { get; set; }

    [JsonPropertyName("IsOpenForBetting")]
    public bool IsOpenForBetting { get; set; }

    [JsonPropertyName("IsResolved")]
    public bool IsResolved { get; set; }
}

public class BetUser
{
    public BetUser()
    {

    }
    public BetUser(MundialitoUser mundialitoUser)
    {
        UserName = mundialitoUser.UserName;
        FirstName = mundialitoUser.FirstName;
        LastName = mundialitoUser.LastName;
    }
    
    [JsonPropertyName("Username")]
    public string UserName { get; set; }

    [JsonPropertyName("FirstName")]
    public string FirstName { get; set; }

    [JsonPropertyName("LastName")]
    public string LastName { get; set; }
}

public class NewBetModel
{
    public NewBetModel()
    {

    }

    public NewBetModel(int id, UpdateBetModel bet)
    {
        BetId = id;
        GameId = bet.GameId;
        HomeScore = bet.HomeScore;
        AwayScore = bet.AwayScore;
        CornersMark = bet.CornersMark;
        CardsMark = bet.CardsMark;
    }

    [JsonPropertyName("BetId")]
    public int BetId { get; set; }

    [Required]
    [JsonPropertyName("GameId")]
    public int GameId { get; set; }

    [Required]
    [Range(0,10)]
    [JsonPropertyName("HomeScore")]
    public int HomeScore { get; set; }

    [Required]
    [Range(0, 10)]
    [JsonPropertyName("AwayScore")]
    public int AwayScore { get; set; }

    [Required]
    [StringLength(1)]
    [RegularExpression("[1X2]")]
    [JsonPropertyName("CornersMark")]
    public string CornersMark { get; set; }

    [Required]
    [StringLength(1)]
    [RegularExpression("[1X2]")]
    [JsonPropertyName("CardsMark")]
    public string CardsMark { get; set; }
}

public class UpdateBetModel
{
    public UpdateBetModel()
    {

    }

    [Required]
    [JsonPropertyName("GameId")]
    public int GameId { get; set; }

    [Required]
    [Range(0, 10)]
    [JsonPropertyName("HomeScore")]
    public int HomeScore { get; set; }

    [Required]
    [Range(0, 10)]
    [JsonPropertyName("AwayScore")]
    public int AwayScore { get; set; }

    [Required]
    [StringLength(1)]
    [RegularExpression("[1X2]")]
    [JsonPropertyName("CornersMark")]
    public string CornersMark { get; set; }

    [Required]
    [StringLength(1)]
    [RegularExpression("[1X2]")]
    [JsonPropertyName("CardsMark")]
    public string CardsMark { get; set; }
}

public class BetGame
{
    public BetGame()
    {

    }

    public BetGame(Game game)
    {
        GameId = game.GameId;
        HomeTeam = new BetGameTeam(game.HomeTeam);
        AwayTeam = new BetGameTeam(game.AwayTeam);
        IsOpen = game.IsOpen();
        Date = game.Date.ToLocalTime();
    }
    [JsonPropertyName("GameId")]
    public int GameId { get; set; }

    [JsonPropertyName("HomeTeam")]
    public BetGameTeam HomeTeam { get; set; }

    [JsonPropertyName("AwayTeam")]
    public BetGameTeam AwayTeam { get; set; }

    [JsonPropertyName("IsOpen")]
    public bool IsOpen { get; set; }

    [DataType(DataType.DateTime)]
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

}

public class BetGameTeam
{

    public BetGameTeam()
    {

    }

    public BetGameTeam(Team team)
    {
        TeamId = team.TeamId;
        Name = team.Name;
        ShortName = team.ShortName;
    }
    [JsonPropertyName("TeamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("ShortName")]
    public string ShortName { get; set; }

}
