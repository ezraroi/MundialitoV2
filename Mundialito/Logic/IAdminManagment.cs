namespace Mundialito.Logic;

public interface IAdminManagment
{
    void MakeAdmin(string userId);

    bool IsAdmin(string userId);
}

