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
        Type = game.Type;
        Date = game.Date.ToLocalTime();
        HomeScore = game.HomeScore;
        AwayScore = game.AwayScore;
        CornersMark = game.CornersMark;
        CardsMark = game.CardsMark;
        Stadium = game.Stadium;
        IsOpen = game.IsOpen();
        IsPendingUpdate = game.IsPendingUpdate();
        IsBetResolved = game.IsBetResolved();
        Mark = game.Mark();
        if (game.HomeTeam.TournamentTeamId.HasValue && game.AwayTeam.TournamentTeamId.HasValue)
        {
            GameStatsPage = string.Format($"https://www.uefa.com/euro2024/teams/comparison/{game.HomeTeam.TournamentTeamId.Value}/{game.AwayTeam.TournamentTeamId.Value}/");
        }
        else
        {
            GameStatsPage = null;
        }
        IntegrationsData = game.IntegrationsData;
    }

    [JsonPropertyName("GameId")]
    public int GameId { get; private set; }

    [JsonPropertyName("Type")]
    public GameType Type { get; private set; }
    
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
    public string CornersMark { get; private set; }

    [JsonPropertyName("CardsMark")]
    public string CardsMark { get; private set; }

    [JsonPropertyName("Stadium")]
    public Stadium Stadium { get; private set; }

    [JsonPropertyName("UserHasBet")]
    public bool UserHasBet { get; set; }

    [JsonPropertyName("CloseTime")]
    public DateTime CloseTime
    {
        get
        {
            return Date.Subtract(TimeSpan.FromMinutes(30));
        }
    }

    [JsonPropertyName("IsOpen")]
    public bool IsOpen { get; private set; }

    [JsonPropertyName("IsPendingUpdate")]
    public bool IsPendingUpdate { get; private set; }

    [JsonPropertyName("IsBetResolved")]
    public bool IsBetResolved { get; private set; }

    [JsonPropertyName("Mark")]
    public string Mark { get; private set; }

    [JsonPropertyName("GameStatsPage")]
    public string? GameStatsPage { get; set; }

    [JsonPropertyName("IntegrationsData")]
    public Dictionary<string, string>? IntegrationsData { get; set; }

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
        TournamentTeamId = team.TournamentTeamId;
        TeamPage = team.TeamPage;
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

    [Url]
    [JsonPropertyName("TeamPage")]
    public string? TeamPage { get; set; }

    [JsonPropertyName("TournamentTeamId")]
    public int? TournamentTeamId { get; set; }


}

public class NewGameModel
{
    [JsonPropertyName("GameId")]
    public int GameId { get; set; }

    [Required]
    [JsonPropertyName("Type")]
    public GameType Type { get; set; }

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
    public bool IsOpen { get; set; }

    [JsonPropertyName("IsPendingUpdate")]
    public bool IsPendingUpdate { get; set; }

    [JsonPropertyName("IntegrationsData")]
    public Dictionary<string, string>? IntegrationsData { get; set; }
    [JsonPropertyName("CloseTime")]
    public DateTime CloseTime
    {
        get
        {
            return Date.Subtract(TimeSpan.FromMinutes(30));
        }
    }
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
        IntegrationsData = game.IntegrationsData;
        Type = game.Type;
    }

    [JsonPropertyName("Type")]
    public GameType Type { get; set; }
    
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [JsonPropertyName("HomeScore")]
    public int? HomeScore { get; set; }

    [JsonPropertyName("AwayScore")]
    public int? AwayScore { get; set; }

    [JsonPropertyName("CornersMark")]
    public string? CornersMark { get; set; }

    [JsonPropertyName("CardsMark")]
    public string? CardsMark { get; set; }

    [JsonPropertyName("IntegrationsData")]
    public Dictionary<string, string>? IntegrationsData { get; set; }

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
    public bool IsOpen { get; private set; }

    [JsonPropertyName("IsPendingUpdate")]
    public bool IsPendingUpdate { get; private set; }

    [JsonPropertyName("IsBetResolved")]
    public bool IsBetResolved { get; private set; }

    [JsonPropertyName("Mark")]
    public string Mark { get; private set; }
    [JsonPropertyName("CloseTime")]
    public DateTime CloseTime
    {
        get
        {
            return Date.Subtract(TimeSpan.FromMinutes(30));
        }
    }

}

public class SimulateGameModel
{
    [Required]
    [JsonPropertyName("HomeScore")]
    public required int HomeScore { get; set; }

    [Required]
    [JsonPropertyName("AwayScore")]
    public required int AwayScore { get; set; }

    [Required]
    [JsonPropertyName("CornersMark")]
    public required string CornersMark { get; set; }

    [Required]
    [JsonPropertyName("CardsMark")]
    public required string CardsMark { get; set; }
}