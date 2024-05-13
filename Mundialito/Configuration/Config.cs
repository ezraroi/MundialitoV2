
namespace Mundialito.Configuration;

public class Config
{


    public const string Key = "App";

    public string AdminUserName { get; set; } = "Roi";
    public string AdminLastName { get; set; } = "Ezra";
    public string AdminEmail { get; set; } = "ezraroi@gmail.com";
    public string TournamentStartDate { get; set; } = "01/06/2024";
    public string TournamentEndDate { get; set; } = "10/07/2024";
    public string ApplicationName { get; set; } = "Mundialito";
    public string TournamentDBCreatorName { get; set; } = "Mundial2023TournamentCreator";
    public string MonkeyUserName { get; set; } = "Monkey";
    public string FromAddress { get; set; } = "Monkey";
    public bool SendBetMail { get; set; } = true;
    public bool PrivateKeyProtection { get; set; } = true;

    public string SendGridUserName { get; set; } = "Monkey";
    public string SendGridPassword { get; set; } = "Monkey";
    public string LinkAddress { get; internal set; } = "http://localhost:5000";
}