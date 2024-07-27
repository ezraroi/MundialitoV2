using Mundialito.DAL.Accounts;
using Mundialito.DAL.Games;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Mundialito.DAL.Bets;

public class Bet
{
    public Bet()
    {

    }

    public Bet(MundialitoUser user, Game game)
    {
        User = user;
        Game = game;
    }

    [JsonPropertyName("BetId")]
    public int BetId { get; set; }

    [Required]
    [JsonPropertyName("UserId")]
    public string UserId { get; set; }
            
    [JsonPropertyName("MundialitoUser")]
    public MundialitoUser User { get; set; }

    [Required]
    [JsonPropertyName("GameId")]
    public int GameId { get; set; }
    
    [JsonPropertyName("Game")]
    public Game Game { get; set; }

    [Required]
    [Range(0, 10)]
    [JsonPropertyName("HomeScore")]
    public int HomeScore { get; set; }

    [Required]
    [Range(0, 10)]
    [JsonPropertyName("AwayScore")]
    public int AwayScore { get; set; }

    [Required]
    [StringLength(1)]
    [RegularExpression("[1X2]")]
    [JsonPropertyName("CornersMark")]
    public string CornersMark { get; set; }

    [Required]
    [StringLength(1)]
    [RegularExpression("[1X2]")]
    [JsonPropertyName("CardsMark")]
    public string CardsMark { get; set; }

    [JsonPropertyName("Points")]
    public int? Points { get; set; }

    
    [JsonPropertyName("CornersWin")]
    public bool CornersWin { get; set; }

    [JsonPropertyName("GameMarkWin")]
    public bool GameMarkWin { get; set; }


    [JsonPropertyName("ResultWin")]
    public bool ResultWin { get; set; }

    [JsonPropertyName("CardsWin")]
    public bool CardsWin { get; set; }

    [JsonPropertyName("MaxPoints")]
    public bool MaxPoints { get; set; }

    public override string ToString()
    {
        return string.Format("Bet ID = {0}, UserID = {1}, Game ID = {2}", BetId, UserId, GameId);
    }
}
