
namespace Mundialito.Configuration;

public class Config
{
    public const string Key = "App";

    public string? AdminUserName { get; set; }
    public string? AdminFirstName { get; set; }
    public string? AdminLastName { get; set; }
    public string? AdminEmail { get; set; }
    public string? TournamentStartDate { get; set; }
    public string? TournamentEndDate { get; set; }
    public string? ApplicationName { get; set; }
    public string? TournamentDBCreatorName { get; set; }
    public string? MonkeyUserName { get; set; }
    public bool SendBetMail { get; set; }
    public bool PrivateKeyProtection { get; set; }
    public string? LinkAddress { get; set; }
    public string? Theme { get; set; }
    public string FromAddress { get; set; }
    public DBTypeEnum DBType {get; set;} = DBTypeEnum.SQLLite;
    public string EmailConnectionString {get; set;}
    public Dictionary<string, string> ClientConfig {get; set;}
}