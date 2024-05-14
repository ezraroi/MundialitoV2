
namespace Mundialito.Configuration;

public class Config
{


    public const string Key = "App";

    public string AdminUserName { get; set; } = "admin";
    public string AdminFirstName { get; set; } = "admin";
    public string AdminLastName { get; set; } = "admin";
    public string AdminEmail { get; set; } = "adminmundialito.com";
    public string TournamentStartDate { get; set; } = "01/06/2024";
    public string TournamentEndDate { get; set; } = "10/07/2024";
    public string ApplicationName { get; set; } = "EuroChamp";
    public string TournamentDBCreatorName { get; set; } = "Mundial2023TournamentCreator";
    public string MonkeyUserName { get; set; } = "monkey";
    public string FromAddress { get; set; } = "http://hello.com";
    public bool SendBetMail { get; set; } = true;
    public bool PrivateKeyProtection { get; set; } = true;

    public string SendGridUserName { get; set; } = "Monkey";
    public string SendGridPassword { get; set; } = "Monkey";
    public string LinkAddress { get; internal set; } = "http://localhost:5000";
}