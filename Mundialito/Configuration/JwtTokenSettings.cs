
namespace Mundialito.Configuration;

public class JwtTokenSettings
{
    public const string Key = "JwtTokenSettings";

    public string? ValidIssuer { get; set; }
    public string? ValidAudience { get; set; }
    public string? SymmetricSecurityKey { get; set; }
    public string? JwtRegisteredClaimNamesSub { get; set; }
    
}