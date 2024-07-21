using Mundialito.DAL.Stadiums;
using Mundialito.DAL.Teams;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.DAL.Games;

public class Game
{
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
}
