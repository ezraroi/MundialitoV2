using Mundialito.DAL.Games;
using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.Models;

public class GameViewModel
{
    public GameViewModel(Game game)
    {
        GameId = game.GameId;
        HomeTeam = new GameTeamModel(game.HomeTeam);
        AwayTeam = new GameTeamModel(game.AwayTeam);
        Date = game.Date;
        HomeScore = game.HomeScore;
        AwayScore = game.AwayScore;
        CornersMark = game.CornersMark;
        CardsMark = game.CardsMark;
        Stadium = game.Stadium;
        IsOpen = game.IsOpen();
        IsPendingUpdate = game.IsPendingUpdate();
        IsBetResolved = game.IsBetResolved();
        Mark = game.Mark();
    }

    [JsonPropertyName("GameId")]
    public int GameId { get; private set; }

    [JsonPropertyName("HomeTeam")]
    public GameTeamModel HomeTeam { get; private set; }

    [JsonPropertyName("AwayTeam")]
    public GameTeamModel AwayTeam { get; private set; }

    [JsonPropertyName("Date")]
    public DateTime Date { get; private set; }

    [JsonPropertyName("HomeScore")]
    public int? HomeScore { get; private set; }

    [JsonPropertyName("AwayScore")]
    public int? AwayScore { get; private set; }

    [JsonPropertyName("CornersMark")]
    public String CornersMark { get; private set; }

    [JsonPropertyName("CardsMark")]
    public String CardsMark { get; private set; }

    [JsonPropertyName("Stadium")]
    public Stadium Stadium { get; private set; }

    [JsonPropertyName("UserHasBet")]
    public Boolean UserHasBet { get; set; }

    [JsonPropertyName("CloseTime")]
    public DateTime CloseTime
    {
        get
        {
            return Date.Subtract(TimeSpan.FromMinutes(30));
        }
    }

    [JsonPropertyName("IsOpen")]
    public Boolean IsOpen { get; private set; }

    [JsonPropertyName("IsPendingUpdate")]
    public Boolean IsPendingUpdate { get; private set; }

    [JsonPropertyName("TeamId")]
    public Boolean IsBetResolved { get; private set; }

    [JsonPropertyName("Mark")]
    public String Mark { get; private set; }

}

public class GameTeamModel
{
    public GameTeamModel()
    {

    }

    public GameTeamModel(Team team)
    {
        TeamId = team.TeamId;
        Name = team.Name;
        Flag = team.Flag;
        Logo = team.Logo;
        ShortName = team.ShortName;
    }

    [JsonPropertyName("TeamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("Flag")]
    public string Flag { get; set; }

    [JsonPropertyName("Logo")]
    public string Logo { get; set; }

    [JsonPropertyName("ShortName")]
    public string ShortName { get; set; }

}

public class NewGameModel
{
    [JsonPropertyName("GameId")]
    public int GameId { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [Required]
    [JsonPropertyName("Stadium")]
    public Stadium? Stadium { get; set; }

    [Required]
    [JsonPropertyName("HomeTeam")]
    public GameTeamModel? HomeTeam { get; set; }

    [Required]
    [JsonPropertyName("AwayTeam")]
    public GameTeamModel? AwayTeam { get; set; }

    [JsonPropertyName("IsOpen")]
    public Boolean IsOpen { get; set; }

    [JsonPropertyName("IsPendingUpdate")]
    public Boolean IsPendingUpdate { get; set; }
}

public class PutGameModel
{
    public PutGameModel()
    {

    }

    public PutGameModel(Game game)
    {
        Date = game.Date;
        HomeScore = game.HomeScore;
        AwayScore = game.AwayScore;
        CornersMark = game.CornersMark;
        CardsMark = game.CardsMark;
    }

    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("HomeScore")]
    public int? HomeScore { get; set; }

    [JsonPropertyName("AwayScore")]
    public int? AwayScore { get; set; }

    [JsonPropertyName("CornersMark")]
    public String? CornersMark { get; set; }

    [JsonPropertyName("CardsMark")]
    public String? CardsMark { get; set; }

}

public class PutGameModelResult : PutGameModel
{
    public PutGameModelResult(Game game, DateTime now) : base(game)
    {
        GameId = game.GameId;
        IsOpen = game.IsOpen(now);
        IsPendingUpdate = game.IsPendingUpdate(now);
        IsBetResolved = game.IsBetResolved(now);
        Mark = game.Mark(now);
    }

    [JsonPropertyName("GameId")]
    public int GameId { get; private set; }

    [JsonPropertyName("IsOpen")]
    public Boolean IsOpen { get; private set; }

    [JsonPropertyName("IsPendingUpdate")]
    public Boolean IsPendingUpdate { get; private set; }

    [JsonPropertyName("IsBetResolved")]
    public Boolean IsBetResolved { get; private set; }

    [JsonPropertyName("Mark")]
    public String Mark { get; private set; }

}
