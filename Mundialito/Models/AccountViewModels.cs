using System.Text.Json.Serialization;

namespace Mundialito.Models;

// Models returned by AccountController actions.

public class UserInfoViewModel
{
    [JsonPropertyName("userName")]
    public string? UserName { get; set; }

    [JsonPropertyName("firstName")]
    public string? FirstName { get; set; }

    [JsonPropertyName("lastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("email")]
    public string? Email { get; set; }

    [JsonPropertyName("roles")]
    public string? Roles { get; set; }
}

internal class JsonPropertyAttribute : Attribute
{
}


