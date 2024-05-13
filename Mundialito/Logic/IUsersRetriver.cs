using Mundialito.Models;

namespace Mundialito.Logic;

public interface IUsersRetriver
{

    UserModel GetUser(String username, bool isLogged);
    List<UserModel> GetAllUsers();
}

