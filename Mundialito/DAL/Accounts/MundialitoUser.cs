using Microsoft.AspNetCore.Identity;

namespace Mundialito.DAL.Accounts;

public class MundialitoUser : IdentityUser
{

    public string FirstName { get; set; }

    public string LastName { get; set; }
    
}
