using Mundialito.DAL.Stadiums;
using System.Diagnostics;
using Mundialito.DAL.ActionLogs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mundialito.Models;
using Mundialito.Auth.Authorization;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StadiumsController : ControllerBase
{
    private readonly IStadiumsRepository stadiumsRepository;
    private readonly ILogger logger;

    public StadiumsController(ILogger<StadiumsController> logger, IStadiumsRepository stadiumsRepository)
    {
        this.stadiumsRepository = stadiumsRepository;
        this.logger = logger;
    }

    [HttpGet]
    public IEnumerable<Stadium> GetAllStadiums()
    {
        return stadiumsRepository.GetStadiums();
    }

    [HttpGet("{id}")]
    public ActionResult<Stadium> GetStadium(int id)
    {
        var item = stadiumsRepository.GetStadium(id);
        if (item == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Stadium with id '{0}' not found", id)});
        return Ok(item);
    }



    [Authorize(Policy = Policies.AdminOnly)]
    [HttpPost]
    public Stadium PostStadium(StadiumModel stadium)
    {
        var res = stadiumsRepository.InsertStadium(new Stadium
        {
            Name = stadium.Name,
            City = stadium.City,
            Capacity = stadium.Capacity
        });
        stadiumsRepository.Save();
        return res;
    }

    [HttpPut("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public ActionResult<Stadium> PutStadium(int id, StadiumModel stadium)
    {
        var stadiumToUpdate = stadiumsRepository.GetStadium(id);
        if (stadiumToUpdate == null)
            return NotFound(new ErrorMessage{ Message = string.Format("Stadium with id '{0}' not found", id)});
        stadiumToUpdate.Name = stadium.Name;
        stadiumToUpdate.City = stadium.City;
        stadiumToUpdate.Capacity = stadium.Capacity;
        stadiumsRepository.Save();
        // The stored row, not the object the caller sent.
        return Ok(stadiumToUpdate);
    }

    [HttpDelete("{id}")]
    [Authorize(Policy = Policies.AdminOnly)]
    public void DeleteStadium(int id)
    {
        logger.LogInformation("Deleting Stadium {0}", id);
        stadiumsRepository.DeleteStadium(id);
        stadiumsRepository.Save();
        logger.LogInformation("Deleted Stadium {0}", id);
    }

}

