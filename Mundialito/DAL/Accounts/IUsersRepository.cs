using Microsoft.AspNetCore.Identity;

namespace Mundialito.DAL.Accounts;

public interface IUsersRepository : IDisposable
{
    IEnumerable<IdentityUser> AllUsers();

    IdentityUser GetUser(String username);

    void DeleteUser(String username);

    void Save(); 
}
