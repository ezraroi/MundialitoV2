using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.Models;

/// <summary>The writable half of a Stadium; see <see cref="TeamModel"/> for why the entity
/// is no longer bound directly. The id comes from the route.</summary>
public class StadiumModel
{
    [Required]
    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [Required]
    [JsonPropertyName("City")]
    public string City { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [JsonPropertyName("Capacity")]
    public int Capacity { get; set; }
}
