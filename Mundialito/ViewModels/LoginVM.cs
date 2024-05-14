using System.ComponentModel.DataAnnotations;

namespace Mundialito.ViewModels;

public class LoginVM
{
    [Required(ErrorMessage ="username is required.")]   
    public string? Username { get; set; }

    [Required(ErrorMessage ="Password is required.")]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Display(Name ="Remember Me")]
    public bool RememberMe { get; set; }
}