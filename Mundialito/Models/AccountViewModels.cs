using System.Text.Json.Serialization;
using Mundialito.DAL.Accounts;

namespace Mundialito.Models;

// Models returned by AccountController actions.

public class UserInfoViewModel
{
    [JsonPropertyName("Username")]
    public string? UserName { get; set; }

    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("LastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("Email")]
    public string? Email { get; set; }

    [JsonPropertyName("Roles")]
    public string? Roles { get; set; }

    [JsonPropertyName("Followers")]
    public ICollection<string> Followers { get; set; }
    
    [JsonPropertyName("Followees")]
    public ICollection<string> Followees { get; set; }
}
