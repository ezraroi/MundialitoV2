using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Mundialito.DAL.Accounts;



public class MundialitoUser : IdentityUser
{

    public Role Role { get; set; }

    [StringLength(100)]
    [MaxLength(100)]
    [Required]
    public string? FirstName { get; set; }

    [StringLength(100)]
    [MaxLength(100)]
    [Required]
    public string? LastName { get; set; }

    public ICollection<UserFollow> Followers { get; set; }
    public ICollection<UserFollow> Followees { get; set; }

    public MundialitoUser()
    {
        Followers = new List<UserFollow>();
        Followees = new List<UserFollow>();
    }

}
