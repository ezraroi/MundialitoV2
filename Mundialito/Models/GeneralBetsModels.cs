using Mundialito.DAL.GeneralBets;
using System.Text.Json.Serialization;

namespace Mundialito.Models;


public class GeneralBetViewModel
{
    public GeneralBetViewModel(GeneralBet bet, DateTime closeTime)
    {
        OwnerId = bet.User.Id;
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
    
    [JsonPropertyName("OwnerName")]
    public string OwnerName { get; private set; }

    [JsonPropertyName("GoldenBootPlayerId")]
    public int GoldenBootPlayerId { get; set; }

    [JsonPropertyName("WinningTeamId")]
    public int WinningTeamId { get; set; }

    [JsonPropertyName("IsResolved")]
    public bool IsResolved { get; set; }

    [JsonPropertyName("IsClosed")]
    public bool IsClosed { get; private set; }
    
    [JsonPropertyName("Points")]
    public int Points { get; set; }

    [JsonPropertyName("CloseTime")]
    public DateTime CloseTime { get; set; }

    [JsonPropertyName("OwnerId")]
    public string OwnerId { get; set; }
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
    public bool PlayerIsRight { get; set; }

    [JsonPropertyName("TeamIsRight")]
    public bool TeamIsRight { get; set; }
}
