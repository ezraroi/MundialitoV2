using System.Text.Json.Serialization;
using Mundialito.DAL.Accounts;

namespace Mundialito.Models;

public class UserModel
{
    public UserModel(MundialitoUser user)
    {
        Username = user.UserName;
        Name = String.Format("{0} {1}", user.FirstName, user.LastName);
        Points = 0;
        Place = String.Empty;
        Id = user.Id;
        Email = user.Email;
    }

    public UserModel(String id, String username)
    {
        Username = username;
        Id = id;
    }

    [JsonPropertyName("Id")]
    public String Id { get; private set; }

    [JsonPropertyName("Place")]
    public String Place { get; set; }

    [JsonPropertyName("PlaceDiff")]
    public String PlaceDiff { get; set; }

    [JsonPropertyName("Email")]
    public String Email { get; set; }
    
    [JsonPropertyName("IsAdmin")]
    public bool IsAdmin { get; set; }

    [JsonPropertyName("Username")]
    public String Username { get; set; }

    [JsonPropertyName("Name")]
    public String Name { get; set; }

    [JsonPropertyName("Points")]
    public int Points { get; set; }

    [JsonPropertyName("YesterdayPoints")]
    public int YesterdayPoints { get; set; }

    [JsonPropertyName("Results")]
    public int Results { get; private set; }

    [JsonPropertyName("Marks")]
    public int Marks { get; private set; }
    
    [JsonPropertyName("TotalMarks")]
    public int TotalMarks { get; set; }

    [JsonPropertyName("Corners")]
    public int Corners { get; private set; }

    [JsonPropertyName("YellowCards")]
    public int YellowCards { get; private set; }

    public void SetGeneralBet(GeneralBetViewModel generalBet)
    {
        if (generalBet.IsResolved)
        {
            Points += generalBet.Points;
        }
    }

    public void AddBet(BetViewModel bet)
    {
        if (bet.IsResolved)
        {
            Points += bet.Points;
            if (bet.ResultWin)
            {
                Results++;
            }
            else if (bet.GameMarkWin)
            {
                Marks++;
            }
            if (bet.CardsWin)
                YellowCards++;
            if (bet.CornersWin)
                Corners++;
        }
    }
}
