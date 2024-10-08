using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;
using Mundialito.DAL.Accounts;

namespace Mundialito.Models;

public class UserModel
{
    public UserModel(MundialitoUser user)
    {
        Username = user.UserName;
        Name = string.Format("{0} {1}", user.FirstName, user.LastName);
        Id = user.Id.ToString();
        Email = user.Email;
        Points = 0;
        ProfilePicture = user.ProfilePicture == null ? "../../../icons/user.webp" :user.ProfilePicture;
        Roles = user.Role.ToString();
    }

    [JsonPropertyName("Id")]
    public string Id { get; private set; }

    [JsonPropertyName("Roles")]
    public string Roles { get; set; }

    [JsonPropertyName("ProfilePicture")]
    public string? ProfilePicture { get; private set; }

    [JsonPropertyName("Email")]
    public string Email { get; set; }
    
    [JsonPropertyName("Username")]
    public string Username { get; set; }

    [JsonPropertyName("Name")]
    public string Name { get; set; }

    [JsonPropertyName("GeneralBet")]
    public GeneralBetViewModel? GeneralBet { get; set; }

    [JsonPropertyName("Points")]
    public int Points { get; set; }

    [JsonPropertyName("Results")]
    public int Results { get; private set; }

    [JsonPropertyName("Marks")]
    public int Marks { get; private set; }

    [JsonPropertyName("Corners")]
    public int Corners { get; private set; }

    [JsonPropertyName("YellowCards")]
    public int YellowCards { get; private set; }

    public void SetGeneralBet(GeneralBetViewModel generalBet)
    {
        GeneralBet = generalBet;
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
                Marks++;
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

public class UserWithPointsModel : UserModel
{
    public UserWithPointsModel(MundialitoUser user) : base(user)
    {
        Points = 0;
        Place = string.Empty;
        Roles = user.Role.ToString();
    }

    [JsonPropertyName("Place")]
    public string Place { get; set; }

    [JsonPropertyName("PlaceDiff")]
    public string PlaceDiff { get; set; }
    
    [JsonPropertyName("YesterdayPoints")]
    public int YesterdayPoints { get; set; }
}


public class UserCompareModel
{
    [Required]
    [JsonPropertyName("Date")]
    public required DateTime Date { get; set; }

    [Required]
    [JsonPropertyName("Entries")]
    public required List<CompareEntry> Entries { get; set; }
}

public class CompareEntry
{
    [Required]
    [JsonPropertyName("Name")]
    public required string Name { get; set; }

    [Required]
    [JsonPropertyName("Place")]
    public required int Place { get; set; }
}