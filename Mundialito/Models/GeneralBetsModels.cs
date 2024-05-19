using Mundialito.DAL.GeneralBets;
using System.Text.Json.Serialization;

namespace Mundialito.Models;


public class GeneralBetViewModel
{
    public GeneralBetViewModel(GeneralBet bet, DateTime closeTime)
    {
        GeneralBetId = bet.GeneralBetId;
        WinningTeamId = bet.WinningTeamId;
        GoldenBootPlayerId = bet.GoldBootPlayerId;
        IsResolved = bet.IsResolved;
        if (IsResolved)
            Points = bet.PlayerPoints.Value + bet.TeamPoints.Value;
        CloseTime = closeTime;
        OwnerName = string.Format("{0} {1}", bet.User.FirstName, bet.User.LastName);
        IsClosed = DateTime.UtcNow > CloseTime;
    }

    [JsonPropertyName("GeneralBetId")]
    public int GeneralBetId { get; set; }

    [JsonPropertyName("WinningTeamId")]
    public int WinningTeamId { get; set; }

    [JsonPropertyName("OwnerName")]
    public String OwnerName { get; private set; }

    [JsonPropertyName("GoldenBootPlayerId")]
    public int GoldenBootPlayerId { get; set; }

    [JsonPropertyName("IsResolved")]
    public Boolean IsResolved { get; set; }

    [JsonPropertyName("IsClosed")]
    public Boolean IsClosed { get; private set; }
    
    [JsonPropertyName("Points")]
    public int Points { get; set; }

    [JsonPropertyName("CloseTime")]
    public DateTime CloseTime { get; set; }
}

public class NewGeneralBetModel
{
    [JsonPropertyName("WinningTeamId")]
    public int WinningTeamId { get; set; }

    [JsonPropertyName("GoldenBootPlayerId")]
    public int GoldenBootPlayerId { get; set; }

    [JsonPropertyName("GeneralBetId")]
    public int GeneralBetId { get; set; }
}

public class UpdateGenralBetModel
{
    [JsonPropertyName("WinningTeamId")]
    public int WinningTeamId { get; set; }

    [JsonPropertyName("GoldenBootPlayerId")]
    public int GoldenBootPlayerId { get; set; }
}

public class ResolveGeneralBetModel
{
    [JsonPropertyName("PlayerIsRight")]
    public Boolean PlayerIsRight { get; set; }

    [JsonPropertyName("TeamIsRight")]
    public Boolean TeamIsRight { get; set; }
}
