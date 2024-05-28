
using System.Text.Json.Serialization;

public class AuthResponse
{
    [JsonPropertyName("Username")]
    public string? Username { get; set; }


    [JsonPropertyName("FirstName")]
    public string? FirstName { get; set; }


    [JsonPropertyName("LastName")]
    public string? LastName { get; set; }

    [JsonPropertyName("Email")]
    public string? Email { get; set; }

    [JsonPropertyName("AccessToken")]
    public string? AccessToken { get; set; }

    [JsonPropertyName("Roles")]
    public string? Roles { get; set; }
}