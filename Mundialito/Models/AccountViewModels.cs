using System.Text.Json.Serialization;

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
}
