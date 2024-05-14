using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.DAL.Players;

public class Player
{
    [JsonPropertyName("PlayerId")]
    public int PlayerId { get; set; }
    
    [Required]
    [JsonPropertyName("Name")]
    public required string Name { get; set; }

}
