using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.DAL.Stadiums;

public class Stadium
{
    [JsonPropertyName("StadiumId")]
    public int StadiumId { get; set; }

    [Required]
    [JsonPropertyName("Name")]
    public required string Name { get; set; }

    [Required]
    [JsonPropertyName("City")]
    public required string City { get; set; }

    [Required]
    [Range(0,int.MaxValue)]
    [JsonPropertyName("Capacity")]
    public required int Capacity { get; set; }
}
