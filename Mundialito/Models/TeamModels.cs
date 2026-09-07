using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.Models;

/// <summary>
/// The writable half of a Team. The entity was bound directly before, which let an admin
/// set TeamId and reach the navigation collections - mass assignment over the row. Only
/// what a caller may legitimately change lives here; the id comes from the route.
/// </summary>
public class TeamModel
{
    [Required]
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [Required]
    [DataType(DataType.ImageUrl)]
    [JsonPropertyName("Flag")]
    public string Flag { get; set; }

    [Required]
    [DataType(DataType.ImageUrl)]
    [JsonPropertyName("Logo")]
    public string Logo { get; set; }

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    [JsonPropertyName("ShortName")]
    public string ShortName { get; set; }

    [JsonPropertyName("TournamentTeamId")]
    public int? TournamentTeamId { get; set; }

    [Url]
    [JsonPropertyName("TeamPage")]
    public string? TeamPage { get; set; }

    [JsonPropertyName("IntegrationsData")]
    public Dictionary<string, string>? IntegrationsData { get; set; }
}
