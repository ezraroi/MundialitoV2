using Mundialito.DAL.Teams;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.Models;

/// <summary>
/// The writable half of a Team. The entity was bound directly before, which let an admin set
/// TeamId and reach the navigation collections - mass assignment over the row. Only what a
/// caller may legitimately change lives here; the id comes from the route.
/// </summary>
public class TeamModel
{
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

    [JsonPropertyName("TournamentTeamId")]
    public int? TournamentTeamId { get; set; }

    [Url]
    [JsonPropertyName("TeamPage")]
    public string? TeamPage { get; set; }

    [JsonPropertyName("IntegrationsData")]
    public Dictionary<string, string>? IntegrationsData { get; set; }

    public Team ToTeam() => ApplyTo(new Team { Name = Name, Flag = Flag, Logo = Logo, ShortName = ShortName });

    /// <summary>Copies every writable field onto a team. The single list of what a caller may
    /// change - create and update both go through it, so they cannot drift apart.</summary>
    public Team ApplyTo(Team team)
    {
        team.Name = Name;
        team.Flag = Flag;
        team.Logo = Logo;
        team.ShortName = ShortName;
        team.TournamentTeamId = TournamentTeamId;
        team.TeamPage = TeamPage;
        team.IntegrationsData = IntegrationsData;
        return team;
    }
}
