using Mundialito.DAL.Stadiums;
using System.Diagnostics;
using Mundialito.DAL.ActionLogs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Mundialito.DAL;

namespace Mundialito.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class StadiumsController : ControllerBase
{
    private readonly IStadiumsRepository stadiumsRepository;
    private readonly IActionLogsRepository actionLogsRepository;

    public StadiumsController(IStadiumsRepository stadiumsRepository, IActionLogsRepository actionLogsRepository)
    {
        if (stadiumsRepository == null)
            throw new ArgumentNullException("stadiumsRepository");
        this.stadiumsRepository = stadiumsRepository;

        if (actionLogsRepository == null)
            throw new ArgumentNullException("actionLogsRepository");
        this.actionLogsRepository = actionLogsRepository;
    }

    [HttpGet]
    public IEnumerable<Stadium> GetAllStadiums()
    {
        return stadiumsRepository.GetStadiums();
    }

    [HttpGet("{id}")]
    public Stadium GetStadium(int id)
    {
        var item = stadiumsRepository.GetStadium(id);

        if (item == null)
            throw new ObjectNotFoundException(string.Format("Stadium with id '{0}' not found", id));
        return item;
    }



    [Authorize(Roles = "Admin")]
    [HttpPost]
    public Stadium PostStadium(Stadium stadium)
    {
        var res = stadiumsRepository.InsertStadium(stadium);
        stadiumsRepository.Save();
        return res;
    }

    [HttpPut("{id}")]
    [Authorize(Roles = "Admin")]
    public Stadium PutStadium(int id, Stadium stadium)
    {
        stadiumsRepository.UpdateStadium(stadium);
        stadiumsRepository.Save();
        return stadium;
    }

    [HttpDelete]
    [Authorize(Roles = "Admin")]
    public void DeleteStadium(int id)
    {
        Trace.TraceInformation("Deleting Stadium {0}", id);
        stadiumsRepository.DeleteStadium(id);
        stadiumsRepository.Save();
    }

}

