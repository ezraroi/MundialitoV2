using Mundialito.DAL.Games;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Mundialito.DAL.Stadiums;

public class Stadium
{
    public int StadiumId { get; set; }

    [Required]
    public required string Name { get; set; }

    [Required]
    public required string City { get; set; }

    [Required]
    [Range(0,int.MaxValue)]
    public required int Capacity { get; set; }
}
