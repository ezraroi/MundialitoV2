﻿using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.Models;

// Models used as parameters to AccountController actions.
public class ChangePasswordBindingModel
{
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    [JsonPropertyName("OldPassword")]
    public string? OldPassword { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    [JsonPropertyName("NewPassword")]
    public string? NewPassword { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [JsonPropertyName("ConfirmPassword")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string? ConfirmPassword { get; set; }
}

public class RegisterBindingModel
{
    [Required]
    [Display(Name = "User name")]
    [JsonPropertyName("Username")]
    public string? UserName { get; set; }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    [JsonPropertyName("Password")]
    public string? Password { get; set; }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [JsonPropertyName("ConfirmPassword")]
    public string? ConfirmPassword { get; set; }

    [Required]
    [Display(Name = "First Name")]
    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }

    [Required]
    [Display(Name = "Last Name")]
    [JsonPropertyName("LastName")]
    public string? LastName { get; set; }

    [Required]
    [DataType(DataType.EmailAddress)]
    [Display(Name = "Email Address")]
    [JsonPropertyName("Email")]
    public string? Email { get; set; }
}

public class ForgotPasswordModel
{
    [Required]
    [DataType(DataType.EmailAddress)]
    [JsonPropertyName("Email")]
    public string? Email { get; set; }
}

public class ResetPasswordModel
{
    [Required]
    [DataType(DataType.Password)]
    [JsonPropertyName("Password")]
    public string Password { get; set; }
    
    [Required]
    [DataType(DataType.Password)]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    [JsonPropertyName("ConfirmPassword")]
    public string ConfirmPassword { get; set; }
    
    [Required]
    [JsonPropertyName("Email")]
    public string Email { get; set; }
        
    [Required]
    [JsonPropertyName("Token")]
    public string Token { get; set; }

}