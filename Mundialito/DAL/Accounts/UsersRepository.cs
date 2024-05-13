using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace Mundialito.DAL.Accounts;

public class UsersRepository : GenericRepository<IdentityUser>, IUsersRepository
{
    public UsersRepository()
        : base(new MundialitoDbContext())
    {
        
    }

    public IEnumerable<IdentityUser> AllUsers()
    {
        return Context.Users;
    }

    public IdentityUser GetUser(String username)
    {
        return Context.Users.SingleOrDefault(user => user.UserName == username);
    }


    public void DeleteUser(string id)
    {
        Delete(id);
    }
}
