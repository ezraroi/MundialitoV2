using Mundialito.DAL.Players;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.Models;

/// <summary>The writable half of a Player - the name and nothing else; see
/// <see cref="TeamModel"/> for why the entity is no longer bound directly. The id comes
/// from the route, and IntegrationsData is filled by the tournament creator, never by a
/// caller.</summary>
public class PlayerModel
{
    [Required]
    [JsonPropertyName("Name")]
    public required string Name { get; set; }

    public Player ToPlayer() => new Player { Name = Name };
}
