using Mundialito.DAL.Stadiums;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.Models;

/// <summary>The writable half of a Stadium; see <see cref="TeamModel"/> for why the entity is
/// no longer bound directly. The id comes from the route.</summary>
public class StadiumModel
{
    [Required]
    [JsonPropertyName("Name")]
    public required string Name { get; set; }

    [Required]
    [JsonPropertyName("City")]
    public required string City { get; set; }

    [Required]
    [Range(0, int.MaxValue)]
    [JsonPropertyName("Capacity")]
    public required int Capacity { get; set; }

    public Stadium ToStadium() => ApplyTo(new Stadium { Name = Name, City = City, Capacity = Capacity });

    public Stadium ApplyTo(Stadium stadium)
    {
        stadium.Name = Name;
        stadium.City = City;
        stadium.Capacity = Capacity;
        return stadium;
    }
}
