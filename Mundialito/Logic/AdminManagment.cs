using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Mundialito.DAL;
using Mundialito.DAL.Accounts;

namespace Mundialito.Logic;

public class AdminManagment : IAdminManagment
{
    private readonly UserManager<MundialitoUser> usersManager;

    public AdminManagment(UserManager<MundialitoUser> usersManager)
    {
        this.usersManager = usersManager;
    }

    public void MakeAdmin(string userId)
    {
        // TODO: Make this work        
    }

    public bool IsAdmin(string userId)
    {
        // TODO: Makw this work
        return true;
    }
}
