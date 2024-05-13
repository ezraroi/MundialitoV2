using Microsoft.AspNetCore.Identity;

namespace Mundialito.DAL.Accounts;

public interface IUsersRepository : IDisposable
{
    IEnumerable<MundialitoUser> AllUsers();

    MundialitoUser GetUser(String username);

    void DeleteUser(String username);

    void Save(); 
}
