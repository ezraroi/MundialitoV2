using System.ComponentModel.DataAnnotations;

namespace Mundialito.ViewModels;

public class RegisterVM
{
    [Required]
    public string? FirstName { get; set; }
    [Required]
    public string? LastName { get; set; }
    [Required]
    [DataType(DataType.EmailAddress)]
    public string? Email { get; set; }
    [Required]
    [DataType(DataType.Password)]
    public string? Password { get; set; }

    [Compare("Password", ErrorMessage = "Passwords don't match.")]
    [Display(Name = "Confirm Password")]
    [DataType(DataType.Password)]
    public string? ConfirmPassword { get; set; }

    
}