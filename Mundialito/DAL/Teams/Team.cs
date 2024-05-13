using Mundialito.DAL.Games;
using System.ComponentModel.DataAnnotations;

namespace Mundialito.DAL.Teams;

public class Team
{
    public int TeamId { get; set; }
    
    [Required]
    public required string Name { get; set; }

    [Required]
    [DataType(DataType.ImageUrl)]
    public required string Flag { get; set; }

    [Required]
    [DataType(DataType.ImageUrl)]
    public required string Logo { get; set; }

    [Required]
    [MinLength(3)]
    [MaxLength(3)]
    public required string ShortName { get; set; }

    public ICollection<Game> HomeMatches { get; set; }

    public ICollection<Game> AwayMatches { get; set; }
}
