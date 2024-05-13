using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace Mundialito.DAL.Accounts;

public class MundialitoUser : IdentityUser
{

    [StringLength(100)]
    [MaxLength(100)]
    [Required]
    public string? FirstName { get; set; }

    [StringLength(100)]
    [MaxLength(100)]
    [Required]
    public string? LastName { get; set; }

}
