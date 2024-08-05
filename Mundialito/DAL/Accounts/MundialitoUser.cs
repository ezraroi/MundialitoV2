using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.AspNetCore.Identity;

namespace Mundialito.DAL.Accounts
{
    public class MundialitoUser : IdentityUser
    {
        public Role Role { get; set; }

        public string? ProfilePicture { get; set; }

        [StringLength(100)]
        [MaxLength(100)]
        [Required]
        public string? FirstName { get; set; }

        [StringLength(100)]
        [MaxLength(100)]
        [Required]
        public string? LastName { get; set; }

        [NotMapped]
        public string FullName
        {
            get
            {
                return $"{FirstName} {LastName}";
            }
        }

        public ICollection<UserFollow> Followers { get; set; }
        public ICollection<UserFollow> Followees { get; set; }

        public MundialitoUser()
        {
            Followers = new List<UserFollow>();
            Followees = new List<UserFollow>();
        }

    }

}