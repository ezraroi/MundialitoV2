using Microsoft.AspNetCore.Identity;
using Mundialito.DAL.Accounts;

namespace Mundialito.Models;

public class UserModel
{
    public UserModel(IdentityUser user)
    {
        Username = user.UserName;
        Name = String.Format("{0} {1}", user.UserName, user.UserName); // TODO: Add first and last name
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

    public String Id { get; private set; }

    public String Place { get; set; }

    public String PlaceDiff { get; set; }

    public String Email { get; set; }
    
    public bool IsAdmin { get; set; }

    public String Username { get; set; }

    public String Name { get; set; }

    public int Points { get; set; }

    public int YesterdayPoints { get; set; }

    public int Results { get; private set; }

    public int Marks { get; private set; }
    
    public int TotalMarks { get; set; }

    public int Corners { get; private set; }

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
