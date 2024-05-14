
using System.Text.Json.Serialization;

public class AuthResponse
{
    public string? Username { get; set; }
    public string? Email { get; set; }
    [JsonPropertyName("access_token")]
    public string? AccessToken { get; set; }
}