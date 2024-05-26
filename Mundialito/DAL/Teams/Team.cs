using Mundialito.DAL.Games;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.DAL.Teams;

public class Team
{
    [Required]
    [JsonPropertyName("TeamId")]
    public int TeamId { get; set; }

    [JsonPropertyName("TournamentTeamId")]
    public int? TournamentTeamId { get; set; }
    
    [Required]
    [JsonPropertyName("Name")]
    public required string Name { get; set; }

    [Required]
    [DataType(DataType.ImageUrl)]
    [JsonPropertyName("Flag")]
    public required string Flag { get; set; }

    [Required]
    [DataType(DataType.ImageUrl)]
    [JsonPropertyName("Logo")]
    public required string Logo { get; set; }

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    [JsonPropertyName("ShortName")]
    public required string ShortName { get; set; }

    [JsonPropertyName("HomeMatches")]
    public ICollection<Game>? HomeMatches { get; set; }

    [JsonPropertyName("AwayMatches")]
    public ICollection<Game>? AwayMatches { get; set; }
    
    [Url]
    [JsonPropertyName("TeamPage")]
    public string? TeamPage { get; set; }
    
}
