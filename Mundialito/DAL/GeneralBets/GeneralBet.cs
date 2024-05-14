using Mundialito.DAL.Accounts;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.DAL.GeneralBets;

public class GeneralBet
{

    public GeneralBet()
    {

    }

    public void Resolve(Boolean player, Boolean team)
    {
        IsResolved = true;
        TeamPoints = team ? 12 : 0;
        PlayerPoints = player ? 12 : 0;
    }

    [JsonPropertyName("GeneralBetId")]
    public int GeneralBetId { get; set; }

    [Required]
    [JsonPropertyName("MundialitoUser")]
    public MundialitoUser User { get; set; }

    [Required]
    [JsonPropertyName("WinningTeamId")]
    public int WinningTeamId { get; set; }

    [Required]
    [JsonPropertyName("GoldBootPlayerId")]
    public int GoldBootPlayerId { get; set; }

    [JsonPropertyName("IsResolved")]
    public Boolean IsResolved { get; set; }

    [JsonPropertyName("TeamPoints")]
    public int? TeamPoints { get; set; }


    [JsonPropertyName("PlayerPoints")]
    public int? PlayerPoints { get; set; }

    public override string ToString()
    {
        return string.Format("General Bet ID = {0}, Owner = {1}, WinningTeamId = {2}, GoldBootPlayerId = {3}, TeamPoints = {4}, PlayerPoints = {5}", GeneralBetId, User == null ? "Unkown" : User.UserName, WinningTeamId, GoldBootPlayerId, TeamPoints.HasValue ? TeamPoints.Value.ToString() : "NA", PlayerPoints.HasValue ? PlayerPoints.Value.ToString() : "NA");
    }
}
