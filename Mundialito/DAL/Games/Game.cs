using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.DAL.Games;

public class Game
{
    private const int MarkGroupsPoints = 3;
    private const int MarkKnockoutsPoints = 4;
    private const int ResultGroupsPoints = 2;
    private const int ResultKnockoutsPoints = 3;
    private const int CardsAndCornersPoints = 1;
    private const int BingoPoints = 2;

    [Required]
    [JsonPropertyName("GameId")]
    public int GameId { get; set; }

    [Required]
    [JsonPropertyName("Type")]
    public GameType Type { get; set; }

    [Required]
    [JsonPropertyName("HomeTeamId")]
    public int HomeTeamId { get; set; }

    [JsonPropertyName("HomeTeam")]
    public virtual Team? HomeTeam { get; set; }

    [Required]
    [JsonPropertyName("AwayTeamId")]
    public int AwayTeamId { get; set; }

    [JsonPropertyName("Team")]
    public virtual Team? AwayTeam { get; set; }

    [Required]
    [DataType(DataType.DateTime)]
    [JsonPropertyName("Date")]
    public DateTime Date { get; set; }

    [Range(0, 10)]
    [JsonPropertyName("HomeScore")]
    public int? HomeScore { get; set; }

    [Range(0, 10)]
    [JsonPropertyName("AwayScore")]
    public int? AwayScore { get; set; }

    [StringLength(1)]
    [RegularExpression("[1X2-]")]
    [JsonPropertyName("CornersMark")]
    public string? CornersMark { get; set; }

    [StringLength(1)]
    [RegularExpression("[1X2-]")]
    [JsonPropertyName("CardsMark")]
    public string? CardsMark { get; set; }

    [Required]
    [JsonPropertyName("StadiumId")]
    public int StadiumId { get; set; }

    [JsonPropertyName("Stadium")]
    public virtual Stadium? Stadium { get; set; }

    [JsonPropertyName("IntegrationsData")]
    public Dictionary<string, string>? IntegrationsData { get; set; }

    [JsonPropertyName("CloseTime")]
    public DateTime CloseTime
    {
        get
        {
            return Date.Subtract(TimeSpan.FromMinutes(15));
        }
    }

    public override string ToString()
    {
        return string.Format("Game ID = {0}, {1} - {2}", GameId, HomeTeam != null ? HomeTeam.Name : "Unknown", AwayTeam != null ? AwayTeam.Name : "Unknown");
    }

    public int MarkPoints() 
    {
        switch (Type)
        {
            case GameType.Groups:
                return MarkGroupsPoints;
            case GameType.Knockouts:
                return MarkKnockoutsPoints;
            default:
                return MarkGroupsPoints;
        }
    }

    public int ResultPoints() 
    {
        switch (Type)
        {
            case GameType.Groups:
                return ResultGroupsPoints;
            case GameType.Knockouts:
                return ResultKnockoutsPoints;
            default:
                return ResultGroupsPoints;
        }
    }

    public int CardsPoints() 
    {
        return CardsAndCornersPoints;
    }

    public int CornersPoints() 
    {
        return CardsAndCornersPoints;
    }

    public int BingoBonusPoints() 
    {
        return BingoPoints;
    }

    public int MaxPoints() 
    {
        return MarkPoints() + ResultPoints() + CardsPoints() + CornersPoints();
    }
}
