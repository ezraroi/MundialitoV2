
namespace Mundialito.Configuration;

public class Config
{
    public const string Key = "App";

    public string AdminUserName { get; set; } = "admin";
    public string AdminFirstName { get; set; } = "roi";
    public string AdminLastName { get; set; } = "ezra";
    public string AdminEmail { get; set; } = "admin@mundialito.com";
    public string TournamentStartDate { get; set; } = "14/06/2024";
    public string TournamentEndDate { get; set; } = "14/07/2024";
    public string ApplicationName { get; set; } = "EuroChamp";
    public string TournamentDBCreatorName { get; set; } = "Euro2024";
    public string MonkeyUserName { get; set; } = "monkey";
    public bool SendBetMail { get; set; } = true;
    public bool PrivateKeyProtection { get; set; } = true;
    public string LinkAddress { get; internal set; } = "http://localhost:5150";
    public string Theme { get; internal set; } = "spaceLab";
    public string FromAddress { get; set; } = "admin@admin";
    public bool UseSqlLite {get; set;} 
    public string EmailConnectionString {get; set;}
}